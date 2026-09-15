using System;
using System.Collections.Generic;
using Oracle.ManagedDataAccess.Client;
using MISReports_Api.Models.DgmDashboard;

namespace MISReports_Api.DAL.DgmDashboard
{
    public class DgmPivTotalDao
    {
        private static readonly string ConnectionString = System.Configuration.ConfigurationManager
            .ConnectionStrings["HQOracle"].ConnectionString;

        public List<DgmPivTotalModel> Fetch(string companyId, string startDate = null, string endDate = null)
        {
            var result = new List<DgmPivTotalModel>();

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
                    select c.paid_date as PIV_Date, sum(c.grand_total) as PIV_collection
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
                    )
                    group by c.paid_date 
                    order by c.paid_date desc";

                using (OracleCommand cmd = new OracleCommand(query, conn))
                {
                    cmd.BindByName = true;
                    cmd.Parameters.Add(new OracleParameter("startDate", sDateStr));
                    cmd.Parameters.Add(new OracleParameter("endDate", eDateStr));
                    cmd.Parameters.Add(new OracleParameter("companyId1", companyId));
                    cmd.Parameters.Add(new OracleParameter("companyId2", companyId));
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            result.Add(new DgmPivTotalModel
                            {
                                date = reader.IsDBNull(0) ? string.Empty : reader.GetDateTime(0).ToString("yyyy-MM-dd"),
                                amount = reader.IsDBNull(1) ? 0 : Convert.ToDouble(reader.GetValue(1))
                            });
                        }
                    }
                }
            }

            return result;
        }
    }
}
