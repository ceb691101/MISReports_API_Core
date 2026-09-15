// Area - wise Solar Sent to Billing Details

using MISReports_Api.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;

namespace MISReports_Api.DAL
{
    public class SolarBillingRepository
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<SolarBillingModel> GetSolarBillingReport(string compId, DateTime fromDate, DateTime toDate)
        {
            var result = new List<SolarBillingModel>();

            string sql = @"
select
    a.dept_id,
    a.Id_no,
    a.application_no,
    a.application_sub_type,
    (b.first_name||' '||b.last_name ) as Name,
    (b.street_address||' '||b.suburb||' '||b.city) as address,
    a.submit_date,
    s.capacity,
    o.ACCOUNT_NO,
    o.EXPORTED_DATE,
    d.tariff_code,
    d.phase,
    o.project_no,
    s.agreement_date,
    s.BANK_ACCOUNT_NO,
    s.BANK_CODE,
    s.BRANCH_CODE,
    (select dept_nm from gldeptm where dept_id = a.dept_id) AS Cost_center_name,
    (select comp_nm from glcompm where Trim(comp_id) = :compId) AS Company_name
from applications a
    inner join applicant b on b.Id_no = a.Id_no
    inner join APPLICATION_REFERENCE app on a.application_id = app.application_id
    inner join wiring_land_detail d on a.application_id = d.application_id and a.dept_id = d.dept_id
    inner join banking_details s on app.projectno = s.job_no
    inner join SPEXPJOB o on trim(app.projectno) = trim(o.project_no) and s.dept_id = o.dept_id
where a.application_type = 'CR'
    and a.application_sub_type in ('NM','NP','NA','BM','BP','BA','NT','AC','PC','PP','PN','PB')
    and a.dept_id in (select dept_id from gldeptm where status = 2 and Trim(comp_id) = :compId)
    and o.EXPORTED_DATE >= TO_DATE(:fromDate, 'yyyy/mm/dd')
    and o.EXPORTED_DATE <= TO_DATE(:toDate, 'yyyy/mm/dd')
order by a.dept_id";

            using (OracleConnection conn = new OracleConnection(_connectionString))
            using (OracleCommand cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("compId", OracleDbType.Varchar2).Value = compId;
                cmd.Parameters.Add("fromDate", OracleDbType.Varchar2).Value = fromDate.ToString("yyyy/MM/dd");
                cmd.Parameters.Add("toDate", OracleDbType.Varchar2).Value = toDate.ToString("yyyy/MM/dd");

                conn.Open();
                using (OracleDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var model = new SolarBillingModel
                        {
                            Dept_Id = reader["dept_id"].ToString(),
                            Id_No = reader["Id_no"].ToString(),
                            Application_No = reader["application_no"].ToString(),
                            Application_Sub_Type = reader["application_sub_type"].ToString(),
                            Name = reader["Name"].ToString(),
                            Address = reader["address"].ToString(),
                            Submit_Date = reader["submit_date"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["submit_date"]),
                            Capacity = reader["capacity"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["capacity"]),
                            Account_No = reader["ACCOUNT_NO"].ToString(),
                            Exported_Date = reader["EXPORTED_DATE"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["EXPORTED_DATE"]),
                            Tariff_Code = reader["tariff_code"].ToString(),
                            Phase = reader["phase"].ToString(),
                            Project_No = reader["project_no"].ToString(),
                            Agreement_Date = reader["agreement_date"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["agreement_date"]),
                            Bank_Account_No = reader["BANK_ACCOUNT_NO"].ToString(),
                            Bank_Code = reader["BANK_CODE"].ToString(),
                            Branch_Code = reader["BRANCH_CODE"].ToString(),
                            Cost_Center_Name = reader["Cost_center_name"].ToString(),
                            Company_Name = reader["Company_name"].ToString()
                        };
                        result.Add(model);
                    }
                }
            }

            return result;
        }
    }
}