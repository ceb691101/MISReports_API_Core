using System;
using System.Collections.Generic;
using Oracle.ManagedDataAccess.Client;
using MISReports_Api.Models.FinancialDashboard;

namespace MISReports_Api.DAL.FinancialDashboard
{
    public class StockDivisionDao
    {
        private static readonly string ConnectionString = System.Configuration.ConfigurationManager
            .ConnectionStrings["HQOracle"].ConnectionString;

        public List<StockDivisionModel> Fetch()
        {
            var result = new List<StockDivisionModel>();

            using (OracleConnection conn = new OracleConnection(ConnectionString))
            {
                conn.Open();
                string query = @"
                    select
                        (case
                            when trim(b.comp_id) in ('DISCO1','DISCO2','DISCO3','DISCO4') then substr(trim(b.comp_id),1,1)||substr(trim(b.comp_id),6,1)
                            when trim(b.parent_id) in ('DISCO1','DISCO2','DISCO3','DISCO4') then substr(trim(b.parent_id),1,1)||substr(trim(b.parent_id),6,1)
                            when trim(b.grp_comp) in ('DISCO1','DISCO2','DISCO3','DISCO4') then substr(trim(b.grp_comp),1,1)||substr(trim(b.grp_comp),6,1)
                            else ''
                        end) as Company,
                        sum(c.qty_on_hand * c.unit_price) as Stock_value
                    from inwrhmtm c
                    join gldeptm a on c.dept_id = a.dept_id
                    join glcompm b on a.comp_id = b.comp_id
                    where c.status = 2
                    and c.grade_cd = 'NEW'
                    and a.status = 2
                    and b.status = 2
                    and (b.comp_id like 'DISCO%' or b.parent_id like 'DISCO%' or b.grp_comp like 'DISCO%')
                    group by
                        (case
                            when trim(b.comp_id) in ('DISCO1','DISCO2','DISCO3','DISCO4') then substr(trim(b.comp_id),1,1)||substr(trim(b.comp_id),6,1)
                            when trim(b.parent_id) in ('DISCO1','DISCO2','DISCO3','DISCO4') then substr(trim(b.parent_id),1,1)||substr(trim(b.parent_id),6,1)
                            when trim(b.grp_comp) in ('DISCO1','DISCO2','DISCO3','DISCO4') then substr(trim(b.grp_comp),1,1)||substr(trim(b.grp_comp),6,1)
                            else ''
                        end)
                    order by
                        (case
                            when trim(b.comp_id) in ('DISCO1','DISCO2','DISCO3','DISCO4') then substr(trim(b.comp_id),1,1)||substr(trim(b.comp_id),6,1)
                            when trim(b.parent_id) in ('DISCO1','DISCO2','DISCO3','DISCO4') then substr(trim(b.parent_id),1,1)||substr(trim(b.parent_id),6,1)
                            when trim(b.grp_comp) in ('DISCO1','DISCO2','DISCO3','DISCO4') then substr(trim(b.grp_comp),1,1)||substr(trim(b.grp_comp),6,1)
                            else ''
                        end)";

                using (OracleCommand cmd = new OracleCommand(query, conn))
                using (OracleDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new StockDivisionModel
                        {
                            company = reader.IsDBNull(0) ? "Other" : reader.GetString(0),
                            amount = reader.IsDBNull(1) ? 0 : Convert.ToDouble(reader.GetValue(1))
                        });
                    }
                }
            }

            return result;
        }
    }
}
