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
    public class JobAllocatedEstimatesDetailsDAL
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

            try
            {
                OracleDecimal od = reader.GetOracleDecimal(ordinal);
                od = OracleDecimal.SetPrecision(od, 28);
                return od.Value;
            }
            catch (OverflowException)
            {
                try
                {
                    return Convert.ToDecimal((double)reader.GetOracleDecimal(ordinal));
                }
                catch
                {
                    return null;
                }
            }
        }

        public List<JobAllocatedEstimatesDetailsModel> GetJobAllocatedEstimatesDetails(string costCtr, string matCode)
        {
            var result = new List<JobAllocatedEstimatesDetailsModel>();

            const string query = @"
                SELECT   T3.res_cd,
                         SUM(T3.estimate_qty) AS est_qty,
                         (SELECT mat_nm
                            FROM inmatm
                           WHERE mat_cd = T3.res_cd) AS mat_name,
                         (SELECT qty_on_hand
                            FROM inwrhmtm
                           WHERE TRIM(mat_cd) = TRIM(T3.res_cd)
                             AND dept_id = :costctr
                             AND status IN (2, 7)
                             AND grade_cd = 'NEW') AS qty_on_hand,
                         (SELECT dept_nm
                            FROM gldeptm
                           WHERE dept_id = :costctr) AS cct_name
                FROM     pcesthmt T1, pcestdmt T3
                WHERE    T1.estimate_no = T3.estimate_no
                  AND    T1.dept_id = T3.dept_id
                  AND    T1.status = 22
                  AND    T3.res_cat = 1
                  AND    T3.res_cd LIKE :matcode || '%'
                  AND    T1.dept_id = :costctr
                GROUP BY T3.res_cd, T3.dept_id
                ORDER BY T3.res_cd";

            string costCtrTrimmed = (costCtr ?? string.Empty).Trim();
            string matCodeTrimmed = (matCode ?? string.Empty).Trim();

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(query, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.BindByName = true; // required so :costctr (used three times) binds correctly by name

                cmd.Parameters.Add(new OracleParameter("costctr", OracleDbType.Varchar2) { Value = costCtrTrimmed });
                cmd.Parameters.Add(new OracleParameter("matcode", OracleDbType.Varchar2) { Value = matCodeTrimmed });

                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new JobAllocatedEstimatesDetailsModel
                        {
                            MatCode = SafeGetString(reader, "res_cd"),
                            EstQty = SafeGetDecimal(reader, "est_qty"),
                            MatName = SafeGetString(reader, "mat_name"),
                            QtyOnHand = SafeGetDecimal(reader, "qty_on_hand"),
                            CctName = SafeGetString(reader, "cct_name")
                        });
                    }
                }
            }

            return result;
        }
    }
}