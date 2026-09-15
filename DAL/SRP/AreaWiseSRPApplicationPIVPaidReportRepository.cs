using MISReports_Api.Models.PIV;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;

namespace MISReports_Api.DAL.PIV
{
    public class AreaWiseSRPApplicationPIVPaidReportRepository
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<AreaWiseSRPApplicationPIVPaidReportModel> GetReport(string compId, string fromDate, string toDate)
        {
            List<AreaWiseSRPApplicationPIVPaidReportModel> list = new List<AreaWiseSRPApplicationPIVPaidReportModel>();

            using (OracleConnection conn = new OracleConnection(_connectionString))
            {
                conn.Open();

                string query = @"
select 
(select grp_comp from glcompm where comp_id in (select comp_id from gldeptm where dept_id =a.dept_id)) AS Division,
(select parent_id from glcompm where comp_id in (select comp_id from gldeptm where dept_id =a.dept_id)) AS Province,
(select comp_id from gldeptm where dept_id =a.dept_id) AS Area,
(select dept_nm from gldeptm where dept_id =a.dept_id) AS CCT_NAME,
a.dept_id,a.Id_no,a.application_no,
(b.first_name||' '||b.last_name ) as Name,
(b.street_address||' '||b.suburb||' '||b.city) as address,
a.submit_date,
a.description,
c.Piv_no,
c.Paid_date,
c.Piv_amount,
d.tariff_code,
d.phase,
d.existing_acc_no
from applications a, applicant b, wiring_land_detail d, piv_detail c
where b.Id_no= a.Id_no
and a.dept_id=c.dept_id
and trim(c.status) in ('P')
and a.status not in ('D')
and trim(a.application_no)=trim(c.reference_no)
and a.application_id=d.application_id
and a.dept_id=d.dept_id
and a.application_type='CR'
and application_sub_type='RS'
and c.reference_type='APP'
AND A.DEPT_ID IN (
    select dept_id from gldeptm where status=2 
    and comp_id in (
        select comp_id from glcompm
        where status=2 and comp_id = :compId
    )
)
and c.piv_date >= TO_DATE(:fromDate,'yyyy/mm/dd')
and c.piv_date <= TO_DATE(:toDate,'yyyy/mm/dd')
order by 1,2,3";

                using (OracleCommand cmd = new OracleCommand(query, conn))
                {
                    cmd.Parameters.Add(new OracleParameter("compId", compId));
                    cmd.Parameters.Add(new OracleParameter("fromDate", fromDate));
                    cmd.Parameters.Add(new OracleParameter("toDate", toDate));

                    OracleDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        list.Add(new AreaWiseSRPApplicationPIVPaidReportModel
                        {
                            Division = reader["Division"]?.ToString(),
                            Province = reader["Province"]?.ToString(),
                            Area = reader["Area"]?.ToString(),
                            CCT_NAME = reader["CCT_NAME"]?.ToString(),
                            DeptId = reader["dept_id"]?.ToString(),
                            IdNo = reader["Id_no"]?.ToString(),
                            ApplicationNo = reader["application_no"]?.ToString(),
                            Name = reader["Name"]?.ToString(),
                            Address = reader["address"]?.ToString(),
                            Description = reader["description"]?.ToString(),
                            PivNo = reader["Piv_no"]?.ToString(),
                            TariffCode = reader["tariff_code"]?.ToString(),
                            Phase = reader["phase"]?.ToString(),
                            ExistingAccNo = reader["existing_acc_no"]?.ToString(),
                            PivAmount = reader["Piv_amount"] == DBNull.Value ? null : (decimal?)Convert.ToDecimal(reader["Piv_amount"]),
                            SubmitDate = reader["submit_date"] == DBNull.Value ? null : (DateTime?)Convert.ToDateTime(reader["submit_date"]),
                            PaidDate = reader["Paid_date"] == DBNull.Value ? null : (DateTime?)Convert.ToDateTime(reader["Paid_date"])
                        });
                    }
                }
            }

            return list;
        }
    }
}