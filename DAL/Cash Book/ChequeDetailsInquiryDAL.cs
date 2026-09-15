using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using MISReports_Api.Models.Accounts;
namespace MISReports_Api.DAL
{
    public class ChequeDetailsInquiryDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<ChequeDetailsInquiryModel> GetChequeDetailsInquiry(string costCtr, string fromNo, string toNo)
        {
            var result = new List<ChequeDetailsInquiryModel>();

            string query = @"
        SELECT T3.chq_run, T1.chq_dt, T1.payee, T1.pymt_docno, T1.chq_no,
               T3.run_by, T3.modi_by, T3.apprv_uid1,
               (CASE WHEN T3.status = 1 THEN 'Approved  Payment Plan'
                     WHEN T3.status = 3 THEN 'Cheque printed'
                     WHEN T3.status = 5 THEN 'Transfer to GL'
                     WHEN T3.status = 7 THEN 'Cheque assignment Report'
                     WHEN T3.status = 8 THEN 'Confirmation'
                     ELSE NULL END) AS tranStatus,
               (SELECT dept_nm FROM gldeptm WHERE dept_id = :costctr) AS CCT_NAME
        FROM CBCHQDMT T1, CBCHQHMT T3
        WHERE T1.dept_id = T3.dept_id
          AND T1.bank_ac = T3.bank_ac
          AND T1.bank_cd = T3.bank_cd
          AND T1.chq_run = T3.chq_run
          AND T1.dept_id = :costctr
          AND TRIM(T1.chq_no) >= :fromno
          AND TRIM(T1.chq_no) <= :tono
        ORDER BY T3.chq_run, T1.pymt_docno";

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(query, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.BindByName = true;
                cmd.Parameters.Add(new OracleParameter("costctr", OracleDbType.Varchar2) { Value = costCtr });
                cmd.Parameters.Add(new OracleParameter("fromno", OracleDbType.Varchar2) { Value = fromNo });
                cmd.Parameters.Add(new OracleParameter("tono", OracleDbType.Varchar2) { Value = toNo });

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new ChequeDetailsInquiryModel
                        {
                            ChqRun = reader["chq_run"] == DBNull.Value ? null : reader["chq_run"].ToString(),
                            ChqDt = reader["chq_dt"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["chq_dt"]),
                            Payee = reader["payee"] == DBNull.Value ? null : reader["payee"].ToString(),
                            PymtDocNo = reader["pymt_docno"] == DBNull.Value ? null : reader["pymt_docno"].ToString(),
                            ChqNo = reader["chq_no"] == DBNull.Value ? null : reader["chq_no"].ToString(),
                            RunBy = reader["run_by"] == DBNull.Value ? null : reader["run_by"].ToString(),
                            ModiBy = reader["modi_by"] == DBNull.Value ? null : reader["modi_by"].ToString(),
                            ApprvUid1 = reader["apprv_uid1"] == DBNull.Value ? null : reader["apprv_uid1"].ToString(),
                            TranStatus = reader["tranStatus"] == DBNull.Value ? null : reader["tranStatus"].ToString(),
                            BranchName = reader["CCT_NAME"] == DBNull.Value ? null : reader["CCT_NAME"].ToString()
                        });
                    }
                }
            }
            return result;
        }
    }
}