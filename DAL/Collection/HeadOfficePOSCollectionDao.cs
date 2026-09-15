using MISReports_Api.Models.Collection;
using NLog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.OleDb;

namespace MISReports_Api.DAL.Collection
{
    public class HeadOfficePOSCollectionDao
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public List<HeadOfficePOSCollectionModel> GetReportData(HeadOfficePOSCollectionRequest request)
        {
            var results = new List<HeadOfficePOSCollectionModel>();

            try
            {
                logger.Info("=== START HeadOfficePOSCollection ===");
                
                string connString = ConfigurationManager.ConnectionStrings["InformixPosPayment"]?.ConnectionString;
                if (string.IsNullOrEmpty(connString))
                {
                    throw new Exception("InformixPosPayment connection string is missing from configuration.");
                }

                using (var conn = new OleDbConnection(connString))
                {
                    conn.Open();

                    string billTypeFilter = request.ReportType == "Bulk" ? "'B'" : "'O'";

                    string sql = $@"
                        SELECT a.AREA_NAME, COUNT(*) as Count, SUM(TRANS_AMT) as SumTransAmt, a.area_code 
                        FROM cus_tran t, areas a 
                        WHERE t.trans_date >= ? AND t.trans_date <= ? 
                        AND t.agent = 'CEBH' AND t.TRANS_TYPE = 0 
                        AND t.BILL_TYPE = {billTypeFilter} 
                        AND t.area_code = a.area_code 
                        GROUP BY 1,4 
                        ORDER BY 1";

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        DateTime fromDate = DateTime.Parse(request.FromDate);
                        DateTime toDate = DateTime.Parse(request.ToDate);

                        cmd.Parameters.AddWithValue("?", fromDate.ToString("dd-MM-yyyy"));
                        cmd.Parameters.AddWithValue("?", toDate.ToString("dd-MM-yyyy"));

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                results.Add(new HeadOfficePOSCollectionModel
                                {
                                    AreaName = reader["AREA_NAME"] != DBNull.Value ? reader["AREA_NAME"].ToString() : "",
                                    Count = reader["Count"] != DBNull.Value ? Convert.ToInt32(reader["Count"]) : 0,
                                    SumTransAmt = reader["SumTransAmt"] != DBNull.Value ? Convert.ToDecimal(reader["SumTransAmt"]) : 0,
                                    AreaCode = reader["area_code"] != DBNull.Value ? reader["area_code"].ToString() : ""
                                });
                            }
                        }
                    }
                }

                logger.Info($"=== END HeadOfficePOSCollection (Success) - {results.Count} records ===");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error occurred while fetching Head Office POS Collection report");
                throw;
            }

            return results;
        }
    }
}
