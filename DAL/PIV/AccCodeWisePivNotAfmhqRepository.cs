using MISReports_Api.Models.PIV;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;

namespace MISReports_Api.DAL.PIV
{
    public class AccCodeWisePivNotAfmhqRepository
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<AccCodeWisePivNotAfmhqModel> GetAccCodeWisePivNotAfmhq(
            DateTime fromDate,
            DateTime toDate,
            string costctr)
        {
            var result = new List<AccCodeWisePivNotAfmhqModel>();

            string sql = @"
SELECT DISTINCT
    (CASE
        WHEN (b.comp_id = 'GENE' OR b.parent_id = 'GENE' OR b.grp_comp = 'GENE') THEN 'GENE'
        WHEN (b.comp_id = 'TRAN' OR b.parent_id = 'TRANS' OR b.grp_comp = 'TRANS') THEN 'TRANS'
        WHEN (b.comp_id = 'AGMAM' OR b.parent_id = 'AGMAM' OR b.grp_comp = 'AGMAM') THEN 'AGMAM'
        WHEN (b.comp_id = 'AGMPRJ' OR b.parent_id = 'AGMPRJ' OR b.grp_comp = 'AGMPRJ') THEN 'AGMPRJ'
        WHEN (b.comp_id = 'DISCO1' OR b.parent_id = 'DISCO1' OR b.grp_comp = 'DISCO1') THEN 'DISCO1'
        WHEN (b.comp_id = 'DISCO2' OR b.parent_id = 'DISCO2' OR b.grp_comp = 'DISCO2') THEN 'DISCO2'
        WHEN (b.comp_id = 'DISCO3' OR b.parent_id = 'DISCO3' OR b.grp_comp = 'DISCO3') THEN 'DISCO3'
        WHEN (b.comp_id = 'DISCO4' OR b.parent_id = 'DISCO4' OR b.grp_comp = 'DISCO4') THEN 'DISCO4'
        ELSE b.comp_id
    END) AS company,
    c.dept_id,
    c.piv_no,
    c.piv_receipt_no,
    c.piv_date,
    c.paid_date,
    a.account_code,
    a.amount,
    (SELECT dept_nm FROM gldeptm WHERE dept_id = c.dept_id) AS CCT_NAME,
    (SELECT dept_nm FROM gldeptm WHERE dept_id = :costctr) AS CCT_NAME1
FROM piv_amount a
JOIN piv_detail c ON c.PIV_NO = a.PIV_NO AND a.dept_id = c.dept_id
JOIN gldeptm a1 ON a1.dept_id = c.dept_id
JOIN glcompm b   ON a1.comp_id = b.comp_id
                 AND b.status = 2
                 AND a1.status = 2
                 AND b.lvl_no < 90
WHERE trim(c.status) IN ('Q', 'P', 'F', 'FR', 'FA')
  AND c.paid_dept_id = :costctr
  AND c.paid_date >= TO_DATE(:fromDate, 'yyyy/mm/dd')
  AND c.paid_date <= TO_DATE(:toDate, 'yyyy/mm/dd')
  AND a.amount != 0
  AND c.dept_id IN (SELECT dept_id FROM gldeptm WHERE comp_id != 'AFMHQ')
  AND b.comp_id IN (
      SELECT comp_id FROM glcompm
      WHERE comp_id IN ('TRANS','GENE','DISCO1','DISCO2','DISCO3','DISCO4','AGMAM','AGMPRJ')
         OR parent_id IN ('TRANS','GENE','DISCO1','DISCO2','DISCO3','DISCO4','AGMAM','AGMPRJ')
         OR grp_comp IN ('TRANS','GENE','DISCO1','DISCO2','DISCO3','DISCO4','AGMAM','AGMPRJ')
  )
ORDER BY company, c.dept_id, a.account_code, c.piv_no, c.piv_receipt_no, c.piv_date, a.amount
";

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("fromDate", OracleDbType.Varchar2).Value = fromDate.ToString("yyyy/MM/dd");
                cmd.Parameters.Add("toDate", OracleDbType.Varchar2).Value = toDate.ToString("yyyy/MM/dd");
                cmd.Parameters.Add("costctr", OracleDbType.Varchar2).Value = costctr?.Trim() ?? "";

                try
                {
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new AccCodeWisePivNotAfmhqModel
                            {
                                Company = reader["company"]?.ToString(),
                                DeptId = reader["dept_id"]?.ToString(),
                                PivNo = reader["piv_no"]?.ToString(),
                                PivReceiptNo = reader["piv_receipt_no"]?.ToString(),
                                PivDate = reader["piv_date"] == DBNull.Value ? null : (DateTime?)reader.GetDateTime(reader.GetOrdinal("piv_date")),
                                PaidDate = reader["paid_date"] == DBNull.Value ? null : (DateTime?)reader.GetDateTime(reader.GetOrdinal("paid_date")),
                                AccountCode = reader["account_code"]?.ToString(),
                                Amount = reader["amount"] == DBNull.Value ? null : (decimal?)reader.GetDecimal(reader.GetOrdinal("amount")),
                                CctName = reader["CCT_NAME"]?.ToString(),
                                CctName1 = reader["CCT_NAME1"]?.ToString()
                            };
                            result.Add(item);
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Error fetching PIV (non-AFMHQ companies) data: " + ex.Message, ex);
                }
            }

            return result;
        }
    }
}