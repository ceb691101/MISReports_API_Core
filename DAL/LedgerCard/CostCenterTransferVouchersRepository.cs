using MISReports_Api.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;

namespace MISReports_Api.DAL
{
    public class CostCenterTransferVouchersRepository
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<CostCenterTransferVouchersModel> GetCostCenterTransferVouchersData(string costctr, int repyear, int startmonth, int endmonth, string subac, string docpf)
        {
            var result = new List<CostCenterTransferVouchersModel>();

            string sql = @"
select      T1.doc_pf AS DocPf,
            T2.Trf_type AS TrfType,
            T1.sub_ac AS SubAc,
            T2.remarks AS Remarks,
            T2.acct_dt AS AcctDt,
            T1.doc_no AS DocNo,
            T2.ref_1 AS Ref1,
            T2.chq_no AS ChqNo,
            T1.cr_amt AS CrAmt,
            T1.dr_amt AS DrAmt,
            T2.log_mth AS LogMth,
            t2.trf_dept as DesgDept,
            (select dept_nm from gldeptm where dept_id=:COSTCTR) as CctName
from        glvocdmt T1, glvochmt T2
where       T1.doc_no=T2.doc_no and
            T1.batch_id=T2.batch_id and
            T1.doc_pf=T2.doc_pf and
            T1.dept_id=T2.dept_id and
            T2.dept_id= :COSTCTR and
            substr(T1.gl_cd,8,5) = 'L9100' and
            T2.status = 6 and
            T2.log_yr = :REPYEAR and
            T2.log_mth >= :STARTMONTH and
            T2.log_mth <= :ENDMONTH and
            t2.trf_dept= :SUBAC";

            if (!string.IsNullOrEmpty(docpf))
            {
                sql += " and trim(T1.doc_pf) = trim(:DOCPF)";
            }

            sql += @"
GROUP BY    T1.doc_pf, T2.Trf_type, T1.sub_ac, substr(T1.gl_cd,8,5), T1.doc_no, T2.log_mth, T2.acct_dt, T2.chq_no, T2.ref_1, T1.cr_amt, T1.dr_amt, T2.remarks, t2.trf_dept
ORDER BY    T1.doc_pf, T2.Trf_type, T1.doc_no, substr(T1.gl_cd,8,5), T1.sub_ac, T2.log_mth, T2.acct_dt, T1.doc_pf, T1.doc_no, T2.chq_no, T2.ref_1, T1.cr_amt, T1.dr_amt";

            using (OracleConnection con = new OracleConnection(connectionString))
            {
                using (OracleCommand cmd = new OracleCommand(sql, con))
                {
                    cmd.BindByName = true;
                    cmd.Parameters.Add(new OracleParameter("COSTCTR", OracleDbType.Varchar2)).Value = costctr;
                    cmd.Parameters.Add(new OracleParameter("REPYEAR", OracleDbType.Int32)).Value = repyear;
                    cmd.Parameters.Add(new OracleParameter("STARTMONTH", OracleDbType.Int32)).Value = startmonth;
                    cmd.Parameters.Add(new OracleParameter("ENDMONTH", OracleDbType.Int32)).Value = endmonth;
                    cmd.Parameters.Add(new OracleParameter("SUBAC", OracleDbType.Varchar2)).Value = subac;
                    if (!string.IsNullOrEmpty(docpf))
                    {
                        cmd.Parameters.Add(new OracleParameter("DOCPF", OracleDbType.Varchar2)).Value = docpf;
                    }

                    con.Open();
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new CostCenterTransferVouchersModel
                            {
                                DocPf = reader["DocPf"] != DBNull.Value ? reader["DocPf"].ToString() : string.Empty,
                                TrfType = reader["TrfType"] != DBNull.Value ? reader["TrfType"].ToString() : string.Empty,
                                SubAc = reader["SubAc"] != DBNull.Value ? reader["SubAc"].ToString() : string.Empty,
                                Remarks = reader["Remarks"] != DBNull.Value ? reader["Remarks"].ToString() : string.Empty,
                                AcctDt = reader["AcctDt"] != DBNull.Value ? Convert.ToDateTime(reader["AcctDt"]) : (DateTime?)null,
                                DocNo = reader["DocNo"] != DBNull.Value ? reader["DocNo"].ToString() : string.Empty,
                                Ref1 = reader["Ref1"] != DBNull.Value ? reader["Ref1"].ToString() : string.Empty,
                                ChqNo = reader["ChqNo"] != DBNull.Value ? reader["ChqNo"].ToString() : string.Empty,
                                CrAmt = reader["CrAmt"] != DBNull.Value ? Convert.ToDecimal(reader["CrAmt"]) : (decimal?)null,
                                DrAmt = reader["DrAmt"] != DBNull.Value ? Convert.ToDecimal(reader["DrAmt"]) : (decimal?)null,
                                LogMth = reader["LogMth"] != DBNull.Value ? Convert.ToInt32(reader["LogMth"]) : (int?)null,
                                DesgDept = reader["DesgDept"] != DBNull.Value ? reader["DesgDept"].ToString() : string.Empty,
                                CctName = reader["CctName"] != DBNull.Value ? reader["CctName"].ToString() : string.Empty,
                            };
                            result.Add(item);
                        }
                    }
                }
            }

            return result;
        }

        public List<DocProfileModel> GetDocProfiles(string costctr)
        {
            var result = new List<DocProfileModel>();
            string sql = "select * from SADOCPHM where dept_id=:COSTCTR and appl_ID='GL'";

            using (OracleConnection con = new OracleConnection(connectionString))
            {
                using (OracleCommand cmd = new OracleCommand(sql, con))
                {
                    cmd.BindByName = true;
                    cmd.Parameters.Add(new OracleParameter("COSTCTR", OracleDbType.Varchar2)).Value = costctr;
                    con.Open();
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        var schemaTable = reader.GetSchemaTable();
                        bool hasDocPf = false;
                        bool hasDocDesc = false;

                        foreach (DataRow row in schemaTable.Rows)
                        {
                            string colName = row["ColumnName"].ToString().ToLower();
                            if (colName == "doc_pf") hasDocPf = true;
                            if (colName == "doc_desc") hasDocDesc = true;
                        }

                        while (reader.Read())
                        {
                            var item = new DocProfileModel
                            {
                                doc_pf = hasDocPf && reader["doc_pf"] != DBNull.Value ? reader["doc_pf"].ToString().Trim() : string.Empty,
                                doc_desc = hasDocDesc && reader["doc_desc"] != DBNull.Value ? reader["doc_desc"].ToString().Trim() : string.Empty
                            };
                            result.Add(item);
                        }
                    }
                }
            }
            return result;
        }
    }
}
