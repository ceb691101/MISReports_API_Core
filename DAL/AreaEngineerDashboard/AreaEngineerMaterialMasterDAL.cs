using System;
using System.Collections.Generic;
using System.Linq;
using Oracle.ManagedDataAccess.Client;
using MISReports_Api.Models.AreaEngineerDashboard;

namespace MISReports_Api.DAL.AreaEngineerDashboard
{
    public class AreaEngineerMaterialMasterDAL
    {
        private static readonly string ConnectionString = System.Configuration.ConfigurationManager
            .ConnectionStrings["HQOracle"].ConnectionString;

        public AreaEngineerMaterialMasterSummaryModel Fetch(string companyId)
        {
            var model = new AreaEngineerMaterialMasterSummaryModel
            {
                provinceId = companyId,
                provinceName = companyId
            };

            var matMap = new Dictionary<string, AreaEngineerMaterialMasterItem>(StringComparer.OrdinalIgnoreCase);
            var areaTotalMap = new Dictionary<string, AreaQtyItem>(StringComparer.OrdinalIgnoreCase);

            using (OracleConnection conn = new OracleConnection(ConnectionString))
            {
                conn.Open();

                // Fetch Province Name if available
                string provNmSql = "SELECT comp_nm FROM glcompm WHERE TRIM(comp_id) = :companyId";
                using (OracleCommand provCmd = new OracleCommand(provNmSql, conn))
                {
                    provCmd.Parameters.Add(new OracleParameter("companyId", companyId));
                    using (OracleDataReader r = provCmd.ExecuteReader())
                    {
                        if (r.Read() && !r.IsDBNull(0))
                        {
                            model.provinceName = r.GetString(0).Trim();
                        }
                    }
                }

                // Query Material stock balances grouped by material and Area (glcompm)
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
                      AND TRIM(c.comp_id) = :companyId
                    GROUP BY i.mat_cd, m.mat_nm, m.maj_uom, m.unit_price, c.comp_id, c.comp_nm
                    ORDER BY area_val DESC, i.mat_cd ASC";

                using (OracleCommand cmd = new OracleCommand(query, conn))
                {
                    cmd.BindByName = true;
                    cmd.Parameters.Add(new OracleParameter("companyId", companyId));
                    using (OracleDataReader reader = cmd.ExecuteReader())
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

                            // Track totals by area
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

            return model;
        }
    }
}