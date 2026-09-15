using System;
using System.Collections.Generic;
using Oracle.ManagedDataAccess.Client;
using MISReports_Api.Models.FinancialDashboard;

namespace MISReports_Api.DAL.FinancialDashboard
{
    public class PivDivisionDao
    {
        private static readonly string ConnectionString = System.Configuration.ConfigurationManager
            .ConnectionStrings["HQOracle"].ConnectionString;

        public List<PivDivisionModel> Fetch()
        {
            var result = new List<PivDivisionModel>();

            using (OracleConnection conn = new OracleConnection(ConnectionString))
            {
                conn.Open();
                string query = @"
                    select distinct c.paid_date as PIV_Date,
                        (Case 
                            when b.comp_id in ( 'DISCO1','DISCO2','DISCO3','DISCO4','AFMHQ') then substr(b.comp_id,0,1)||substr(b.comp_id,6,1)
                            when b.parent_id in ( 'DISCO1','DISCO2','DISCO3','DISCO4','AFMHQ') then substr(b.parent_id,0,1)||substr(b.parent_id,6,1)
                            when b.grp_comp in ( 'DISCO1','DISCO2','DISCO3','DISCO4','AFMHQ') then substr(b.grp_comp,0,1)||substr(b.grp_comp,6,1)
                            else '' 
                        end ) as Company,
                        sum(c.grand_total) as PIV_collection
                    from piv_detail c, gldeptm a, glcompm b 
                    where trim(c.status) in ('Q', 'P','F','FR','FA')
                    and c.paid_date >= ( SELECT TO_DATE((SYSDATE - 7),'dd/mm/yy')  FROM dual ) 
                    and c.paid_date <= ( SELECT TO_DATE((SYSDATE - 1),'dd/mm/yy')  FROM dual )
                    and a.comp_id = b.comp_id
                    and c.dept_id = a.dept_id
                    and a.status = 2
                    and b.status = 2
                    and c.dept_id in (
                        select dept_id from gldeptm where status = 2 and comp_id in (
                            select comp_id from glcompm
                            where status = 2 and ((comp_id like 'DISCO%' or parent_id like 'DISCO%' or grp_comp like 'DISCO%') or (comp_id like 'AFMHQ%' or parent_id like 'AFMHQ%' or grp_comp like 'AFMHQ%'))
                        )
                    )
                    group by c.paid_date, 
                        (Case 
                            when b.comp_id in ( 'DISCO1','DISCO2','DISCO3','DISCO4','AFMHQ') then substr(b.comp_id,0,1)||substr(b.comp_id,6,1)
                            when b.parent_id in ( 'DISCO1','DISCO2','DISCO3','DISCO4','AFMHQ') then substr(b.parent_id,0,1)||substr(b.parent_id,6,1)
                            when b.grp_comp in ( 'DISCO1','DISCO2','DISCO3','DISCO4','AFMHQ') then substr(b.grp_comp,0,1)||substr(b.grp_comp,6,1)
                            else ''  
                        end )
                    order by c.paid_date desc, 
                        (Case 
                            when b.comp_id in ( 'DISCO1','DISCO2','DISCO3','DISCO4','AFMHQ') then substr(b.comp_id,0,1)||substr(b.comp_id,6,1)
                            when b.parent_id in ( 'DISCO1','DISCO2','DISCO3','DISCO4','AFMHQ') then substr(b.parent_id,0,1)||substr(b.parent_id,6,1)
                            when b.grp_comp in ( 'DISCO1','DISCO2','DISCO3','DISCO4','AFMHQ') then substr(b.grp_comp,0,1)||substr(b.grp_comp,6,1)
                            else '' 
                        end )";

                using (OracleCommand cmd = new OracleCommand(query, conn))
                using (OracleDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new PivDivisionModel
                        {   
                            date = reader.IsDBNull(0) ? string.Empty : reader.GetDateTime(0).ToString("yyyy-MM-dd"),
                            company = reader.IsDBNull(1) ? "Other" : reader.GetString(1),
                            amount = reader.IsDBNull(2) ? 0 : Convert.ToDouble(reader.GetValue(2))
                        });
                    }
                }
            }

            return result;
        }
    }
}
