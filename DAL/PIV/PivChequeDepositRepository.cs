//11. PIV Details for Cheque Deposits/ Cheque No

// File: PivChequeDepositRepository.cs
using MISReports_Api.Models.PIV;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;

namespace MISReports_Api.DAL.PIV
{
    public class PivChequeDepositRepository
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<PivChequeDepositModel> GetApplicationsByChequeNo(string chequeNo)
        {
            var result = new List<PivChequeDepositModel>();

            if (string.IsNullOrWhiteSpace(chequeNo))
                return result;

            string sql = @"
select a.dept_id,a.Id_no,a.application_no,(b.first_name||' '||b.last_name ) as Name,
       (d.service_street_address||' '||d.service_suburb||' '||d.service_city) as address,
       a.submit_date, a.description, c.Piv_no, c.Paid_date, c.Piv_amount, d.tariff_code, d.phase,
       to_char(c.cheque_no) as cheque_no,
       (select comp_id from gldeptm where dept_id =a.dept_id) AS Area,
       (select parent_id from glcompm where comp_id in (select comp_id from gldeptm where dept_id =a.dept_id)) AS province,
       (select dept_nm from gldeptm where dept_id =a.dept_id) AS CCT_NAME
from applications a, applicant b , piv_detail c, wiring_land_detail d
where b.Id_no= a.Id_no
  and a.dept_id=c.dept_id
  and trim(a.application_no)=trim(c.reference_no)
  and a.application_id=d.application_id
  and a.dept_id=d.dept_id
  and c.status in ('P','Q')
  and c.reference_type='EST'
  AND trim(to_char(c.cheque_no))=trim(:chequeNo)
union all
select a.dept_id,a.Id_no,a.application_no,(b.first_name||' '||b.last_name ) as Name,
       (d.service_street_address||' '||d.service_suburb||' '||d.service_city) as address,
       a.submit_date, a.description, c.Piv_no, c.Paid_date, c.Piv_amount, d.tariff_code, d.phase,
       piv.cheque_no as cheque_no ,
       (select comp_id from gldeptm where dept_id =a.dept_id) AS Area,
       (select parent_id from glcompm where comp_id in (select comp_id from gldeptm where dept_id =a.dept_id)) AS province,
       (select dept_nm from gldeptm where dept_id =a.dept_id) AS CCT_NAME
from applications a, applicant b , wiring_land_detail d, piv_detail c, piv_payment piv
where b.Id_no= a.Id_no
  and a.dept_id=c.dept_id
  and trim(a.application_no)=trim(c.reference_no)
  and a.application_id=d.application_id
  and a.dept_id=d.dept_id
  and c.piv_no=piv.piv_no
  and c.status in ('P','Q')
  and c.reference_type in ('EST','ELN')
  AND trim(piv.cheque_no) =trim(:chequeNo)
order by 1";

            using (OracleConnection conn = new OracleConnection(_connectionString))
            using (OracleCommand cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("chequeNo", OracleDbType.Varchar2).Value = chequeNo.Trim();

                conn.Open();
                using (OracleDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var model = new PivChequeDepositModel
                        {
                            Dept_Id = reader["dept_id"].ToString(),
                            Id_No = reader["Id_no"].ToString(),
                            Application_No = reader["application_no"].ToString(),
                            Name = reader["Name"].ToString(),
                            Address = reader["address"].ToString(),
                            Submit_Date = reader["submit_date"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["submit_date"]),
                            Description = reader["description"].ToString(),
                            Piv_No = reader["Piv_no"].ToString(),
                            Paid_Date = reader["Paid_date"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["Paid_date"]),
                            Piv_Amount = reader["Piv_amount"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["Piv_amount"]),
                            Tariff_Code = reader["tariff_code"].ToString(),
                            Phase = reader["phase"].ToString(),
                            Cheque_No = reader["cheque_no"].ToString(),
                            Area = reader["Area"].ToString(),
                            Province = reader["province"].ToString(),
                            CCT_NAME = reader["CCT_NAME"].ToString()
                        };
                        result.Add(model);
                    }
                }
            }

            return result;
        }
    }
}