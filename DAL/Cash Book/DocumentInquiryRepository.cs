using MISReports_Api.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;

namespace MISReports_Api.Repositories
{
    public class DocumentInquiryRepository
    {
        private readonly string _connectionString = ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public async Task<List<DocumentInquiryModel>> GetCashBookReportAsync(string costCenter, DateTime fromDate, DateTime toDate)
        {
            var result = new List<DocumentInquiryModel>();

            const string sql = @"
   SELECT DISTINCT
       'Cash Book- A' AS Category,
       C.doc_dt,
       C.non_taxabl,
       C.doc_no,
       (CASE
            WHEN C.status IN (1, 2) THEN ''
            ELSE C.apprv_uid1
        END) AS apprv_uid1,
       (CASE
            WHEN C.status IN (1, 2) THEN ''
            ELSE TO_CHAR(C.appr_dt, 'yyyy/mm/dd')
        END) AS appr_dt1,
       (CASE
            WHEN C.status = 1 THEN 'New'
            WHEN C.status = 2 THEN 'Send for Approval'
            WHEN C.status = 3 THEN 'Approved'
            WHEN C.status = 4 THEN 'Transfer to GL'
            WHEN C.status = 6 THEN 'To be cancelled'
            WHEN C.status = 5 THEN 'Cancelled Record'
            WHEN C.status = 7 THEN 'Payment Plan generated'
            WHEN C.status = 8 THEN 'GL Posted'
            ELSE NULL
        END) AS tranStatus,
       C.payee,
       TO_CHAR(A.chq_dt,'yyyy/mm/dd') AS chq_dt,
       A.chq_no,
       A.chq_run AS pymt_docno,

       (SELECT CASE
                   WHEN B.status = 1 THEN 'Approved Payment Plan'
                   WHEN B.status = 3 THEN 'Cheque printed'
                   WHEN B.status = 5 THEN 'Transfer to GL'
                   WHEN B.status = 7 THEN 'Cheque assignment Report'
                   WHEN B.status = 8 THEN 'Confirmation'
               ELSE NULL END
        FROM cbchqhmt B
        WHERE TRIM(A.chq_run) = TRIM(B.chq_run)

        UNION ALL

        SELECT CASE
                   WHEN B.status = 1 THEN 'Create Payment Plan'
                   WHEN B.status = 3 THEN 'Print PP Final report'
                   WHEN B.status = 4 THEN 'Edit Payment Plan'
                   WHEN B.status = 4 THEN 'Rejected'
                   WHEN B.status = 6 THEN 'Send for second approval'
                   WHEN B.status = 5 THEN 'Send PP for Approval'
               ELSE NULL END
        FROM cbchqhtt B
        WHERE TRIM(A.chq_run) = TRIM(B.chq_run)
          AND B.status <> 0
       ) AS PP_Status,

       (SELECT dept_nm
        FROM gldeptm
        WHERE dept_id = :costctr) AS CCT_NAME

FROM cbpmthmt C
LEFT OUTER JOIN cbchqdmt A
       ON A.pymt_docpf = C.doc_pf
      AND A.pymt_docno = C.doc_no
      AND TRIM(A.chq_run) = TRIM(C.chq_run)
      AND TRIM(A.chq_no)  = TRIM(C.chq_no)
WHERE C.dept_id = :costctr
  AND C.doc_dt >= TO_DATE(:fromDate, 'yyyy/mm/dd')
  AND C.doc_dt <= TO_DATE(:toDate, 'yyyy/mm/dd')

UNION ALL

SELECT DISTINCT
       'Cash Book-T' AS Category,
       doc_dt,
       non_taxabl,
       doc_no,
       (CASE
            WHEN status = 1 THEN ''
            WHEN status = 2 THEN '** To be Approved by ' || apprv_uid1
            ELSE apprv_uid1
        END) AS apprv_uid1,
       (CASE
            WHEN status IN (1, 2) THEN ''
            ELSE TO_CHAR(appr_dt, 'yyyy/mm/dd')
        END) AS appr_dt1,
       (CASE
            WHEN status = 1 THEN 'New Record'
            WHEN status = 2 THEN 'Send for 1st. Approval'
            WHEN status = 3 THEN 'Approved'
            WHEN status = 4 THEN 'Rejected'
            WHEN status = 6 THEN 'Send for second approval'
            WHEN status = 7 THEN 'Approved Once'
            WHEN status = 8 THEN 'Printed'
            WHEN status = 9 THEN 'Cancelled'
            ELSE NULL
        END) AS tranStatus,
       payee,
       '' AS chq_dt,
       '' AS chq_no,
       '' AS pymt_docno,
       '' AS PP_Status,
       (SELECT dept_nm FROM gldeptm WHERE dept_id = :costctr) AS CCT_NAME
FROM cbpmthtt
WHERE status <> 0
  AND dept_id = :costctr
  AND doc_dt >= TO_DATE(:fromDate, 'yyyy/mm/dd')
  AND doc_dt <= TO_DATE(:toDate, 'yyyy/mm/dd')

ORDER BY 1, 4
";

            using (var conn = new OracleConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new OracleCommand(sql, conn))
                {
                    cmd.BindByName = true;
                    cmd.Parameters.Add("costctr", OracleDbType.Varchar2).Value = costCenter;
                    cmd.Parameters.Add("fromDate", OracleDbType.Varchar2).Value = fromDate.ToString("yyyy/MM/dd");
                    cmd.Parameters.Add("toDate", OracleDbType.Varchar2).Value = toDate.ToString("yyyy/MM/dd");

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            result.Add(new DocumentInquiryModel
                            {
                                Category = reader["CATEGORY"].ToString(),
                                DocDt = reader["DOC_DT"] as DateTime?,
                                NonTaxabl = reader["NON_TAXABL"]?.ToString(),
                                DocNo = reader["DOC_NO"].ToString(),
                                ApprvUid1 = reader["APPRV_UID1"]?.ToString(),
                                ApprDt1 = reader["APPR_DT1"]?.ToString(),
                                TranStatus = reader["TRANSTATUS"]?.ToString(),
                                Payee = reader["PAYEE"]?.ToString(),
                                ChqDt = reader["CHQ_DT"]?.ToString(),
                                ChqNo = reader["CHQ_NO"]?.ToString(),
                                PymtDocno = reader["PYMT_DOCNO"]?.ToString(),
                                PpStatus = reader["PP_Status"]?.ToString(),
                                CctName = reader["CCT_NAME"]?.ToString()
                            });
                        }
                    }
                }
            }

            return result;
        }
    }
}