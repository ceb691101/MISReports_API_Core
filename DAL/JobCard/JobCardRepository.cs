using MISReports_Api.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MISReports_Api.DAL
{
    public class JobCardRepository
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public async Task<List<JobcardModel>> GetJobCardsAsync(string projectNo, string costCenter)
        {
            var jobCardList = new List<JobcardModel>();

            try
            {
                using (var conn = new OracleConnection(connectionString))
                {
                    await conn.OpenAsync();

                    string sql = @"
                        select T4.project_no  , (select  sum(T11.commited_cost)
                      from 	pcesthmt T22, pcestdmt T11
                      where
                     T22.estimate_no=T11.estimate_no and
                     T22.dept_id=T11.dept_id and
                       T22.project_no= T4.project_no  and  T22.dept_id =:costctr) as commited_cost,
                    	T4.descr    ,
	                    T4.std_cost  ,
	                    T4.fund_source,
                        T4.estimate_no,T4.prj_ass_dt,
	                    (CASE WHEN T4.Status = 1 then 'OPEN' WHEN T4.Status = 3 THEN 'TRANSFERED ON ' ||t4.conf_dt  WHEN T4.Status = 6 THEN 'TO BE APPROVED (CONSTRUCTION REVISED JOBS)'
                         WHEN T4.Status = 41 THEN 'REJECTED JOB'
                        WHEN T4.Status in(5, 7,25) THEN 'UNDER-REVISION'
                        WHEN T4.Status in(4) THEN 'SOFT-CLOSE'
                        WHEN T4.Status = 19 THEN 'EXISTING JOB ENTRY'
                        WHEN T4.Status = 22 THEN 'TO BE ALLOCATED TO CONTRACTOR'
                         WHEN T4.Status in(55,56,57,58,59) THEN 'TO BE APPROVED (DEPOT REVISED JOBS)'
                        WHEN T4.Status in(60) THEN 'REVISED JOB APPROVED.CONSUMER SHOULD BE PAY EXTRA AMOUNT'
                         WHEN T4.Status in(61) THEN 'To be Approved by CE (REVISED JOBS)'
                          ELSE 'UNKNOWN' END) as status,
                         T2.Log_yr ,
                       T2.Log_mth  ,
                      T2.doc_pf  ,
                     T2.doc_no  ,
                     T2.acct_dt  ,T3.seq_no,
                       case  when T5.doc_pf in
                       ('ISSUE', 'ISSUE-03', 'TV-ISSUE', 'RTV-CL', 'RTV-CL-03') then T3.trx_amt when T5.doc_pf in ('RTV', 'RTV-03', 'TV-ISSUE-CL','ISSUE-CL') then -T3.trx_amt
                       else case  when T3.add_ded='ADD' then T3.trx_amt when T3.add_ded='DED' then -T3.trx_amt else NULL end  end   trx_amt,
                        ( case  when T1.res_type is null or T1.res_type like '%MAT%' then 'MATERIAL'
                       when T1.res_type like 'LABOUR%' then 'LABOUR'
                       else 'OTHER' end ) as res_type
                      from   glvochmt T2,
                      pcesthmt T4,
                      pctrxhmt T5,
                     (pctrxdmt T3 LEFT OUTER JOIN pcrsgrpm T1 on T3.res_cd=T1.res_cd )
                      where  T3.doc_no=T5.doc_no and
                      T3.doc_pf=T5.doc_pf and
                      T3.doc_no=T2.doc_no and
                       T3.doc_pf=T2.doc_pf and
                       T4.project_no=T3.project_no and
                       T2.dept_id =  T4.dept_id and
                       T4.dept_id =  :costctr and
                        trim(T3.project_no)   = :projectno    and T3.trx_amt !=0
                         group by T4.project_no  ,
	                  T4.descr    ,
	                   T4.std_cost  ,
	                  T4.fund_source,
                       T4.estimate_no,T4.prj_ass_dt,T4.Status,t4.conf_dt,T2.Log_yr, T2.Log_mth, T2.doc_no, T2.doc_pf, T2.acct_dt , ( case  when T1.res_type is null or T1.res_type like '%MAT%' then 'MATERIAL'
                        when T1.res_type like 'LABOUR%' then 'LABOUR'
                        else 'OTHER' end )  ,T1.res_type,case  when T5.doc_pf in
                       ('ISSUE', 'ISSUE-03', 'TV-ISSUE', 'RTV-CL', 'RTV-CL-03') then T3.trx_amt when T5.doc_pf in ('RTV', 'RTV-03', 'TV-ISSUE-CL','ISSUE-CL') then -T3.trx_amt
                        else case  when T3.add_ded='ADD' then T3.trx_amt when T3.add_ded='DED' then -T3.trx_amt else NULL end  end,T3.seq_no
                      order by T2.Log_yr asc, T2.Log_mth asc, T2.doc_pf asc, T2.doc_no asc,  case  when T5.doc_pf in
                       ('ISSUE', 'ISSUE-03', 'TV-ISSUE', 'RTV-CL', 'RTV-CL-03') then T3.trx_amt when T5.doc_pf in ('RTV', 'RTV-03', 'TV-ISSUE-CL','ISSUE-CL') then -T3.trx_amt
                         else case  when T3.add_ded='ADD' then T3.trx_amt when T3.add_ded='DED' then -T3.trx_amt else NULL end  end asc";

                    using (var cmd = new OracleCommand(sql, conn))
                    {
                        cmd.BindByName = true;
                        cmd.Parameters.Add("costctr", costCenter);
                        cmd.Parameters.Add("projectno", projectNo);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                jobCardList.Add(new JobcardModel
                                {
                                    ProjectNo = reader["project_no"] != DBNull.Value ? reader["project_no"].ToString() : null,
                                    CommitedCost = reader["commited_cost"] != DBNull.Value ? Convert.ToDecimal(reader["commited_cost"]) : 0m,
                                    Description = reader["descr"] != DBNull.Value ? reader["descr"].ToString() : null,
                                    EstimatedCost = reader["std_cost"] != DBNull.Value ? Convert.ToDecimal(reader["std_cost"]) : 0m,
                                    FundSource = reader["fund_source"] != DBNull.Value ? reader["fund_source"].ToString() : null,
                                    EstimateNo = reader["estimate_no"] != DBNull.Value ? reader["estimate_no"].ToString() : null,
                                    ProjectAssignedDate = reader["prj_ass_dt"] != DBNull.Value ? Convert.ToDateTime(reader["prj_ass_dt"]) : (DateTime?)null,
                                    Status = reader["status"] != DBNull.Value ? reader["status"].ToString() : null,
                                    LogYear = reader["Log_yr"] != DBNull.Value ? Convert.ToInt32(reader["Log_yr"]) : 0,
                                    LogMonth = reader["Log_mth"] != DBNull.Value ? Convert.ToInt32(reader["Log_mth"]) : 0,
                                    DocumentProfile = reader["doc_pf"] != DBNull.Value ? reader["doc_pf"].ToString() : null,
                                    DocumentNo = reader["doc_no"] != DBNull.Value ? reader["doc_no"].ToString() : null,
                                    AccDate = reader["acct_dt"] != DBNull.Value ? Convert.ToDateTime(reader["acct_dt"]) : (DateTime?)null,
                                    SequenceNo = reader["seq_no"] != DBNull.Value ? Convert.ToInt32(reader["seq_no"]) : 0,
                                    TrxAmt = reader["trx_amt"] != DBNull.Value ? Convert.ToDecimal(reader["trx_amt"]) : 0m,
                                    ResType = reader["res_type"] != DBNull.Value ? reader["res_type"].ToString() : null
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetJobCard: {ex.Message}");
                throw;
            }

            return jobCardList;
        }
    }
}