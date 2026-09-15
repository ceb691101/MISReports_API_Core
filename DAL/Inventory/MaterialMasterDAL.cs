using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using MISReports_Api.Models.Inventory;

namespace MISReports_Api.DAL
{
    public class MaterialMasterDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<MaterialMasterModel> GetMaterialMaster(string matCode, int? status)
        {
            var result = new List<MaterialMasterModel>();

            const string query = @"
                SELECT A.mat_cd,
                       A.mat_nm,
                       A.maj_uom,
                       A.unit_price,
                       CASE
                           WHEN A.status = 2 THEN 'Active'
                           WHEN A.status = 3 THEN 'Inactive'
                           ELSE A.status || '- Unknown'
                       END AS status
                FROM inmatm A
                WHERE
                    (:matcode IS NULL OR A.mat_cd LIKE :matcode || '%')
                AND
                    (:status IS NULL OR A.status = :status)
                AND
                    A.status IN (2,3)
                ORDER BY A.mat_cd";

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(query, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.BindByName = true;

                // Material Code parameter
                cmd.Parameters.Add(new OracleParameter("matcode", OracleDbType.Varchar2)
                {
                    Value = string.IsNullOrWhiteSpace(matCode)
                        ? (object)DBNull.Value
                        : matCode.Trim()
                });

                // Status parameter
                cmd.Parameters.Add(new OracleParameter("status", OracleDbType.Int32)
                {
                    Value = status.HasValue
                        ? (object)status.Value
                        : DBNull.Value
                });

                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new MaterialMasterModel
                        {
                            MatCd = reader["mat_cd"] == DBNull.Value ? null : reader["mat_cd"].ToString(),
                            MatNm = reader["mat_nm"] == DBNull.Value ? null : reader["mat_nm"].ToString(),
                            MajUom = reader["maj_uom"] == DBNull.Value ? null : reader["maj_uom"].ToString(),
                            UnitPrice = reader["unit_price"] == DBNull.Value
                                ? (decimal?)null
                                : Convert.ToDecimal(reader["unit_price"]),
                            Status = reader["status"] == DBNull.Value ? null : reader["status"].ToString()
                        });
                    }
                }
            }

            return result;
        }
    }
}