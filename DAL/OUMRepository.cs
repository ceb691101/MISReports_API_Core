using MISReports_Api.Compat;
using MISReports_Api.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace MISReports_Api.DAL
{
    public class OUMRepository
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        //private readonly string connectionString = ConfigurationManager.ConnectionStrings["InformixCreditCard"].ConnectionString;
        string connectionstring = ConfigurationManager.ConnectionStrings["InformixCreditCardOleDb"]?.ConnectionString
            ?? throw new ConfigurationErrorsException("Connection string 'InformixCreditCardOleDb' is missing from connectionStrings.config.");
        public int InsertIntoAmex2(List<OUMEmployeeModel> data)
        {


            int count = 0;
            using (DbConnection conn = Informix.CreateConnection(connectionstring))
            {
                try
                {
                    conn.Open();
                    // Clear existing data
                    using (var cmd = Informix.CreateCommand("DELETE FROM test_amex2", conn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // Insert new data
                    foreach (var item in data)
                    {
                        using (var cmd = Informix.CreateCommand())
                        {
                            cmd.Connection = conn;
                            cmd.CommandText = @"INSERT INTO test_amex2 (pdate, o_id, acct_no, cname, bill_amt, tax, tot_amt, authcode, cno) 
                                              VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)";

                            cmd.Parameters.AddWithValue("pdate", item.AuthDate);
                            cmd.Parameters.AddWithValue("o_id", item.OrderId);
                            cmd.Parameters.AddWithValue("acct_no", item.AcctNumber ?? "");
                            cmd.Parameters.AddWithValue("cname", item.BankCode ?? "");
                            cmd.Parameters.AddWithValue("bill_amt", item.BillAmt);
                            cmd.Parameters.AddWithValue("tax", item.TaxAmt);
                            cmd.Parameters.AddWithValue("tot_amt", item.TotAmt);
                            cmd.Parameters.AddWithValue("authcode", item.AuthCode ?? "");
                            cmd.Parameters.AddWithValue("cno", item.CardNo ?? "");

                            count += cmd.ExecuteNonQuery();
                        }
                    }
                }
                catch (DbException ex)
                {
                    logger.Error(ex, "InsertIntoAmex2 failed (exception rethrown)");
                    Console.WriteLine($"Error inserting into Amex2: {ex.Message}");
                    throw;
                }
            }
            return count;
        }

        public void RefreshCrdTemp()
        {
            using (var conn = Informix.CreateConnection(connectionstring))
            {
                try
                {
                    conn.Open();

                    // Delete existing records from test_crdt_tmp
                    using (var cmd = Informix.CreateCommand("DELETE FROM test_crdt_tmp", conn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // Insert from amex2 to crdt_tmp
                    string insertSql = @"INSERT INTO test_crdt_tmp 
                                       SELECT o_id, acct_no, '-', '-', bill_amt, tax, tot_amt, 'S',
                                              authcode, pdate, pdate, 'S', 0, cname, 'CRC', '', '', '', '', '', '',
                                              cno, 'Bil', acct_no, 'RSK', ''
                                       FROM test_amex2";

                    using (var cmd = Informix.CreateCommand(insertSql, conn))
                    {
                        cmd.ExecuteNonQuery();

                    }

                    // Update null values for specific fields
                    using (var cmd = Informix.CreateCommand(@"UPDATE test_crdt_tmp 
                                                     SET updt_flag = NULL, post_flag = NULL, err_flag = NULL, sms_st = NULL", conn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // Update payment_type where ref_number is longer than 10 characters  
                    using (var cmd = Informix.CreateCommand(@"UPDATE test_crdt_tmp SET payment_type = 'PIV' WHERE LENGTH(ref_number) > 10", conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (DbException ex)
                {
                    logger.Error(ex, "RefreshCrdTemp failed (exception rethrown)");
                    Console.WriteLine($"Error refreshing CrdTemp: {ex.Message}");
                    throw;
                }
            }
        }

        public List<OUMCrdTempModel> GetCrdTempRecords()
        {
            var records = new List<OUMCrdTempModel>();
            using (var conn = Informix.CreateConnection(connectionstring))
            {
                try
                {
                    conn.Open();
                    string sql = "SELECT * FROM test_crdt_tmp ORDER BY auth_date";

                    using (var cmd = Informix.CreateCommand(sql, conn))
                    using (DbDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var record = new OUMCrdTempModel
                            {


                                OrderId = reader.GetInt32(0),
                                AcctNumber = reader.GetString(1),
                                CustName = reader.GetString(2),
                                UserName = reader.GetString(3),
                                BillAmt = reader.GetDecimal(4),
                                TaxAmt = reader.GetDecimal(5),
                                TotAmt = reader.GetDecimal(6),
                                TrStatus = reader.GetString(7),
                                AuthCode = reader.GetString(8),
                                PmntDate = reader.GetDateTime(9),
                                AuthDate = reader.GetDateTime(10),
                                CebRes = reader.GetString(11),
                                SerlNo = reader.GetInt32(12),
                                BankCode = reader.GetString(13),
                                BranCode = reader.GetString(14),
                                //inst_status = reader.GetString(15),
                                // updt_status = reader.GetString(16),
                                //updt_flag = reader.IsDBNull(17) ? null : reader.GetString(17),
                                // post_flag = reader.IsDBNull(18) ? null : reader.GetString(18),
                                // err_flag = reader.IsDBNull(19) ? null : reader.GetString(19),
                                //post_date = reader.GetDateTime(20),
                                CardNo = reader.GetString(21),
                                PaymentType = reader.GetString(22),
                                RefNumber = reader.GetString(23),
                                ReferenceType = reader.GetString(24)
                                // sms_st = reader.IsDBNull(25) ? null : reader.GetString(25)
                            };
                            records.Add(record);
                        }
                    }
                }
                catch (DbException ex)
                {
                    logger.Error(ex, "GetCrdTempRecords failed (exception rethrown)");
                    Console.WriteLine($"Error retrieving CrdTemp records: {ex.Message}");
                    throw;
                }
            }
            return records;
        }

        public bool ApproveRecords()
        {

            bool success = false;
            using (var conn = Informix.CreateConnection(connectionstring))
            {
                DbTransaction transaction = null;
                try
                {
                    conn.Open();
                    transaction = conn.BeginTransaction();

                    // Delete crdt_tmp records (this is for temporary)




                    // Insert into test_crdtcdslt
                    using (var cmd = Informix.CreateCommand("INSERT INTO test_crdtcdslt SELECT * FROM test_crdt_tmp", conn, transaction))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // Insert into backup table
                    using (var cmd = Informix.CreateCommand("INSERT INTO crdt_tmp_backup SELECT * FROM test_crdt_tmp", conn, transaction))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // Delete from temp table
                    using (var cmd = Informix.CreateCommand("DELETE FROM test_crdt_tmp", conn, transaction))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    success = true;
                    return success;
                }
                catch (DbException ex)
                {
                    logger.Error(ex, "ApproveRecords failed");
                    transaction?.Rollback();
                    return success;

                }
            }
        }



    }
}