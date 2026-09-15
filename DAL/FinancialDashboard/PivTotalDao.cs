using System;
using System.Collections.Generic;
using Oracle.ManagedDataAccess.Client;
using MISReports_Api.Models.FinancialDashboard;

namespace MISReports_Api.DAL.FinancialDashboard
{
    public class PivTotalDao
    {
        private static readonly string ConnectionString = System.Configuration.ConfigurationManager
            .ConnectionStrings["HQOracle"].ConnectionString;

        public List<PivTotalModel> Fetch()
        {
            var result = new List<PivTotalModel>();

            using (OracleConnection conn = new OracleConnection(ConnectionString))
            {
                conn.Open();
                string query = @"
                    select distinct c.paid_date as PIV_Date, sum(c.grand_total) as PIV_collection
                    from piv_detail c 
                    where trim(c.status) in ('Q', 'P','F','FR','FA')
                    and c.paid_date >= ( SELECT TO_DATE((SYSDATE - 7),'dd/mm/yy')  FROM dual ) 
                    and c.paid_date <= ( SELECT TO_DATE((SYSDATE - 1),'dd/mm/yy')  FROM dual )
                    and c.dept_id in (
                        select dept_id from gldeptm where status = 2 and comp_id in (
                            select comp_id from glcompm
                            where status = 2 and ((comp_id like 'DISCO%' or parent_id like 'DISCO%' or grp_comp like 'DISCO%') or (comp_id like 'AFMHQ%' or parent_id like 'AFMHQ%' or grp_comp like 'AFMHQ%'))
                        )
                    )
                    group by c.paid_date 
                    order by c.paid_date desc";

                using (OracleCommand cmd = new OracleCommand(query, conn))
                using (OracleDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new PivTotalModel
                        {
                            date = reader.IsDBNull(0) ? string.Empty : reader.GetDateTime(0).ToString("yyyy-MM-dd"),
                            amount = reader.IsDBNull(1) ? 0 : Convert.ToDouble(reader.GetValue(1))
                        });
                    }
                }
            }

            return result;
        }
    }
}
