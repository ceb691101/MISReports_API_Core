using System;
using Oracle.ManagedDataAccess.Client;

namespace MISReports_Api.DAL.DgmDashboard
{
    public class DgmPivPeriodSummaryDao
    {
        private static readonly string ConnectionString = System.Configuration.ConfigurationManager
            .ConnectionStrings["HQOracle"].ConnectionString;

        public double Fetch(string companyId, string startDate, string endDate)
        {
            double total = 0;

            string sDateStr = string.IsNullOrWhiteSpace(startDate) 
                ? DateTime.Today.AddDays(-30).ToString("yyyy-MM-dd") 
                : startDate;
            string eDateStr = string.IsNullOrWhiteSpace(endDate) 
                ? DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd") 
                : endDate;

            using (OracleConnection conn = new OracleConnection(ConnectionString))
            {
                conn.Open();
                string query = @"
                    select sum(c.grand_total) as PIV_collection
                    from piv_detail c 
                    where trim(c.status) in ('Q', 'P','F','FR','FA')
                    and c.paid_date >= TO_DATE(:startDate, 'YYYY-MM-DD')
                    and c.paid_date <= TO_DATE(:endDate, 'YYYY-MM-DD')
                    and c.dept_id in (
                        select dept_id from gldeptm 
                        where status = 2 
                        and comp_id in (
                            select comp_id from glcompm
                            where status = 2 
                            and (TRIM(comp_id) = :companyId1 or TRIM(parent_id) = :companyId2)
                        )
                    )";

                using (OracleCommand cmd = new OracleCommand(query, conn))
                {
                    cmd.BindByName = true;
                    cmd.Parameters.Add(new OracleParameter("startDate", sDateStr));
                    cmd.Parameters.Add(new OracleParameter("endDate", eDateStr));
                    cmd.Parameters.Add(new OracleParameter("companyId1", companyId));
                    cmd.Parameters.Add(new OracleParameter("companyId2", companyId));

                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read() && !reader.IsDBNull(0))
                        {
                            total = Convert.ToDouble(reader.GetValue(0));
                        }
                    }
                }
            }

            return total;
        }
    }
}
