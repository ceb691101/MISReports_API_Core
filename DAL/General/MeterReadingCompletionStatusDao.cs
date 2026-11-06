using MISReports_Api.Compat;
using MISReports_Api.Models.General;
using NLog;
using System;
using System.Collections.Generic;
using System.Data.Common;

namespace MISReports_Api.DAL.General
{
    public class MeterReadingCompletionStatusDao
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private static readonly string[] Months = {
            "Jan", "Feb", "Mar", "Apr", "May", "Jun",
            "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"
        };

        public static string FormatBillCycle(int cycle)
        {
            if (cycle <= 0) return "";
            int baseYear = 1988;
            int baseMonth = 8;
            int totalMonths = baseMonth + (cycle - 1);
            int year = baseYear + (totalMonths / 12);
            int month = totalMonths % 12;
            string yy = (year % 100).ToString("D2");
            return $"{cycle} - {Months[month]} {yy}";
        }

        private DbConnection GetMrmsConnection()
        {
            var setting = ConfigurationManager.ConnectionStrings["InformixMrms"];
            string connStr = setting?.ConnectionString;
            if (string.IsNullOrWhiteSpace(connStr))
            {
                throw new ConfigurationErrorsException("Connection string \'InformixMrms\' is missing from connectionStrings.config.");
            }
            return Informix.CreateConnection(connStr);
        }

        private DbConnection GetOrdinaryConnection()
        {
            var setting = ConfigurationManager.ConnectionStrings["InformixConnection"];
            string connStr = setting?.ConnectionString;
            if (string.IsNullOrWhiteSpace(connStr))
            {
                throw new ConfigurationErrorsException("Connection string \'InformixConnection\' is missing from connectionStrings.config.");
            }
            return Informix.CreateConnection(connStr);
        }

        private class AreaInfo
        {
            public string AreaCode { get; set; }
            public string AreaName { get; set; }
            public string ProvName { get; set; }
        }

        private class PackInfo
        {
            public string AreaCode { get; set; }
            public int BillCycle { get; set; }
            public string DailyPack { get; set; }
            public DateTime? ConfDate { get; set; }
            public string ConfStatus { get; set; }
        }

        public MeterReadingCompletionStatusResponse GetCompletionStatus(int? targetCycle = null)
        {
            var response = new MeterReadingCompletionStatusResponse();

            using (var mrmsConn = GetMrmsConnection())
            {
                mrmsConn.Open();

                // 1. Get max bill cycle if not specified
                int cycle = targetCycle ?? 0;
                if (cycle <= 0)
                {
                    using (var cmd = Informix.CreateCommand("select max(bill_cycle) from mtr_pack_confirm", mrmsConn))
                    {
                        cmd.CommandTimeout = 120;
                        var result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value && int.TryParse(result.ToString(), out int parsed))
                        {
                            cycle = parsed;
                        }
                    }
                }

                if (cycle <= 0)
                {
                    throw new Exception("Unable to determine current bill cycle from mtr_pack_confirm.");
                }

                int prevCycle = cycle - 1;
                response.CurrentCycle = cycle;
                response.CurrentCycleLabel = FormatBillCycle(cycle);
                response.PreviousCycle = prevCycle;
                response.PreviousCycleLabel = FormatBillCycle(prevCycle);

                // 2. Fetch areas and provinces
                var areasList = new List<AreaInfo>();
                string areaSql = @"select a.area_code, a.area_name, p.prov_name 
                                   from areas a, prov_servers p 
                                   where a.prov_code = p.prov_code 
                                   order by p.prov_name, a.area_name";

                try
                {
                    using (var cmd = Informix.CreateCommand(areaSql, mrmsConn))
                    {
                        cmd.CommandTimeout = 120;
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                areasList.Add(new AreaInfo
                                {
                                    AreaCode = reader["area_code"]?.ToString()?.Trim(),
                                    AreaName = reader["area_name"]?.ToString()?.Trim(),
                                    ProvName = reader["prov_name"]?.ToString()?.Trim()
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.Warn(ex, "Failed to load areas from mrmsdb. Trying billsmry...");
                    // Try loading areas from billsmry
                    using (var ordConn = GetOrdinaryConnection())
                    {
                        ordConn.Open();
                        using (var cmd = Informix.CreateCommand(areaSql, ordConn))
                        {
                            cmd.CommandTimeout = 120;
                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    areasList.Add(new AreaInfo
                                    {
                                        AreaCode = reader["area_code"]?.ToString()?.Trim(),
                                        AreaName = reader["area_name"]?.ToString()?.Trim(),
                                        ProvName = reader["prov_name"]?.ToString()?.Trim()
                                    });
                                }
                            }
                        }
                    }
                }

                // 3. Fetch pack confirmation records for prevCycle and cycle
                var prevPacksByArea = new Dictionary<string, List<PackInfo>>(StringComparer.OrdinalIgnoreCase);
                var currPacksMap = new Dictionary<string, PackInfo>(StringComparer.OrdinalIgnoreCase);

                string packSql = @"select area_code, bill_cycle, daily_pack, r_conf_dt, r_conf_status 
                                   from mtr_pack_confirm 
                                   where bill_cycle in (?, ?) and r_conf_status in ('1', '2') 
                                   order by area_code, r_conf_dt, daily_pack";

                using (var cmd = Informix.CreateCommand(packSql, mrmsConn))
                {
                    cmd.CommandTimeout = 300;
                    cmd.Parameters.AddWithValue("?", prevCycle);
                    cmd.Parameters.AddWithValue("?", cycle);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string areaCode = reader["area_code"]?.ToString()?.Trim();
                            int bCycle = Convert.ToInt32(reader["bill_cycle"]);
                            string dlyPack = reader["daily_pack"]?.ToString()?.Trim();
                            DateTime? dt = reader["r_conf_dt"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["r_conf_dt"]);
                            string status = reader["r_conf_status"]?.ToString()?.Trim();

                            var pInfo = new PackInfo
                            {
                                AreaCode = areaCode,
                                BillCycle = bCycle,
                                DailyPack = dlyPack,
                                ConfDate = dt,
                                ConfStatus = status
                            };

                            if (bCycle == prevCycle)
                            {
                                if (!prevPacksByArea.TryGetValue(areaCode, out var list))
                                {
                                    list = new List<PackInfo>();
                                    prevPacksByArea[areaCode] = list;
                                }
                                list.Add(pInfo);
                            }
                            else if (bCycle == cycle)
                            {
                                string key = $"{areaCode}_{dlyPack}";
                                currPacksMap[key] = pInfo;
                            }
                        }
                    }
                }

                // 4. Generate report rows
                foreach (var area in areasList)
                {
                    if (prevPacksByArea.TryGetValue(area.AreaCode, out var prevList) && prevList.Count > 0)
                    {
                        foreach (var prev in prevList)
                        {
                            string key = $"{area.AreaCode}_{prev.DailyPack}";
                            currPacksMap.TryGetValue(key, out var curr);

                            string remarks = "";
                            if (curr != null && curr.ConfStatus == "2")
                            {
                                remarks = "Forcefully Comp.";
                            }

                            response.Rows.Add(new MeterReadingCompletionStatusItem
                            {
                                Province = area.ProvName,
                                AreaCode = area.AreaCode,
                                AreaName = area.AreaName,
                                LastCompletedCycle = response.PreviousCycleLabel,
                                LastReadingCompletedPack = prev.DailyPack,
                                LastPackCompletedDate = prev.ConfDate?.ToString("dd/MM/yyyy HH:mm:ss") ?? "",
                                CurrentCycle = response.CurrentCycleLabel,
                                CurrentReadingCompletedPack = curr != null ? curr.DailyPack : "",
                                CurrentPackCompletedDate = curr?.ConfDate?.ToString("dd/MM/yyyy HH:mm:ss") ?? "",
                                Remarks = remarks
                            });
                        }
                    }
                }
            }

            return response;
        }
    }
}

