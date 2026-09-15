using MISReports_Api.Models.PIV;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;

namespace MISReports_Api.DAL.PIV
{
    public class TypewisePIVRepository
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<TypewisePIVModel> GetTypewisePIVReport(string costctr, DateTime fromDate, DateTime toDate)
        {
            var result = new List<TypewisePIVModel>();

            string sql = @"
select
    c.dept_id,
    (select title_nm from gltitlm where title_cd = c.title_cd ) as title_cd,
    c.reference_no,
    c.piv_date,
    c.paid_date,
    c.piv_no ,
    c.cus_vat_no,
    a.name,
    a.address,
    a.telephone_no,
    a.collect_person_id ,
    a.collect_person_name,
    c.description,
    c.grand_total,
    a.vat_reg_no ,
    c.payment_mode,
    p.cheque_no,
    (select dept_nm from gldeptm where dept_id = :costctr) AS cct_name
from piv_applicant a , piv_detail c , piv_payment p
where c.PIV_NO = a.PIV_NO
    and c.PIV_NO = p.PIV_NO
    and trim(c.status) in ('Q', 'P','F','FR','FA')
    and c.dept_id = :costctr
    and c.paid_date >= TO_DATE( :fromDate,'yyyy/mm/dd')
    and c.paid_date <= TO_DATE( :toDate,'yyyy/mm/dd')
group by
    c.dept_id,
    c.title_cd,
    c.piv_no,
    c.reference_Type,
    c.reference_no,
    c.piv_date,
    c.paid_date,
    c.piv_no ,
    c.cus_vat_no,
    a.name,
    a.address,
    a.telephone_no,
    a.collect_person_id,
    a.collect_person_name,
    c.description,
    c.grand_total,
    a.vat_reg_no ,
    c.payment_mode,
    p.cheque_no
order by
    c.dept_id,
    c.title_cd,
    c.piv_no";

            using (OracleConnection conn = new OracleConnection(_connectionString))
            using (OracleCommand cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;

                cmd.Parameters.Add("costctr", OracleDbType.Varchar2).Value = costctr;
                cmd.Parameters.Add("fromDate", OracleDbType.Varchar2).Value = fromDate.ToString("yyyy/MM/dd");
                cmd.Parameters.Add("toDate", OracleDbType.Varchar2).Value = toDate.ToString("yyyy/MM/dd");

                conn.Open();
                using (OracleDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var model = new TypewisePIVModel
                        {
                            Dept_Id = reader["dept_id"].ToString(),
                            Title_Cd = reader["title_cd"].ToString(),
                            Reference_No = reader["reference_no"].ToString(),
                            Piv_Date = reader["piv_date"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["piv_date"]),
                            Paid_Date = reader["paid_date"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["paid_date"]),
                            Piv_No = reader["piv_no"].ToString(),
                            Cus_Vat_No = reader["cus_vat_no"].ToString(),
                            Name = reader["name"].ToString(),
                            Address = reader["address"].ToString(),
                            Telephone_No = reader["telephone_no"].ToString(),
                            Collect_Person_Id = reader["collect_person_id"].ToString(),
                            Collect_Person_Name = reader["collect_person_name"].ToString(),
                            Description = reader["description"].ToString(),
                            Grand_Total = reader["grand_total"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["grand_total"]),
                            Vat_Reg_No = reader["vat_reg_no"].ToString(),
                            Payment_Mode = reader["payment_mode"].ToString(),
                            Cheque_No = reader["cheque_no"].ToString(),
                            Cct_Name = reader["cct_name"].ToString()
                        };
                        result.Add(model);
                    }
                }
            }

            return result;
        }
    }
}