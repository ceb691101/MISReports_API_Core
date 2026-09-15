using System;
using System.Collections.Generic;
using System.Linq;
using System.Configuration;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using MISReports_Api.Models.Accounts;

namespace MISReports_Api.DAL
{
    public class FundSummaryDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        private static string SafeGetString(OracleDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            if (reader.IsDBNull(ordinal)) return null;

            Type fieldType = reader.GetFieldType(ordinal);
            if (fieldType == typeof(decimal))
            {
                OracleDecimal od = reader.GetOracleDecimal(ordinal);
                try
                {
                    od = OracleDecimal.SetPrecision(od, 28);
                    return od.Value.ToString();
                }
                catch (OverflowException)
                {
                    return ((double)od).ToString();
                }
            }

            return reader.GetValue(ordinal)?.ToString();
        }

        private static decimal? SafeGetDecimal(OracleDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            if (reader.IsDBNull(ordinal)) return null;

            OracleDecimal od = reader.GetOracleDecimal(ordinal);
            try
            {
                od = OracleDecimal.SetPrecision(od, 28);
                return od.Value;
            }
            catch (OverflowException)
            {
                return (decimal)(double)od;
            }
        }

        public List<FundSummaryModel> GetFundSummary(DateTime fromDate, DateTime toDate, string compId)
        {
            var result = new List<FundSummaryModel>();

            const string query = @"
                SELECT    h.dept_id,
                          h.project_no,
                          c.LINE_LENGTH   AS TOTAL_LENGTH,
                          c.INSIDE_LENGTH AS PREMISES_LENGTH,
                          c.TOTAL_COST    AS CUSTOMER_AMOUNT,
                          qry.RES_TYPE,
                          qry.totcost     AS TOTCOST,
                          qry.cebcost     AS CEBCOST,
                          (SELECT dept_nm FROM gldeptm WHERE dept_id = h.dept_id) AS CCT_NAME,
                          (SELECT comp_nm FROM glcompm WHERE TRIM(comp_id) = TRIM(:compid)) AS AREA_NAME
                FROM      speststd c,
                          pcesthmt h,
                          (SELECT estimate_no,
                                  RES_TYPE,
                                  SUM(UNIT_PRICE * FUND_QTY)     AS cebcost,
                                  SUM(UNIT_PRICE * ESTIMATE_QTY) AS totcost
                             FROM pcestdmt d
                            WHERE d.ESTIMATE_NO LIKE '%/ENC/%'
                              AND d.dept_id IN
                                  (SELECT dept_id
                                     FROM gldeptm
                                    WHERE TRIM(comp_id) IN
                                          (SELECT comp_id
                                             FROM glcompm
                                            WHERE TRIM(comp_id) = TRIM(:compid)))
                            GROUP BY estimate_no, RES_TYPE) qry
                WHERE     TRIM(h.estimate_no) = TRIM(qry.estimate_no)
                  AND     TRIM(h.estimate_no) = TRIM(c.estimate_no)
                  AND     h.ETIMATE_DT >= :fromdate
                  AND     h.ETIMATE_DT <  :todateexcl
                GROUP BY  h.dept_id, h.project_no, c.LINE_LENGTH, c.INSIDE_LENGTH,
                          c.TOTAL_COST, qry.RES_TYPE, qry.totcost, qry.cebcost
                ORDER BY  h.dept_id, h.project_no";

            string compIdTrimmed = (compId ?? string.Empty).Trim();
            DateTime toDateExclusive = toDate.Date.AddDays(1);

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(query, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.BindByName = true; // required so :compid (used multiple times) binds correctly by name

                cmd.Parameters.Add(new OracleParameter("compid", OracleDbType.Varchar2) { Value = compIdTrimmed });
                cmd.Parameters.Add(new OracleParameter("fromdate", OracleDbType.Date) { Value = fromDate.Date });
                cmd.Parameters.Add(new OracleParameter("todateexcl", OracleDbType.Date) { Value = toDateExclusive });

                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new FundSummaryModel
                        {
                            DeptId = SafeGetString(reader, "DEPT_ID"),
                            ProjectNo = SafeGetString(reader, "PROJECT_NO"),
                            TotalLength = SafeGetDecimal(reader, "TOTAL_LENGTH"),
                            PremisesLength = SafeGetDecimal(reader, "PREMISES_LENGTH"),
                            CustomerAmount = SafeGetDecimal(reader, "CUSTOMER_AMOUNT"),
                            ResType = SafeGetString(reader, "RES_TYPE"),
                            TotCost = SafeGetDecimal(reader, "TOTCOST"),
                            CebCost = SafeGetDecimal(reader, "CEBCOST"),
                            CctName = SafeGetString(reader, "CCT_NAME"),
                            AreaName = SafeGetString(reader, "AREA_NAME")
                        });
                    }
                }
            }

            return result;
        }
    }
}