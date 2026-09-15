using System;
using Oracle.ManagedDataAccess.Client;

namespace MISReports_Api.DAL.FinancialDashboard
{
    public class StockTotalDao
    {
        private static readonly string ConnectionString = System.Configuration.ConfigurationManager
            .ConnectionStrings["HQOracle"].ConnectionString;

        public double Fetch()
        {
            double total = 0;

            using (OracleConnection conn = new OracleConnection(ConnectionString))
            {
                conn.Open();
                string query = @"
                    select distinct sum(c.qty_on_hand * c.unit_price) as Stock_value
                    from inwrhmtm c
                    where c.status = 2 and c.grade_cd = 'NEW'
                    and c.dept_id in (
                        select dept_id from gldeptm where status = 2 and comp_id in (
                            select comp_id from glcompm
                            where status = 2 and (comp_id like 'DISCO%' or parent_id like 'DISCO%' or grp_comp like 'DISCO%')
                        )
                    )";

                using (OracleCommand cmd = new OracleCommand(query, conn))
                using (OracleDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read() && !reader.IsDBNull(0))
                    {
                        total = Convert.ToDouble(reader.GetValue(0));
                    }
                }
            }

            return total;
        }
    }
}
