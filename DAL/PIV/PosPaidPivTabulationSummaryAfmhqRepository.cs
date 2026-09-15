//19. POS Paid PIV Tabulation Summary Report (AFMHQ)

// PosPaidPivTabulationSummaryAfmhqRepository.cs
using MISReports_Api.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;

namespace MISReports_Api.Repositories
{
    public class PosPaidPivTabulationSummaryAfmhqRepository
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<PosPaidPivTabulationSummaryAfmhqModel> GetPosPaidPivTabulationSummaryAfmhq(
            string costCtr,
            DateTime fromDate,
            DateTime toDate)
        {
            var result = new List<PosPaidPivTabulationSummaryAfmhqModel>();

            string sql = @"
SELECT
    (CASE
        WHEN (b.comp_id = 'GENE' OR b.parent_id = 'GENE' OR b.grp_comp = 'GENE')
            THEN 'Division - Generation'
        WHEN (b.comp_id = 'TRAN' OR b.parent_id = 'TRANS' OR b.grp_comp = 'TRANS')
            THEN 'Division - Transmission'
        WHEN (b.comp_id = 'AGMAM' OR b.parent_id = 'AGMAM' OR b.grp_comp = 'AGMAM')
            THEN 'Division - Assest Management'
        WHEN (b.comp_id = 'AGMPRJ' OR b.parent_id = 'AGMPRJ' OR b.grp_comp = 'AGMPRJ')
            THEN 'Division - Projects'
        WHEN (b.comp_id = 'AFMHQ' OR b.parent_id = 'AFMHQ' OR b.grp_comp = 'AFMHQ')
            THEN 'Division - Head Quarters'
        WHEN (b.comp_id LIKE 'DISCO%')
            THEN 'Cost Center -' || c.dept_id
        WHEN (b.parent_id LIKE 'DISCO%')
            THEN 'Branch -' || b.comp_id
        WHEN (b.grp_comp LIKE 'DISCO%')
            THEN 'Branch -' || b.parent_id
        ELSE 'UNKNOWN'
    END) AS Company,

    a.account_code,
    a.account_code AS c8,
    SUM(a.amount) AS amount,

    (SELECT dept_nm
     FROM gldeptm
     WHERE dept_id = :costctr) AS CCT_NAME1

FROM piv_amount a
JOIN piv_detail c ON c.PIV_NO = a.PIV_NO AND a.dept_id = c.dept_id
JOIN gldeptm a1 ON c.dept_id = a1.dept_id AND a1.status = 2
JOIN glcompm b   ON a1.comp_id = b.comp_id AND b.status = 2 AND b.lvl_no < 90

WHERE TRIM(c.status) IN ('Q', 'P', 'F', 'FR', 'FA')
  AND c.paid_dept_id = :costctr
  AND c.paid_date >= TO_DATE(:fromDate, 'yyyy/mm/dd')
  AND c.paid_date <= TO_DATE(:toDate, 'yyyy/mm/dd')

  AND b.comp_id IN (
      SELECT comp_id FROM glcompm
      WHERE comp_id IN ('TRANS','GENE','DISCO1','DISCO2','DISCO3','DISCO4','AFMHQ','AGMAM','AGMPRJ','GENE')
         OR parent_id IN ('TRANS','GENE','DISCO1','DISCO2','DISCO3','DISCO4','AFMHQ','AGMAM','AGMPRJ','GENE')
         OR grp_comp  IN ('TRANS','GENE','DISCO1','DISCO2','DISCO3','DISCO4','AFMHQ','AGMAM','AGMPRJ','GENE')
  )

GROUP BY
    (CASE
        WHEN (b.comp_id = 'GENE' OR b.parent_id = 'GENE' OR b.grp_comp = 'GENE') THEN 'Division - Generation'
        WHEN (b.comp_id = 'TRAN' OR b.parent_id = 'TRANS' OR b.grp_comp = 'TRANS') THEN 'Division - Transmission'
        WHEN (b.comp_id = 'AGMAM' OR b.parent_id = 'AGMAM' OR b.grp_comp = 'AGMAM') THEN 'Division - Assest Management'
        WHEN (b.comp_id = 'AGMPRJ' OR b.parent_id = 'AGMPRJ' OR b.grp_comp = 'AGMPRJ') THEN 'Division - Projects'
        WHEN (b.comp_id = 'AFMHQ' OR b.parent_id = 'AFMHQ' OR b.grp_comp = 'AFMHQ') THEN 'Division - Head Quarters'
        WHEN (b.comp_id LIKE 'DISCO%') THEN 'Cost Center -' || c.dept_id
        WHEN (b.parent_id LIKE 'DISCO%') THEN 'Branch -' || b.comp_id
        WHEN (b.grp_comp LIKE 'DISCO%') THEN 'Branch -' || b.parent_id
        ELSE 'UNKNOWN'
    END),
    a.account_code

ORDER BY
    (CASE
        WHEN (b.comp_id = 'GENE' OR b.parent_id = 'GENE' OR b.grp_comp = 'GENE') THEN 'Division - Generation'
        WHEN (b.comp_id = 'TRAN' OR b.parent_id = 'TRANS' OR b.grp_comp = 'TRANS') THEN 'Division - Transmission'
        WHEN (b.comp_id = 'AGMAM' OR b.parent_id = 'AGMAM' OR b.grp_comp = 'AGMAM') THEN 'Division - Assest Management'
        WHEN (b.comp_id = 'AGMPRJ' OR b.parent_id = 'AGMPRJ' OR b.grp_comp = 'AGMPRJ') THEN 'Division - Projects'
        WHEN (b.comp_id = 'AFMHQ' OR b.parent_id = 'AFMHQ' OR b.grp_comp = 'AFMHQ') THEN 'Division - Head Quarters'
        WHEN (b.comp_id LIKE 'DISCO%') THEN 'Cost Center -' || c.dept_id
        WHEN (b.parent_id LIKE 'DISCO%') THEN 'Branch -' || b.comp_id
        WHEN (b.grp_comp LIKE 'DISCO%') THEN 'Branch -' || b.parent_id
        ELSE 'UNKNOWN'
    END),
    a.account_code";

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("costctr", OracleDbType.Varchar2).Value = costCtr?.Trim() ?? "";
                cmd.Parameters.Add("fromDate", OracleDbType.Varchar2).Value = fromDate.ToString("yyyy/MM/dd");
                cmd.Parameters.Add("toDate", OracleDbType.Varchar2).Value = toDate.ToString("yyyy/MM/dd");

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new PosPaidPivTabulationSummaryAfmhqModel
                        {
                            Company = reader["Company"].ToString(),
                            Account_Code = reader["account_code"].ToString(),
                            C8 = reader["c8"].ToString(),
                            Amount = reader["amount"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["amount"]),
                            CCT_NAME1 = reader["CCT_NAME1"].ToString()
                        });
                    }
                }
            }

            return result;
        }
    }
}