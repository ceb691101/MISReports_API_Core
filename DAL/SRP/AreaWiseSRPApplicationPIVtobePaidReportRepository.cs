using MISReports_Api.Models.PIV;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;

namespace MISReports_Api.DAL.PIV
{
    public class AreaWiseSRPApplicationPIVtobePaidReportRepository
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public async Task<List<AreaWiseSRPApplicationPIVtobePaidReportModel>> GetReportAsync(
            string compId, string fromDate, string toDate)
        {
            var list = new List<AreaWiseSRPApplicationPIVtobePaidReportModel>();

            using (OracleConnection conn = new OracleConnection(_connectionString))
            {
                await conn.OpenAsync();

                string query = @"
SELECT :compId as Division_Name,
(select comp_nm from glcompm where comp_id in
(case when b.lvl_no = 60 then b.comp_id else b.parent_id end)) as Province,
(select comp_nm from glcompm where comp_id in
(select comp_id from gldeptm where dept_id = a.dept_id)) as Area_nm,
(select dept_id || ' - ' || dept_nm from gldeptm where dept_id = a.dept_id) as dept_nm,
'Pending SRP Application PIV' as Category,
count(application_no) as No_of_pending_estimation,
(select comp_nm from glcompm where comp_id = :compId) as comp_nm
from applications a, gldeptm a1, glcompm b, wiring_land_detail d, piv_detail c
where a.dept_id = c.dept_id
and trim(c.status) in ('A')
and a.status not in ('D')
and trim(a.application_no) = trim(c.reference_no)
and a.application_id = d.application_id
and a.dept_id = d.dept_id
and a.application_type = 'CR'
and a.application_sub_type = 'RS'
and c.reference_type = 'APP'
AND A.DEPT_ID IN (
    select dept_id from gldeptm
    where status = 2 and comp_id in (
        select comp_id from glcompm
        where status = 2 and
        (comp_id = :compId or parent_id = :compId or grp_comp = :compId)
    )
)
and c.piv_date >= TO_DATE(:fromDate,'yyyy/mm/dd')
and c.piv_date <= TO_DATE(:toDate,'yyyy/mm/dd')
and a1.dept_id = a.dept_id
and b.status = 2
and a1.comp_id = b.comp_id
and b.lvl_no < 90
and a1.status = 2
group by (case when b.lvl_no = 60 then b.comp_id else b.parent_id end), a.dept_id
order by 1,2,3,4";

                using (OracleCommand cmd = new OracleCommand(query, conn))
                {
                    // IMPORTANT FIX
                    cmd.BindByName = true;

                    cmd.Parameters.Add("compId", OracleDbType.Varchar2).Value = compId;
                    cmd.Parameters.Add("fromDate", OracleDbType.Varchar2).Value = fromDate;
                    cmd.Parameters.Add("toDate", OracleDbType.Varchar2).Value = toDate;

                    using (OracleDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            list.Add(new AreaWiseSRPApplicationPIVtobePaidReportModel
                            {
                                Division_Name = reader["Division_Name"]?.ToString(),
                                Province = reader["Province"]?.ToString(),
                                Area_nm = reader["Area_nm"]?.ToString(),
                                Dept_nm = reader["dept_nm"]?.ToString(),
                                Category = reader["Category"]?.ToString(),
                                No_of_pending_estimation = reader["No_of_pending_estimation"] == DBNull.Value
                                    ? 0
                                    : Convert.ToInt32(reader["No_of_pending_estimation"]),
                                Comp_nm = reader["comp_nm"]?.ToString()
                            });
                        }
                    }
                }
            }

            return list;
        }
    }
}