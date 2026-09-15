using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;
using MISReports_Api.DAL.AreaEngineerDashboard;
using MISReports_Api.Models.AreaEngineerDashboard;
using MISReports_Api.Models.DgmDashboard;
using MISReports_Api.Models.FinancialDashboard;
using MISReports_Api.Models.IntegratedDashboard;

namespace MISReports_Api.DAL.IntegratedDashboard
{
    public class IntegratedAreaEngineerDao
    {
        private static readonly string ConnectionString = System.Configuration.ConfigurationManager
            .ConnectionStrings["HQOracle"].ConnectionString;

        private static readonly AreaEngineerPivTotalDAL PivTotalDal = new AreaEngineerPivTotalDAL();
        private static readonly AreaEngineerPivPeriodSummaryDAL PivPeriodSummaryDal = new AreaEngineerPivPeriodSummaryDAL();
        private static readonly AreaEngineerStockValueDAL StockValueDal = new AreaEngineerStockValueDAL();
        private static readonly AreaEngineerAppCountDAL AppCountDal = new AreaEngineerAppCountDAL();
        private static readonly AreaEngineerConnectionGivenDAL ConnectionGivenDal = new AreaEngineerConnectionGivenDAL();
        private static readonly AreaEngineerPendingApplicationsDAL PendingApplicationsDal = new AreaEngineerPendingApplicationsDAL();
        private static readonly AreaEngineerMaterialMasterDAL MaterialMasterDal = new AreaEngineerMaterialMasterDAL();

        public static string ResolveCompanyId(string provinceCode)
        {
            if (string.IsNullOrWhiteSpace(provinceCode) || provinceCode.Equals("all", StringComparison.OrdinalIgnoreCase))
                return "WPN";

            var clean = provinceCode.Trim().ToUpper();

            switch (clean)
            {
                case "1":
                case "WPN":
                case "WESTERN PROVINCE NORTH":
                    return "WPN";
                case "2":
                case "WPS":
                case "WPS1":
                case "WPS 1":
                case "WESTERN PROVINCE SOUTH 1":
                case "WESTERN PROVINCE SOUTH I":
                    return "WPS1";
                case "C":
                case "WPS2":
                case "WPSII":
                case "WPS II":
                case "WESTERN PROVINCE SOUTH 2":
                case "WESTERN PROVINCE SOUTH II":
                    return "WPSII";
                case "3":
                case "CC":
                case "COL CITY":
                case "COLOMBO CITY":
                    return "CC";
                case "4":
                case "NP":
                case "NORTHERN":
                case "NORTHERN PROVINCE":
                    return "NP";
                case "5":
                case "CP":
                case "CENTRAL":
                case "CENTRAL PROVINCE":
                    return "CP";
                case "E":
                case "CP2":
                case "CENTRAL 2":
                case "CENTRAL II":
                    return "CP2";
                case "6":
                case "UVA":
                case "UVAP":
                case "UVA PROVINCE":
                    return "UVAP";
                case "7":
                case "EP":
                case "EASTERN":
                case "EASTERN PROVINCE":
                    return "EP";
                case "8":
                case "NWP":
                case "NORTH WESTERN":
                case "NORTH WESTERN PROVINCE":
                    return "NWP";
                case "D":
                case "NWP2":
                case "NWP 2":
                case "NORTH WESTERN 2":
                case "NORTH WESTERN PROVINCE II-EDL":
                    return "NWP 2";
                case "9":
                case "SAB":
                case "SABP":
                case "SABARAGAMUWA":
                case "SABARAGAMUWA PROVINCE":
                    return "SABP";
                case "A":
                case "NCP":
                case "NORTH CENTRAL":
                case "NORTH CENTRAL PROVINCE":
                    return "NCP";
                case "B":
                case "SP":
                case "SOUTHERN":
                case "SOUTHERN PROVINCE":
                    return "SP";
                case "F":
                case "SP2":
                case "SOUTHERN 2":
                case "SOUTHERN PROVINCE 2":
                    return "SP2";
                default:
                    return clean;
            }
        }

        public static string ResolveAreaCompanyId(string areaCode, string provinceCode = null)
        {
            if (string.IsNullOrWhiteSpace(areaCode) || areaCode.Equals("all", StringComparison.OrdinalIgnoreCase))
                return null;

            string clean = areaCode.Trim().ToUpper();

            switch (clean)
            {
                // Western Province North (WPN)
                case "49":
                case "GAMPAHA":
                case "GAMPAH":
                    return "GAMPAH";
                case "27":
                case "JA-ELA":
                case "JA ELA":
                case "JAELA":
                    return "JAELA";
                case "48":
                case "KELANIYA":
                case "KELANI":
                    return "KELANI";
                case "37":
                case "NEGOMBO":
                case "NEGOMB":
                    return "NEGOMB";
                case "66":
                case "DIVULAPITIYA":
                case "DIVULA":
                    return "DIVULA";
                case "53":
                case "VEYANGODA":
                case "VEYANG":
                    return "VEYANG";
                case "55":
                case "KIRINDIWELA":
                case "KIRIND":
                    return "KIRIND";

                // Western Province South 1 (WPS1)
                case "38":
                case "DEHIWALA":
                case "DEHIWA":
                    return "DEHIWA";
                case "44":
                case "KALUTARA":
                case "KALUTR":
                    return "KALUTR";
                case "87":
                case "MATHUGAMA":
                case "MATUGAMA":
                case "MTGMA":
                    return "MTGMA";
                case "21":
                case "RATMALANA":
                case "RLANA":
                case "RTMALN":
                    return "RLANA";

                // Western Province South 2 (WPSII)
                case "46":
                case "AVISSAWELLA":
                case "AVISSA":
                    return "AVISSA";
                case "65":
                case "BANDARAGAMA":
                case "BANDAR":
                case "BGAMA":
                    return "BGAMA";
                case "41":
                case "HOMAGAMA":
                case "HOMAGA":
                    return "HOMAGA";
                case "31":
                case "HORANA":
                case "HORNA":
                    return "HORNA";
                case "42":
                case "SRI JAYAWARDENE":
                case "SRI JAYAWARDENAPURA":
                case "JAPURA":
                    return "JAPURA";

                // Central Province (CP)
                case "77":
                case "KANDY CITY":
                case "KANDY":
                    return "KANDY";
                case "71":
                case "KATUGASTOTA":
                case "KATUGS":
                    return "KATUGS";
                case "40":
                case "KUNDASALE":
                case "KUNDSL":
                    return "KUNDSL";
                case "30":
                case "MATALE":
                    return "MATALE";
                case "80":
                case "GALAGEDARA":
                case "GALAGE":
                    return "GALAGE";
                case "85":
                case "DAMBULLA":
                case "DAMBUL":
                    return "DAMBUL";
                case "GINIG":
                case "GINIGA":
                case "GINIGATHHENA":
                    return "GINIG";
                case "NAWAL":
                case "NAWLPI":
                case "NAWALAPITIYA":
                    return "NAWAL";
                case "NELIYA":
                case "NUELIY":
                case "NUWARA-ELIYA":
                    return "NELIYA";
                case "PERA":
                case "PERADA":
                case "PERADENIYA":
                    return "PERA";

                // Southern Province (SP)
                case "45":
                case "GALLE":
                    return "GALLE";
                case "47":
                case "MATARA":
                case "MAT":
                    return "MAT";
                case "25":
                case "HAMBANTOTA":
                case "HAM":
                    return "HAM";
                case "62":
                case "TANGALLE":
                case "TAN":
                case "TANGAL":
                    return "TAN";
                case "54":
                case "AKURESSA":
                case "AKURES":
                    return "MAT";
                case "28":
                case "AMBALANGODA":
                case "AMBALA":
                case "AMBALN":
                    return "AMBALN";
                case "86":
                case "BADDEGAMA":
                case "BADDEG":
                    return "BADDEG";
                case "WELIGA":
                case "WELIGAMA":
                    return "WELIGA";
                case "KAM":
                case "KAMBURUPITIYA":
                    return "KAM";

                // North Western Province (NWP)
                case "19":
                case "CHILAW":
                    return "CHILAW";
                case "59":
                case "KULIYAPITIYA":
                case "KULIYA":
                    return "KULIYA";
                case "83":
                case "PUTTALAM":
                case "PUTTAL":
                    return "PUTTAL";
                case "50":
                case "WENNAPPUWA":
                case "WENNAP":
                    return "WENNAP";
                case "43":
                case "KURUNEGALA":
                case "KURU":
                case "KURUNE":
                    return "KURU";
                case "84":
                case "NARAMMALA":
                case "NARAM":
                case "NARAMM":
                    return "NARAM";
                case "76":
                case "WARIYAPOLA":
                case "WARI":
                case "WARIYA":
                    return "WARI";
                case "89":
                case "MAHAWA":
                case "MAHO":
                    return "MAHO";

                // Sabaragamuwa (SABP)
                case "26":
                case "RATNAPURA":
                case "RATNAP":
                    return "RATNAP";
                case "64":
                case "EBILIPITIYA":
                case "EMBILIPITIYA":
                case "EMBILI":
                    return "EMBILI";
                case "63":
                case "EHELIYAGODA":
                case "EHALI":
                    return "EHALI";
                case "52":
                case "KAHAWATTA":
                case "KAHAWA":
                    return "KAHAWA";
                case "79":
                case "RUWANWELLA":
                case "RUWANW":
                    return "RUWANW";
                case "BALANG":
                case "BALANGODA":
                    return "BALANG";
                case "KEGAL":
                case "KEGALL":
                case "KEGALLE":
                    return "KEGAL";
                case "MAWAN":
                case "MAWANE":
                case "MAWANELLA":
                    return "MAWAN";

                default:
                    return clean;
            }
        }

        public string DetermineTargetCompany(string provinceCode, string areaCode)
        {
            var resolvedArea = ResolveAreaCompanyId(areaCode, provinceCode);
            if (!string.IsNullOrWhiteSpace(resolvedArea))
                return resolvedArea;

            return ResolveCompanyId(provinceCode);
        }

        public double FetchStockValue(string provinceCode, string areaCode = null)
        {
            string targetCompany = DetermineTargetCompany(provinceCode, areaCode);
            if (string.IsNullOrWhiteSpace(targetCompany)) return 0;

            double val = StockValueDal.Fetch(targetCompany);
            if (val == 0)
            {
                // If direct area fetch returned 0, try with province-parent subquery
                using (var conn = new OracleConnection(ConnectionString))
                {
                    conn.Open();
                    string query = @"
                        select sum(c.qty_on_hand * c.unit_price) as Stock_value
                        from inwrhmtm c   
                        where c.status = 2 
                        and c.grade_cd = 'NEW'
                        and c.dept_id in (
                            select dept_id 
                            from gldeptm  
                            where status = 2 
                            and comp_id in (
                                select comp_id from glcompm
                                where status = 2 
                                and (TRIM(comp_id) = :c1 or TRIM(parent_id) = :c2)
                            )
                        )";

                    using (var cmd = new OracleCommand(query, conn))
                    {
                        cmd.BindByName = true;
                        cmd.Parameters.Add(new OracleParameter("c1", targetCompany));
                        cmd.Parameters.Add(new OracleParameter("c2", targetCompany));
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read() && !reader.IsDBNull(0))
                            {
                                val = Convert.ToDouble(reader.GetValue(0));
                            }
                        }
                    }
                }
            }
            return val;
        }

        public List<PivTotalModel> FetchPivTotal(string provinceCode, string areaCode = null)
        {
            string targetCompany = DetermineTargetCompany(provinceCode, areaCode);
            if (string.IsNullOrWhiteSpace(targetCompany)) return new List<PivTotalModel>();

            var raw = PivTotalDal.Fetch(targetCompany);
            if (raw == null || raw.Count == 0)
            {
                // Try with parent_id fallback query
                using (var conn = new OracleConnection(ConnectionString))
                {
                    conn.Open();
                    string sDateStr = DateTime.Today.AddDays(-30).ToString("yyyy-MM-dd");
                    string eDateStr = DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd");
                    string query = @"
                        select c.paid_date as PIV_Date, sum(c.grand_total) as PIV_collection
                        from piv_detail c 
                        where trim(c.status) in ('Q', 'P','F','FR','FA')
                        and c.paid_date >= TO_DATE(:startDate, 'YYYY-MM-DD')
                        and c.paid_date <= TO_DATE(:endDate, 'YYYY-MM-DD')
                        and c.dept_id in (
                            select dept_id from gldeptm 
                            where status = 2 
                            and comp_id in (
                                select comp_id from glcompm
                                where status = 2
                                and (TRIM(comp_id) = :c1 or TRIM(parent_id) = :c2)
                            )
                        )
                        group by c.paid_date 
                        order by c.paid_date desc";

                    using (var cmd = new OracleCommand(query, conn))
                    {
                        cmd.BindByName = true;
                        cmd.Parameters.Add(new OracleParameter("startDate", sDateStr));
                        cmd.Parameters.Add(new OracleParameter("endDate", eDateStr));
                        cmd.Parameters.Add(new OracleParameter("c1", targetCompany));
                        cmd.Parameters.Add(new OracleParameter("c2", targetCompany));

                        using (var reader = cmd.ExecuteReader())
                        {
                            var list = new List<DgmPivTotalModel>();
                            while (reader.Read())
                            {
                                list.Add(new DgmPivTotalModel
                                {
                                    date = reader.IsDBNull(0) ? string.Empty : reader.GetDateTime(0).ToString("yyyy-MM-dd"),
                                    amount = reader.IsDBNull(1) ? 0 : Convert.ToDouble(reader.GetValue(1))
                                });
                            }
                            raw = list;
                        }
                    }
                }
            }

            return (raw ?? new List<DgmPivTotalModel>()).Select(r => new PivTotalModel
            {
                date = r.date,
                amount = r.amount
            }).OrderBy(x => x.date).ToList();
        }

        public double FetchPivPeriodSummary(string provinceCode, string startDate = null, string endDate = null, string areaCode = null)
        {
            string targetCompany = DetermineTargetCompany(provinceCode, areaCode);
            if (string.IsNullOrWhiteSpace(targetCompany)) return 0;

            double val = PivPeriodSummaryDal.Fetch(targetCompany, startDate, endDate);
            if (val == 0)
            {
                using (var conn = new OracleConnection(ConnectionString))
                {
                    conn.Open();
                    string sDateStr = string.IsNullOrWhiteSpace(startDate)
                        ? DateTime.Today.AddDays(-30).ToString("yyyy-MM-dd")
                        : startDate;
                    string eDateStr = string.IsNullOrWhiteSpace(endDate)
                        ? DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd")
                        : endDate;

                    string query = @"
                        select sum(c.grand_total) as PIV_collection
                        from piv_detail c 
                        where trim(c.status) in ('Q', 'P','F','FR','FA')
                        and c.paid_date >= TO_DATE(:startDate, 'YYYY-MM-DD')
                        and c.paid_date <= TO_DATE(:endDate, 'YYYY-MM-DD')
                        and c.dept_id in (
                            select dept_id from gldeptm 
                            where status = 2 
                            and comp_id in (
                                select comp_id from glcompm
                                where status = 2
                                and (TRIM(comp_id) = :c1 or TRIM(parent_id) = :c2)
                            )
                        )";

                    using (var cmd = new OracleCommand(query, conn))
                    {
                        cmd.BindByName = true;
                        cmd.Parameters.Add(new OracleParameter("startDate", sDateStr));
                        cmd.Parameters.Add(new OracleParameter("endDate", eDateStr));
                        cmd.Parameters.Add(new OracleParameter("c1", targetCompany));
                        cmd.Parameters.Add(new OracleParameter("c2", targetCompany));
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read() && !reader.IsDBNull(0))
                            {
                                val = Convert.ToDouble(reader.GetValue(0));
                            }
                        }
                    }
                }
            }
            return val;
        }

        public AreaEngineerMaterialMasterSummaryModel FetchMaterialMaster(string provinceCode, string areaCode = null)
        {
            string targetCompany = DetermineTargetCompany(provinceCode, areaCode);
            if (string.IsNullOrWhiteSpace(targetCompany)) return new AreaEngineerMaterialMasterSummaryModel();

            var result = MaterialMasterDal.Fetch(targetCompany);
            if (result == null || result.materials == null || result.materials.Count == 0)
            {
                // Try matching via parent_id or comp_id in glcompm
                var model = new AreaEngineerMaterialMasterSummaryModel
                {
                    provinceId = targetCompany,
                    provinceName = targetCompany
                };
                var matMap = new Dictionary<string, AreaEngineerMaterialMasterItem>(StringComparer.OrdinalIgnoreCase);
                var areaTotalMap = new Dictionary<string, AreaQtyItem>(StringComparer.OrdinalIgnoreCase);

                using (var conn = new OracleConnection(ConnectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT 
                            i.mat_cd,
                            m.mat_nm,
                            m.maj_uom,
                            m.unit_price,
                            c.comp_id AS area_id,
                            c.comp_nm AS area_name,
                            SUM(i.qty_on_hand) AS area_qty,
                            SUM(i.qty_on_hand * NVL(m.unit_price, 0)) AS area_val
                        FROM inwrhmtm i
                        JOIN inmatm m ON i.mat_cd = m.mat_cd
                        JOIN gldeptm d ON i.dept_id = d.dept_id
                        JOIN glcompm c ON d.comp_id = c.comp_id
                        WHERE i.status = 2
                          AND i.grade_cd = 'NEW'
                          AND i.qty_on_hand > 0
                          AND d.status = 2
                          AND c.status = 2
                          AND (TRIM(c.comp_id) = :c1 OR TRIM(c.parent_id) = :c2)
                        GROUP BY i.mat_cd, m.mat_nm, m.maj_uom, m.unit_price, c.comp_id, c.comp_nm
                        ORDER BY area_val DESC, i.mat_cd ASC";

                    using (var cmd = new OracleCommand(query, conn))
                    {
                        cmd.BindByName = true;
                        cmd.Parameters.Add(new OracleParameter("c1", targetCompany));
                        cmd.Parameters.Add(new OracleParameter("c2", targetCompany));
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string matCd = reader.IsDBNull(0) ? string.Empty : reader.GetString(0).Trim();
                                string matNm = reader.IsDBNull(1) ? string.Empty : reader.GetString(1).Trim();
                                string uomCd = reader.IsDBNull(2) ? string.Empty : reader.GetString(2).Trim();
                                double unitPrice = reader.IsDBNull(3) ? 0 : Convert.ToDouble(reader.GetValue(3));
                                string areaId = reader.IsDBNull(4) ? string.Empty : reader.GetString(4).Trim();
                                string areaNm = reader.IsDBNull(5) ? string.Empty : reader.GetString(5).Trim();
                                double areaQty = reader.IsDBNull(6) ? 0 : Convert.ToDouble(reader.GetValue(6));
                                double areaVal = reader.IsDBNull(7) ? 0 : Convert.ToDouble(reader.GetValue(7));

                                if (!matMap.TryGetValue(matCd, out var item))
                                {
                                    item = new AreaEngineerMaterialMasterItem
                                    {
                                        matCd = matCd,
                                        matNm = string.IsNullOrWhiteSpace(matNm) ? matCd : matNm,
                                        uomCd = uomCd,
                                        unitPrice = unitPrice,
                                        provinceQtyOnHand = 0,
                                        provinceStockValue = 0,
                                        areaBreakdown = new List<AreaQtyItem>()
                                    };
                                    matMap[matCd] = item;
                                }

                                item.provinceQtyOnHand += areaQty;
                                item.provinceStockValue += areaVal;
                                item.areaBreakdown.Add(new AreaQtyItem
                                {
                                    areaId = areaId,
                                    areaName = string.IsNullOrWhiteSpace(areaNm) ? areaId : areaNm,
                                    qtyOnHand = areaQty,
                                    stockValue = areaVal
                                });

                                if (!areaTotalMap.TryGetValue(areaId, out var areaTot))
                                {
                                    areaTot = new AreaQtyItem
                                    {
                                        areaId = areaId,
                                        areaName = string.IsNullOrWhiteSpace(areaNm) ? areaId : areaNm,
                                        qtyOnHand = 0,
                                        stockValue = 0
                                    };
                                    areaTotalMap[areaId] = areaTot;
                                }
                                areaTot.qtyOnHand += areaQty;
                                areaTot.stockValue += areaVal;
                            }
                        }
                    }
                }

                model.materials = matMap.Values.OrderByDescending(x => x.provinceStockValue).ToList();
                model.areaTotals = areaTotalMap.Values.OrderByDescending(x => x.stockValue).ToList();
                model.totalProvinceQtyOnHand = model.materials.Sum(x => x.provinceQtyOnHand);
                model.totalProvinceStockValue = model.materials.Sum(x => x.provinceStockValue);
                result = model;
            }

            return result ?? new AreaEngineerMaterialMasterSummaryModel();
        }

        public List<DgmAppCountModel> FetchApplicationCounts(int year, string provinceCode, string areaCode = null)
        {
            string targetCompany = DetermineTargetCompany(provinceCode, areaCode);
            if (string.IsNullOrWhiteSpace(targetCompany)) return new List<DgmAppCountModel>();

            var raw = AppCountDal.Fetch(year, targetCompany);
            if (raw == null || raw.Count == 0)
            {
                // Query with parent_id fallback
                using (var conn = new OracleConnection(ConnectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT 
                            app.dept_id,
                            appty.description,
                            app.application_type,
                            COUNT(*) AS no_of_applications
                        FROM applications app
                        INNER JOIN applicationtypes appty ON app.application_type = appty.apptype
                        WHERE app.status NOT IN ('D')
                        AND TO_CHAR(app.submit_date, 'YYYY') = :year
                        AND app.dept_id IN (
                            SELECT dept_id 
                            FROM gldeptm 
                            WHERE comp_id IN (
                                SELECT comp_id 
                                FROM glcompm 
                                WHERE TRIM(comp_id) = :c1 OR TRIM(parent_id) = :c2
                            )
                        )
                        GROUP BY app.dept_id, appty.description, app.application_type
                        ORDER BY app.dept_id";

                    using (var cmd = new OracleCommand(query, conn))
                    {
                        cmd.BindByName = true;
                        cmd.Parameters.Add(new OracleParameter("year", year.ToString()));
                        cmd.Parameters.Add(new OracleParameter("c1", targetCompany));
                        cmd.Parameters.Add(new OracleParameter("c2", targetCompany));
                        using (var reader = cmd.ExecuteReader())
                        {
                            var list = new List<DgmAppCountModel>();
                            while (reader.Read())
                            {
                                list.Add(new DgmAppCountModel
                                {
                                    deptId = reader.IsDBNull(0) ? string.Empty : reader.GetString(0).Trim(),
                                    description = reader.IsDBNull(1) ? string.Empty : reader.GetString(1).Trim(),
                                    appType = reader.IsDBNull(2) ? string.Empty : reader.GetString(2).Trim(),
                                    noOfApplications = reader.IsDBNull(3) ? 0 : Convert.ToInt32(reader.GetValue(3))
                                });
                            }
                            raw = list;
                        }
                    }
                }
            }

            return raw ?? new List<DgmAppCountModel>();
        }

        public List<DgmConnectionGivenModel> FetchConnectionsGiven(int year, string provinceCode, string areaCode = null)
        {
            string targetCompany = DetermineTargetCompany(provinceCode, areaCode);
            if (string.IsNullOrWhiteSpace(targetCompany)) return new List<DgmConnectionGivenModel>();

            var raw = ConnectionGivenDal.Fetch(year, targetCompany);
            if (raw == null || raw.Count == 0)
            {
                using (var conn = new OracleConnection(ConnectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT 
                            app.dept_id,
                            appty.description,
                            app.application_type,
                            COUNT(*) AS no_of_connections
                        FROM applications app
                        INNER JOIN applicationtypes appty ON app.application_type = appty.apptype
                        INNER JOIN pcesthmt T1 ON TRIM(T1.estimate_no) = TRIM(app.application_no)
                        INNER JOIN spodrcrd L ON TRIM(T1.project_no) = TRIM(L.project_no)
                        WHERE app.status NOT IN ('D') 
                        AND TO_CHAR(app.submit_date, 'YYYY') = :year
                        AND app.dept_id IN (
                            SELECT dept_id 
                            FROM gldeptm 
                            WHERE comp_id IN (
                                SELECT comp_id FROM glcompm 
                                WHERE TRIM(comp_id) = :c1 OR TRIM(parent_id) = :c2
                            )
                        )
                        GROUP BY app.dept_id, appty.description, app.application_type
                        UNION ALL
                        SELECT 
                            app.dept_id,
                            appty.description,
                            app.application_type,
                            COUNT(*) AS no_of_connections
                        FROM applications app
                        INNER JOIN applicationtypes appty ON app.application_type = appty.apptype
                        INNER JOIN pcesthmt T1 ON TRIM(T1.estimate_no) = TRIM(app.application_no)
                        WHERE app.status NOT IN ('D') 
                        AND TO_CHAR(app.submit_date, 'YYYY') = :year and T1.Status in (3,4)
                        and TRIM(T1.project_no) not in (select TRIM(L.project_no) from spodrcrd L)
                        AND app.dept_id IN (
                            SELECT dept_id 
                            FROM gldeptm 
                            WHERE comp_id IN (
                                SELECT comp_id FROM glcompm 
                                WHERE TRIM(comp_id) = :c3 OR TRIM(parent_id) = :c4
                            )
                        )
                        GROUP BY app.dept_id, appty.description, app.application_type
                        ORDER BY 1";

                    using (var cmd = new OracleCommand(query, conn))
                    {
                        cmd.BindByName = true;
                        cmd.Parameters.Add(new OracleParameter("year", year.ToString()));
                        cmd.Parameters.Add(new OracleParameter("c1", targetCompany));
                        cmd.Parameters.Add(new OracleParameter("c2", targetCompany));
                        cmd.Parameters.Add(new OracleParameter("c3", targetCompany));
                        cmd.Parameters.Add(new OracleParameter("c4", targetCompany));
                        using (var reader = cmd.ExecuteReader())
                        {
                            var list = new List<DgmConnectionGivenModel>();
                            while (reader.Read())
                            {
                                list.Add(new DgmConnectionGivenModel
                                {
                                    deptId = reader.IsDBNull(0) ? string.Empty : reader.GetString(0).Trim(),
                                    description = reader.IsDBNull(1) ? string.Empty : reader.GetString(1).Trim(),
                                    appType = reader.IsDBNull(2) ? string.Empty : reader.GetString(2).Trim(),
                                    noOfConnections = reader.IsDBNull(3) ? 0 : Convert.ToInt32(reader.GetValue(3))
                                });
                            }
                            raw = list;
                        }
                    }
                }
            }

            return raw ?? new List<DgmConnectionGivenModel>();
        }

        public List<DgmPendingApplicationModel> FetchPendingApplications(int year, string provinceCode, string areaCode = null)
        {
            string targetCompany = DetermineTargetCompany(provinceCode, areaCode);
            if (string.IsNullOrWhiteSpace(targetCompany)) return new List<DgmPendingApplicationModel>();

            var raw = PendingApplicationsDal.Fetch(year, targetCompany);
            if (raw == null || raw.Count == 0)
            {
                using (var conn = new OracleConnection(ConnectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT 
                            app.dept_id,
                            appty.description,
                            app.application_type,
                            app.application_no
                        FROM applications app
                        INNER JOIN applicationtypes appty ON app.application_type = appty.apptype
                        WHERE app.status NOT IN ('D')
                        AND TO_CHAR(app.submit_date, 'YYYY') = :year
                        AND app.dept_id IN (
                            SELECT dept_id 
                            FROM gldeptm 
                            WHERE comp_id IN (
                                SELECT comp_id FROM glcompm 
                                WHERE TRIM(comp_id) = :c1 OR TRIM(parent_id) = :c2
                            )
                        )
                        AND app.application_no NOT IN (
                            SELECT app.application_no
                            FROM applications app
                            INNER JOIN applicationtypes appty ON app.application_type = appty.apptype
                            INNER JOIN pcesthmt T1 ON TRIM(T1.estimate_no) = TRIM(app.application_no)
                            INNER JOIN spodrcrd L ON TRIM(T1.project_no) = TRIM(L.project_no)
                            WHERE app.status NOT IN ('D')
                            AND T1.status = 1
                            AND TO_CHAR(app.submit_date, 'YYYY') = :year
                            AND app.dept_id IN (
                                SELECT dept_id 
                                FROM gldeptm 
                                WHERE comp_id IN (
                                    SELECT comp_id FROM glcompm 
                                    WHERE TRIM(comp_id) = :c3 OR TRIM(parent_id) = :c4
                                )
                            )
                        )
                        ORDER BY app.dept_id, appty.description, app.application_type, app.application_no";

                    using (var cmd = new OracleCommand(query, conn))
                    {
                        cmd.BindByName = true;
                        cmd.Parameters.Add(new OracleParameter("year", year.ToString()));
                        cmd.Parameters.Add(new OracleParameter("c1", targetCompany));
                        cmd.Parameters.Add(new OracleParameter("c2", targetCompany));
                        cmd.Parameters.Add(new OracleParameter("c3", targetCompany));
                        cmd.Parameters.Add(new OracleParameter("c4", targetCompany));
                        using (var reader = cmd.ExecuteReader())
                        {
                            var list = new List<DgmPendingApplicationModel>();
                            while (reader.Read())
                            {
                                list.Add(new DgmPendingApplicationModel
                                {
                                    deptId = reader.IsDBNull(0) ? string.Empty : reader.GetString(0).Trim(),
                                    description = reader.IsDBNull(1) ? string.Empty : reader.GetString(1).Trim(),
                                    appType = reader.IsDBNull(2) ? string.Empty : reader.GetString(2).Trim(),
                                    applicationNo = reader.IsDBNull(3) ? string.Empty : reader.GetString(3).Trim()
                                });
                            }
                            raw = list;
                        }
                    }
                }
            }

            return raw ?? new List<DgmPendingApplicationModel>();
        }

        public IntegratedAreaEngineerSummaryModel FetchSummary(string provinceCode, int year, string startDate = null, string endDate = null, string areaCode = null)
        {
            string targetCompany = DetermineTargetCompany(provinceCode, areaCode);
            var targetYear = year <= 0 ? DateTime.Today.Year : year;

            // Run all 7 DB queries in parallel for maximum throughput
            double stock = 0;
            List<PivTotalModel> pivList = null;
            double pivPeriod = 0;
            AreaEngineerMaterialMasterSummaryModel materials = null;
            List<DgmAppCountModel> apps = null;
            List<DgmConnectionGivenModel> conns = null;
            List<DgmPendingApplicationModel> pendings = null;

            var tasks = new[]
            {
                Task.Run(() => { stock = FetchStockValue(provinceCode, areaCode); }),
                Task.Run(() => { pivList = FetchPivTotal(provinceCode, areaCode); }),
                Task.Run(() => { pivPeriod = FetchPivPeriodSummary(provinceCode, startDate, endDate, areaCode); }),
                Task.Run(() => { materials = FetchMaterialMaster(provinceCode, areaCode); }),
                Task.Run(() => { apps = FetchApplicationCounts(targetYear, provinceCode, areaCode); }),
                Task.Run(() => { conns = FetchConnectionsGiven(targetYear, provinceCode, areaCode); }),
                Task.Run(() => { pendings = FetchPendingApplications(targetYear, provinceCode, areaCode); }),
            };

            Task.WaitAll(tasks);

            return new IntegratedAreaEngineerSummaryModel
            {
                ProvinceCode = targetCompany,
                ProvinceName = materials?.provinceName ?? targetCompany,
                Year = targetYear,
                StockValue = stock,
                PivTotal = pivList ?? new List<PivTotalModel>(),
                PivPeriodSummary = pivPeriod,
                MaterialMaster = materials,
                ApplicationCounts = apps ?? new List<DgmAppCountModel>(),
                ConnectionsGiven = conns ?? new List<DgmConnectionGivenModel>(),
                PendingApplications = pendings ?? new List<DgmPendingApplicationModel>(),
                FetchedAt = DateTimeOffset.UtcNow
            };
        }
    }
}
