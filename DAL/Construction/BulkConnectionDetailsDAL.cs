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
    public class BulkConnectionDetailsDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

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

        private const string ProvinceCase = @"
                        (CASE
                            WHEN a.dept_id LIKE '410%' THEN 'NP'
                            WHEN a.dept_id LIKE '420%' THEN 'NCP'
                            WHEN a.dept_id LIKE '430%' THEN 'CP'
                            WHEN a.dept_id LIKE '490%' THEN 'CP2'
                            WHEN a.dept_id LIKE '440%' THEN 'WPN'
                            WHEN a.dept_id LIKE '450%' THEN 'NWP'
                            WHEN a.dept_id LIKE '480%' THEN 'NWP2'
                            WHEN a.dept_id LIKE '460%' THEN 'EP'
                            WHEN a.dept_id LIKE '510%' THEN 'WPS I'
                            WHEN a.dept_id LIKE '501%' OR a.dept_id LIKE '513%' THEN 'WPS II'
                            WHEN a.dept_id LIKE '520%' THEN 'SP'
                            WHEN a.dept_id LIKE '940%' THEN 'SP2'
                            WHEN a.dept_id LIKE '530%' THEN 'UVA'
                            WHEN a.dept_id LIKE '540%' THEN 'CC'
                            WHEN a.dept_id LIKE '550%' THEN 'SAB'
                            ELSE a.dept_id
                         END)";

        public List<BulkConnectionDetailsModel> GetBulkConnectionDetails(DateTime fromDate, DateTime toDate)
        {
            var result = new List<BulkConnectionDetailsModel>();

            string query = $@"
                SELECT  a.prj_ass_dt,
                        a.project_no,
                        {ProvinceCase} AS provision,
                        a.dept_id,
                        d1.line_type,
                        d1.length AS mv_line,
                        d1.linedes,
                        d1.line_cost,
                        b.demand,
                        d.total_cost AS standard_cost,
                        d.cebcost,
                        d.rebate_cost AS standard_rebate_cost,
                        d.toconpay AS consumer_payable,
                        a.partial_amt AS construction_rebate_amount,
                        a.std_cost AS work_estimate_actual_cost
                FROM      pcesthmt a, wiring_land_detail b, applications c,
                          spstdesthmt d, spstdestdmt d1, estimate_referencebs e
                WHERE     a.prj_ass_dt >= :fromdate
                  AND     a.prj_ass_dt <  :todateexcl
                  AND     c.application_type = 'BS'
                  AND     TRIM(b.application_id) = TRIM(c.application_id)
                  AND     TRIM(c.application_no) = TRIM(d.app_no)
                  AND     TRIM(e.sestimate_no) = TRIM(d.app_no)
                  AND     TRIM(e.westimate_no) = TRIM(a.estimate_no)
                  AND     d1.app_no = d.app_no
                  AND     d1.dept_id = d.dept_id
                ORDER BY {ProvinceCase}, a.dept_id, a.prj_ass_dt, a.project_no, d1.line_type";

            DateTime toDateExclusive = toDate.Date.AddDays(1);

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(query, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.BindByName = true;

                cmd.Parameters.Add(new OracleParameter("fromdate", OracleDbType.Date) { Value = fromDate.Date });
                cmd.Parameters.Add(new OracleParameter("todateexcl", OracleDbType.Date) { Value = toDateExclusive });

                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new BulkConnectionDetailsModel
                        {
                            PrjAssDt = reader["prj_ass_dt"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["prj_ass_dt"]),
                            ProjectNo = reader["project_no"] == DBNull.Value ? null : reader["project_no"].ToString(),
                            Provision = reader["provision"] == DBNull.Value ? null : reader["provision"].ToString(),
                            DeptId = reader["dept_id"] == DBNull.Value ? null : reader["dept_id"].ToString(),
                            LineType = reader["line_type"] == DBNull.Value ? null : reader["line_type"].ToString(),
                            MvLine = SafeGetDecimal(reader, "mv_line"),
                            LineDes = reader["linedes"] == DBNull.Value ? null : reader["linedes"].ToString(),
                            LineCost = SafeGetDecimal(reader, "line_cost"),
                            Demand = SafeGetDecimal(reader, "demand"),
                            StandardCost = SafeGetDecimal(reader, "standard_cost"),
                            CebCost = SafeGetDecimal(reader, "cebcost"),
                            StandardRebateCost = SafeGetDecimal(reader, "standard_rebate_cost"),
                            ConsumerPayable = SafeGetDecimal(reader, "consumer_payable"),
                            ConstructionRebateAmount = SafeGetDecimal(reader, "construction_rebate_amount"),
                            WorkEstimateActualCost = SafeGetDecimal(reader, "work_estimate_actual_cost")
                        });
                    }
                }
            }

            return result;
        }
    }
}