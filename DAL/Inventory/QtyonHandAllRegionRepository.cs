using MISReports_Api.Models.Inventory;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MISReports_Api.DAL.Inventory
{
    public class QtyonHandAllRegionRepository
    {
        private readonly string connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public async Task<List<QtyonHandAllRegionModel>> GetQtyonHandAllRegionAsync(string compId, string matcode)
        {
            var list = new List<QtyonHandAllRegionModel>();

            try
            {
                using (var conn = new OracleConnection(connectionString))
                {
                    await conn.OpenAsync();

                    string sql = @"
select T1.MAT_CD  ,
(select case  when  lvl_no = 60 then parent_id else Grp_comp  end  from glcompm where comp_id in (select comp_id from gldeptm where dept_id = T1.dept_id)) as region,
(select case  when  lvl_no = 60 then comp_id else parent_id  end  from glcompm where comp_id in (select comp_id from gldeptm where dept_id = T1.dept_id)) as c8,
T1.dept_id,
T2.MAT_NM,
T2.unit_price,
SUM(T1.QTY_ON_HAND) as commited_cost,
T1.UOM_CD
from INMATM T2,
INWRHMTM T1
where (T2.MAT_CD = T1.MAT_CD)
and T1.dept_id in (select dept_id from gldeptm where comp_id in (select comp_id from glcompm where status=2 and
( parent_id =:compId or Grp_comp = :compId or comp_id = :compId)))
and T1.mat_cd like :matcode ||'%'
and (T1.GRADE_CD = 'NEW')
and (T1.status = 2)
group by T1.MAT_CD,T2.MAT_NM,T1.UOM_CD,T1.dept_id,T2.unit_price
order by 1 asc, 2 asc, 3 asc, 4 asc";

                    using (var cmd = new OracleCommand(sql, conn))
                    {
                        cmd.BindByName = true;

                        cmd.Parameters.Add("compId", compId);
                        cmd.Parameters.Add("matcode", matcode);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                list.Add(new QtyonHandAllRegionModel
                                {
                                    MAT_CD = reader["MAT_CD"] != DBNull.Value ? reader["MAT_CD"].ToString() : null,
                                    REGION = reader["region"] != DBNull.Value ? reader["region"].ToString() : null,
                                    C8 = reader["c8"] != DBNull.Value ? reader["c8"].ToString() : null,
                                    DEPT_ID = reader["dept_id"] != DBNull.Value ? reader["dept_id"].ToString() : null,
                                    MAT_NM = reader["MAT_NM"] != DBNull.Value ? reader["MAT_NM"].ToString() : null,
                                    UNIT_PRICE = reader["unit_price"] != DBNull.Value ? Convert.ToDecimal(reader["unit_price"]) : 0,
                                    COMMITED_COST = reader["commited_cost"] != DBNull.Value ? Convert.ToDecimal(reader["commited_cost"]) : 0,
                                    UOM_CD = reader["UOM_CD"] != DBNull.Value ? reader["UOM_CD"].ToString() : null
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetQtyonHandAllRegionAsync: {ex.Message}");
                throw;
            }

            return list;
        }
    }
}