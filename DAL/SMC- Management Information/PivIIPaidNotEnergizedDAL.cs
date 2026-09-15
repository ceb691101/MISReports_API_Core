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
    public class PivIIPaidNotEnergizedDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<PivIIPaidNotEnergizedModel> GetPivIIPaidNotEnergized(string fromDate, string toDate, string costCtr)
        {
            var result = new List<PivIIPaidNotEnergizedModel>();

            const string query = @"
                SELECT A.application_type,
                       (SELECT DISTINCT Description FROM applicationsubtypes WHERE AppSUBTYPE = A.application_sub_type) AS application_sub_type,
                       D.tariff_code,
                       D.phase,
                       T1.std_cost,
                       T1.estimate_no,
                       T1.project_no,
                       C.confirmed_date,
                       C.piv_no,
                       C.paid_amount,
                       (SELECT dept_nm FROM gldeptm WHERE dept_id = :costctr) AS cct_name
                FROM applications A, pcesthmt T1, piv_detail C, wiring_land_detail D
                WHERE TRIM(T1.project_no) NOT IN (
                        SELECT TRIM(L.project_no) FROM spodrcrd L WHERE L.dept_id = :costctr
                      )
                  AND A.application_type IN ('NC', 'CR')
                  AND TRIM(T1.estimate_no) = TRIM(C.reference_no)
                  AND TRIM(T1.estimate_no) = TRIM(A.application_no)
                  AND A.application_no = C.reference_no
                  AND A.application_id = D.application_id
                  AND C.id_no = A.id_no
                  AND A.dept_id = C.dept_id
                  AND A.dept_id = D.dept_id
                  AND C.reference_type = 'EST'
                  AND C.status IN ('C', 'P')
                  AND C.confirmed_date >= TO_DATE(:fromdate, 'yyyy/mm/dd')
                  AND C.confirmed_date <= TO_DATE(:todate, 'yyyy/mm/dd')
                  AND T1.dept_id = :costctr
                ORDER BY 1, 2, 3, 4";

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(query, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.BindByName = true; // required so :costctr (used 3x) binds correctly by name
                cmd.Parameters.Add(new OracleParameter("costctr", OracleDbType.Varchar2) { Value = costCtr });
                cmd.Parameters.Add(new OracleParameter("fromdate", OracleDbType.Varchar2) { Value = fromDate });
                cmd.Parameters.Add(new OracleParameter("todate", OracleDbType.Varchar2) { Value = toDate });

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new PivIIPaidNotEnergizedModel
                        {
                            ApplicationType = reader["application_type"] == DBNull.Value ? null : reader["application_type"].ToString(),
                            ApplicationSubType = reader["application_sub_type"] == DBNull.Value ? null : reader["application_sub_type"].ToString(),
                            TariffCode = reader["tariff_code"] == DBNull.Value ? null : reader["tariff_code"].ToString(),
                            Phase = reader["phase"] == DBNull.Value ? null : reader["phase"].ToString(),
                            StdCost = GetSafeDecimal(reader, "std_cost"),
                            EstimateNo = reader["estimate_no"] == DBNull.Value ? null : reader["estimate_no"].ToString(),
                            ProjectNo = reader["project_no"] == DBNull.Value ? null : reader["project_no"].ToString(),
                            ConfirmedDate = reader["confirmed_date"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["confirmed_date"]),
                            PivNo = reader["piv_no"] == DBNull.Value ? null : reader["piv_no"].ToString(),
                            PaidAmount = GetSafeDecimal(reader, "paid_amount"),
                            CctName = reader["cct_name"] == DBNull.Value ? null : reader["cct_name"].ToString()
                        });
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Safely reads a NUMBER column as a nullable decimal.
        /// Oracle's NUMBER type can carry more precision/scale than .NET's decimal
        /// can hold (decimal maxes out around 28-29 significant digits), which makes
        /// a plain Convert.ToDecimal(reader[...]) throw OverflowException on some rows.
        /// Reading via OracleDecimal and clamping precision first avoids that.
        /// </summary>
        private static decimal? GetSafeDecimal(OracleDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            if (reader.IsDBNull(ordinal))
                return null;

            OracleDecimal oraVal = reader.GetOracleDecimal(ordinal);

            // Clamp to a precision .NET decimal can safely represent.
            oraVal = OracleDecimal.SetPrecision(oraVal, 28);

            return oraVal.Value;
        }
    }
}