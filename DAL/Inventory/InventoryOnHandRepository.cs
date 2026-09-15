using MISReports_Api.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MISReports_Api.DAL
{
    public class InventoryOnHandRepository
    {
        public async Task<List<InventoryOnHandModel>> GetInventoryOnHand(string deptId, string matCode = null)
        {
            var resultList = new List<InventoryOnHandModel>();
            Exception lastException = null;

            Debug.WriteLine($"GetInventoryOnHand started for deptId: {deptId}, matCode: {matCode}");

            string[] connectionStringNames = { "HQOracle" };

            foreach (var connectionStringName in connectionStringNames)
            {
                try
                {
                    string connectionString = ConfigurationManager.ConnectionStrings[connectionStringName]?.ConnectionString;
                    if (string.IsNullOrEmpty(connectionString)) continue;

                    using (var conn = new OracleConnection(connectionString))
                    {
                        await conn.OpenAsync();

                        string sql = @"
SELECT
    A.mat_cd       AS MAT_CD,
    D.mat_nm       AS MAT_NM,
    A.grade_cd     AS GRD_CD,
    D.maj_uom      AS MAJ_UOM,
    SUM(A.qty_alocatd) AS ALLOCATED,
    SUM(A.qty_on_hand) AS QTY_ON_HAND,
    A.unit_price       AS UNIT_PRICE,
    SUM(A.unit_price * A.qty_on_hand) AS VALUE,
    (SELECT dept_nm FROM gldeptm WHERE dept_id = :deptId) AS CCT_NAME
FROM
    inwrhmtm A
INNER JOIN
    inmatm D
    ON A.mat_cd = D.mat_cd
WHERE
    A.dept_id = :deptId
    AND (:matCode IS NULL OR :matCode = '' OR A.mat_cd LIKE :matCode || '%')
    AND A.status IN (2, 7)
GROUP BY
    A.mat_cd,
    D.mat_nm,
    A.grade_cd,
    D.maj_uom,
    A.unit_price
ORDER BY
    A.mat_cd";

                        using (var cmd = new OracleCommand(sql, conn))
                        {
                            cmd.BindByName = true;
                            cmd.Parameters.Add("deptId", deptId);
                            cmd.Parameters.Add("matCode", string.IsNullOrEmpty(matCode) ? DBNull.Value : (object)matCode);

                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    resultList.Add(new InventoryOnHandModel
                                    {
                                        MatCd = SafeGetString(reader, "MAT_CD"),
                                        MatNm = SafeGetString(reader, "MAT_NM"),
                                        GrdCd = SafeGetString(reader, "GRD_CD"),
                                        MajUom = SafeGetString(reader, "MAJ_UOM"),
                                        Alocated = SafeGetDecimal(reader, "ALLOCATED"),
                                        QtyOnHand = SafeGetDecimal(reader, "QTY_ON_HAND"),
                                        UnitPrice = SafeGetDecimal(reader, "UNIT_PRICE"),
                                        Value = SafeGetDecimal(reader, "VALUE"),
                                        CctName = SafeGetString(reader, "CCT_NAME")
                                    });
                                }
                            }
                        }

                        return resultList;
                    }
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    continue;
                }
            }

            if (lastException != null)
                throw new Exception("All DB connections failed", lastException);

            return resultList;
        }

        private string SafeGetString(OracleDataReader reader, string columnName)
        {
            try
            {
                int idx = reader.GetOrdinal(columnName);
                return reader.IsDBNull(idx) ? null : reader.GetString(idx);
            }
            catch { return null; }
        }

        private decimal SafeGetDecimal(OracleDataReader reader, string columnName)
        {
            try
            {
                int idx = reader.GetOrdinal(columnName);
                return reader.IsDBNull(idx) ? 0 : reader.GetDecimal(idx);
            }
            catch { return 0; }
        }
    }
}