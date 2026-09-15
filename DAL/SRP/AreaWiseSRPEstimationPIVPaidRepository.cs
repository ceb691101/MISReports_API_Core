using MISReports_Api.Models.PIV;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;

namespace MISReports_Api.DAL.PIV
{
    public class AreaWiseSRPEstimationPIVPaidRepository
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public async Task<List<AreaWiseSRPEstimationPIVPaidReportModel>> GetReportAsync(
            string compId,
            string fromDate,
            string toDate)
        {
            var list = new List<AreaWiseSRPEstimationPIVPaidReportModel>();

            using (OracleConnection conn = new OracleConnection(_connectionString))
            {
                await conn.OpenAsync();

                string query = @"
select 
(select grp_comp from glcompm where comp_id in 
    (select comp_id from gldeptm where dept_id = a.dept_id)) AS Division,

(select parent_id from glcompm where comp_id in 
    (select comp_id from gldeptm where dept_id = a.dept_id)) AS Province,

(select comp_id from gldeptm where dept_id = a.dept_id) AS Area,

(select dept_nm from gldeptm where dept_id = a.dept_id) AS CCT_NAME,

a.dept_id,
a.Id_no,
a.application_no,

(b.first_name || ' ' || b.last_name) as Name,
(b.street_address || ' ' || b.suburb || ' ' || b.city) as address,

a.submit_date,
a.description,

c.Piv_no,
c.Paid_date,
c.Piv_amount,

d.tariff_code,
d.phase,
d.existing_acc_no,

(select comp_nm from glcompm where comp_id = :compId) AS COMP_NM

from applications a,
     applicant b,
     wiring_land_detail d,
     APPLICATION_REFERENCE app,
     piv_detail c

where b.Id_no = a.Id_no
and a.dept_id = c.dept_id
and trim(c.status) in ('P')

and (trim(a.application_no) = trim(c.reference_no)
     or trim(app.projectno) = trim(c.reference_no))

and a.application_id = d.application_id
and a.application_no = app.application_no
and a.dept_id = d.dept_id

and a.application_type = 'CR'
and a.application_sub_type = 'RS'

and c.reference_type in ('EST','JOB')

AND A.DEPT_ID IN
(
select dept_id
from gldeptm
where status = 2
and comp_id in
(
select comp_id
from glcompm
where status = 2
and comp_id = :compId
)
)

and c.piv_date >= TO_DATE(:fromDate,'yyyy/mm/dd')
and c.piv_date <= TO_DATE(:toDate,'yyyy/mm/dd')

order by 1,2,3";

                using (OracleCommand cmd = new OracleCommand(query, conn))
                {
                    cmd.BindByName = true;

                    cmd.Parameters.Add("compId", OracleDbType.Varchar2).Value = compId;
                    cmd.Parameters.Add("fromDate", OracleDbType.Varchar2).Value = fromDate;
                    cmd.Parameters.Add("toDate", OracleDbType.Varchar2).Value = toDate;

                    using (OracleDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            list.Add(new AreaWiseSRPEstimationPIVPaidReportModel
                            {
                                Division = reader["Division"]?.ToString(),
                                Province = reader["Province"]?.ToString(),
                                Area = reader["Area"]?.ToString(),
                                CCT_NAME = reader["CCT_NAME"]?.ToString(),

                                Dept_Id = reader["dept_id"]?.ToString(),
                                Id_No = reader["Id_no"]?.ToString(),
                                Application_No = reader["application_no"]?.ToString(),

                                Name = reader["Name"]?.ToString(),
                                Address = reader["address"]?.ToString(),

                                Submit_Date = reader["submit_date"] == DBNull.Value
                                    ? null
                                    : (DateTime?)Convert.ToDateTime(reader["submit_date"]),

                                Description = reader["description"]?.ToString(),

                                Piv_No = reader["Piv_no"]?.ToString(),

                                Paid_Date = reader["Paid_date"] == DBNull.Value
                                    ? null
                                    : (DateTime?)Convert.ToDateTime(reader["Paid_date"]),

                                Piv_Amount = reader["Piv_amount"] == DBNull.Value
                                    ? null
                                    : (decimal?)Convert.ToDecimal(reader["Piv_amount"]),

                                Tariff_Code = reader["tariff_code"]?.ToString(),
                                Phase = reader["phase"]?.ToString(),
                                Existing_Acc_No = reader["existing_acc_no"]?.ToString(),

                                Comp_Nm = reader["COMP_NM"]?.ToString()
                            });
                        }
                    }
                }
            }

            return list;
        }
    }
}