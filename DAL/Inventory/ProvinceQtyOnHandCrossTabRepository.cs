using MISReports_Api.Models.Inventory;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;

namespace MISReports_Api.DAL.Inventory
{
    public class ProvinceQtyOnHandCrossTabRepository
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public async Task<List<ProvinceQtyOnHandCrossTabModel>> GetReportAsync(
            string compId,
            string matcode)
        {
            var list = new List<ProvinceQtyOnHandCrossTabModel>();

            // null-guard: treat null/missing matcode as empty string
            var safeMatcode = (matcode ?? "").Trim();

            using (OracleConnection conn = new OracleConnection(_connectionString))
            {
                await conn.OpenAsync();

                // Each :placeholder name is UNIQUE and matches the parameter added below
                string query = @"
select 
T1.MAT_CD,
T2.MAT_NM,
SUM(T1.QTY_ON_HAND) as commited_cost,
(T1.dept_id||'-'|| 
(select dept_nm
 from gldeptm
 where T1.DEPT_ID = dept_id)) as c8,
(select comp_nm
 from glcompm
 where comp_id in
 (select comp_id
  from gldeptm
  where dept_id = T1.DEPT_ID)) as area,
T1.UOM_CD,
(select comp_nm
 from glcompm
 where trim(comp_id) = :compId1) as region
from INMATM T2,
     INWRHMTM T1
where T2.MAT_CD = T1.MAT_CD
and T1.QTY_ON_HAND > 0
and T1.DEPT_ID IN
(
  select dept_id
  from gldeptm
  where comp_id IN
  (
    select comp_id
    from glcompm
    where trim(comp_id) = :compId2
    or trim(parent_id) = :compId3
  )
)
and trim(T1.mat_cd) like :matcode || '%'
and T1.GRADE_CD = 'NEW'
and T1.status = 2
group by
T1.MAT_CD,
T2.MAT_NM,
T1.UOM_CD,
T1.dept_id
order by
1 asc, 2 asc, 5 asc, 4 asc";

                using (OracleCommand cmd = new OracleCommand(query, conn))
                {
                    cmd.BindByName = true;   // match by NAME not position

                    cmd.Parameters.Add("compId1", OracleDbType.Varchar2).Value = compId.Trim();
                    cmd.Parameters.Add("compId2", OracleDbType.Varchar2).Value = compId.Trim();
                    cmd.Parameters.Add("compId3", OracleDbType.Varchar2).Value = compId.Trim();
                    cmd.Parameters.Add("matcode", OracleDbType.Varchar2).Value = safeMatcode;

                    using (OracleDataReader reader = (OracleDataReader)await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            list.Add(new ProvinceQtyOnHandCrossTabModel
                            {
                                MAT_CD = reader["MAT_CD"]?.ToString().Trim(),
                                MAT_NM = reader["MAT_NM"]?.ToString().Trim(),
                                Committed_Cost = reader["commited_cost"] == DBNull.Value
                                                 ? 0
                                                 : Convert.ToDecimal(reader["commited_cost"]),
                                C8 = reader["c8"]?.ToString().Trim(),
                                Area = reader["area"]?.ToString().Trim(),
                                UOM_CD = reader["UOM_CD"]?.ToString().Trim(),
                                Region = reader["region"]?.ToString().Trim()
                            });
                        }
                    }
                }
            }

            return list;
        }
    }
}