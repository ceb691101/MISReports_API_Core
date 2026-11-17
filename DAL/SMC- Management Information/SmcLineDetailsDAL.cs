using MISReports_Api.Compat;
using System;
using System.Collections.Generic;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using MISReports_Api.Models.Accounts;
namespace MISReports_Api.DAL
{
    public class SmcLineDetailsDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        // Overflow-safe decimal reader, same pattern used on CCJobSummaryDAL.
        // Oracle NUMBER columns can exceed .NET decimal's ~28-29 digit range,
        // making Convert.ToDecimal throw OverflowException.
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

        public List<SmcLineDetailsModel> GetSmcLineDetails(string fromDate, string toDate, string compId)
        {
            var result = new List<SmcLineDetailsModel>();

            string query = @"
        SELECT
               (SELECT comp_nm FROM glcompm WHERE comp_id = :compid) AS comp_nm,
               (SELECT comp_nm FROM glcompm WHERE comp_id IN (SELECT comp_id FROM gldeptm WHERE dept_id = A.dept_id)) AS area,
               A.dept_id,
               B.phase, B.connection_type, B.tariff_cat_code,
               E.loop_cable,
               D.wiring_type, D.line_length, D.service_length, D.inside_length,
               A.std_cost AS actual_cost, D.total_cost AS Standard_Cost,
               A.project_no
        FROM pcesthmt A, pcestdmt A1, wiring_land_detail B, application_reference C, speststd D, spserest E
        WHERE TRIM(E.application_no) = TRIM(C.application_no)
          AND A.prj_ass_dt >= TO_DATE(:fromdate,'yyyy/mm/dd')
          AND A.prj_ass_dt <= TO_DATE(:todate,'yyyy/mm/dd')
          AND TRIM(A.estimate_no) = TRIM(C.application_no)
          AND TRIM(B.application_id) = TRIM(C.application_id)
          AND TRIM(A.estimate_no) = TRIM(D.estimate_no)
          AND A.dept_id = A1.dept_id
          AND A.estimate_no = A1.estimate_no
          AND A1.commited_qty > 0
          AND A.estimate_no LIKE '%ENC%'
          AND A.dept_id IN (
              SELECT dept_id FROM gldeptm
              WHERE comp_id IN (
                  SELECT comp_id FROM glcompm
                  WHERE status = 2
                    AND (comp_id = :compid OR parent_id = :compid OR grp_comp = :compid)
              )
          )
        GROUP BY A.dept_id, B.phase, B.connection_type, B.tariff_cat_code, D.wiring_type,
                 E.loop_cable, A.std_cost, D.total_cost,
                 A.project_no, D.line_length, D.service_length, D.inside_length
        ORDER BY A.dept_id, A.project_no, B.phase, B.connection_type, B.tariff_cat_code, E.loop_cable, D.wiring_type";

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(query, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.BindByName = true;
                cmd.Parameters.Add(new OracleParameter("compid", OracleDbType.Varchar2) { Value = compId });
                cmd.Parameters.Add(new OracleParameter("fromdate", OracleDbType.Varchar2) { Value = fromDate });
                cmd.Parameters.Add(new OracleParameter("todate", OracleDbType.Varchar2) { Value = toDate });

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new SmcLineDetailsModel
                        {
                            CompNm = reader["comp_nm"] == DBNull.Value ? null : reader["comp_nm"].ToString(),
                            Area = reader["area"] == DBNull.Value ? null : reader["area"].ToString(),
                            DeptId = reader["dept_id"] == DBNull.Value ? null : reader["dept_id"].ToString(),
                            Phase = reader["phase"] == DBNull.Value ? null : reader["phase"].ToString(),
                            ConnectionType = reader["connection_type"] == DBNull.Value ? null : reader["connection_type"].ToString(),
                            TariffCatCode = reader["tariff_cat_code"] == DBNull.Value ? null : reader["tariff_cat_code"].ToString(),
                            LoopCable = reader["loop_cable"] == DBNull.Value ? null : reader["loop_cable"].ToString(),
                            WiringType = reader["wiring_type"] == DBNull.Value ? null : reader["wiring_type"].ToString(),
                            LineLength = SafeGetDecimal(reader, "line_length"),
                            ServiceLength = SafeGetDecimal(reader, "service_length"),
                            InsideLength = SafeGetDecimal(reader, "inside_length"),
                            ActualCost = SafeGetDecimal(reader, "actual_cost"),
                            StandardCost = SafeGetDecimal(reader, "Standard_Cost"),
                            ProjectNo = reader["project_no"] == DBNull.Value ? null : reader["project_no"].ToString()
                        });
                    }
                }
            }
            return result;
        }
    }
}