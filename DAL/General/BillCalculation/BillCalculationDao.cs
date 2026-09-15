using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using MISReports_Api.DBAccess;
using MISReports_Api.Models.General;

namespace MISReports_Api.DAL.General.BillCalculation
{
    public class BillCalculationDao
    {
        private readonly DBConnection _dbConnection;

        public BillCalculationDao()
        {
            _dbConnection = new DBConnection();
        }

        /// <summary>
        /// Get all tariff periods that overlap with the given date range
        /// </summary>
        private List<TariffPeriod> GetTariffPeriods(int category, DateTime fromDate, DateTime toDate)
        {
            string sql = @"SELECT DISTINCT category, effective_from, effective_to 
                          FROM tariff_table 
                          WHERE category = ? 
                          AND effective_from <= ? 
                          AND effective_to >= ?
                          ORDER BY effective_from";

            var periods = new List<TariffPeriod>();

            try
            {
                using (var conn = _dbConnection.GetConnection(false))
                {
                    conn.Open();

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@category", category);
                        //Local Machine
                        //cmd.Parameters.AddWithValue("@toDate", toDate.ToString("yyyy-MM-dd"));
                        //cmd.Parameters.AddWithValue("@fromDate", fromDate.ToString("yyyy-MM-dd"));
                        //Server
                        cmd.Parameters.AddWithValue("@toDate", toDate.ToString("dd-MM-yyyy"));
                        cmd.Parameters.AddWithValue("@fromDate", fromDate.ToString("dd-MM-yyyy"));

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                periods.Add(new TariffPeriod
                                {
                                    Category = Convert.ToInt32(reader["category"]),
                                    EffectiveFrom = Convert.ToDateTime(reader["effective_from"]),
                                    EffectiveTo = Convert.ToDateTime(reader["effective_to"])
                                });
                            }
                        }
                    }
                }

                return periods;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"Error in GetTariffPeriods: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Get all tariff blocks for a specific category and period
        /// </summary>
        public List<TariffInfo> GetTariffBlocks(int category, DateTime effectiveDate)
        {
            string sql = @"SELECT * FROM tariff_table 
                          WHERE category = ? 
                          AND effective_from <= ? 
                          AND effective_to >= ?
                          ORDER BY from_units";

            var tariffs = new List<TariffInfo>();

            try
            {
                using (var conn = _dbConnection.GetConnection(false))
                {
                    conn.Open();

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@category", category);
                        //Local Machine
                        //cmd.Parameters.AddWithValue("@effectiveFrom", effectiveDate.ToString("yyyy-MM-dd"));
                        //cmd.Parameters.AddWithValue("@effectiveTo", effectiveDate.ToString("yyyy-MM-dd"));
                        //Server
                        cmd.Parameters.AddWithValue("@effectiveFrom", effectiveDate.ToString("dd-MM-yyyy"));
                        cmd.Parameters.AddWithValue("@effectiveTo", effectiveDate.ToString("dd-MM-yyyy"));

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int fromUnits = reader["from_units"] != DBNull.Value ? Convert.ToInt32(reader["from_units"]) : 0;
                                int toUnits = reader["to_units"] != DBNull.Value ? Convert.ToInt32(reader["to_units"]) : 0;

                                // Calculate block limit (to_units - from_units + 1)
                                // For last block where to_units = 0 or very large number (like 10000000), treat as unlimited
                                int blockLimit;
                                if (toUnits == 0 || toUnits >= 10000000)
                                {
                                    blockLimit = 0; // Unlimited block
                                    toUnits = 0; // Normalize to 0 for display
                                }
                                else
                                {
                                    blockLimit = toUnits - fromUnits + 1;
                                }

                                tariffs.Add(new TariffInfo
                                {
                                    Category = Convert.ToInt32(reader["category"]),
                                    EffectiveFrom = Convert.ToDateTime(reader["effective_from"]),
                                    EffectiveTo = Convert.ToDateTime(reader["effective_to"]),
                                    FromUnits = fromUnits,
                                    ToUnits = toUnits,
                                    BlockLimit = blockLimit,
                                    Rate = reader["rate"] != DBNull.Value ? Convert.ToDecimal(reader["rate"]) : 0,

                                    // Fixed charge fields
                                    TypeFixed = reader["type_fixed"] != DBNull.Value ? reader["type_fixed"].ToString() : "",
                                    BasicBlock = reader["block_basic"] != DBNull.Value ? reader["block_basic"].ToString() : "",
                                    MinCharge = reader["min_chrge"] != DBNull.Value ? reader["min_chrge"].ToString() : "",
                                    FixCharge = reader["fixed_charge"] != DBNull.Value ? Convert.ToDecimal(reader["fixed_charge"]) : 0
                                });
                            }
                        }
                    }
                }

                return tariffs;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"Error in GetTariffBlocks: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Calculate detailed bill handling multiple tariff periods
        /// </summary>
        public DetailedBillCalculation CalculateDetailedBill(BillCalculationRequest request)
        {
            var result = new DetailedBillCalculation
            {
                Category = request.Category,
                FullUnits = request.FullUnits,
                FromDate = request.FromDate,
                ToDate = request.ToDate
            };

            try
            {
                // Calculate total number of days
                int totalDays = (request.ToDate - request.FromDate).Days;
                result.NumberOfDays = totalDays;

                // Get all tariff periods that overlap with the date range
                var tariffPeriods = GetTariffPeriods(request.Category, request.FromDate, request.ToDate);

                if (tariffPeriods.Count == 0)
                {
                    throw new Exception($"No tariff periods found for category {request.Category}");
                }

                bool skipProRataFixed = tariffPeriods.Count == 1 && totalDays < 54;

                decimal balanceUnits = request.FullUnits;
                int balanceDays = totalDays;

                // Process each tariff period
                foreach (var period in tariffPeriods)
                {
                    // Determine the actual date range for this period
                    DateTime periodStart = period.EffectiveFrom > request.FromDate ? period.EffectiveFrom : request.FromDate;
                    DateTime periodEnd = period.EffectiveTo < request.ToDate ? period.EffectiveTo : request.ToDate;

                    // Calculate days in this period
                    int daysInPeriod = (periodEnd - periodStart).Days;

                    // BUGFIX: Skip periods with 0 or negative days (no overlap)
                    // This prevents divide-by-zero errors when fromDate falls on the last day of a tariff period
                    if (daysInPeriod <= 0)
                    {
                        System.Diagnostics.Trace.WriteLine($"Skipping period with {daysInPeriod} days: {periodStart:yyyy-MM-dd} to {periodEnd:yyyy-MM-dd}");
                        continue;
                    }

                    // Add 1 to the first day calculation ONLY if:
                    // 1. This is the start of the tariff period (periodStart == period.EffectiveFrom)
                    // 2. AND the user's fromDate is DIFFERENT from the tariff period start
                    // 3. AND there are balance days
                    if (periodStart == period.EffectiveFrom &&
                        request.FromDate != period.EffectiveFrom &&
                        balanceDays > 0)
                    {
                        daysInPeriod = daysInPeriod + 1;
                    }

                    // Calculate units for this period using the formula from the docx
                    // units1 = ((bal_units * No_days1 / bal_no_days))
                    //decimal unitsInPeriod = (balanceUnits * daysInPeriod) / (decimal)balanceDays;

                    // Calculate units for this period using the formula from the docx
                    // units1 = ((bal_units * No_days1 / bal_no_days))
                    decimal unitsInPeriod;

                    if (balanceDays <= 0)
                    {
                        // Safety check: if no balance days remain, assign all remaining units
                        unitsInPeriod = balanceUnits;
                        //System.Diagnostics.Trace.WriteLine($"Warning: balanceDays is {balanceDays} for category {request.Category}, assigning remaining {balanceUnits} units to period {periodStart:yyyy-MM-dd} to {periodEnd:yyyy-MM-dd}");
                        System.Diagnostics.Trace.WriteLine($"Warning: balanceDays is {balanceDays} for category {request.Category}, assigning remaining {balanceUnits} units to period {periodStart:dd-MM-yyyy} to {periodEnd:dd-MM-yyyy}");
                    }
                    else
                    {
                        unitsInPeriod = (balanceUnits * daysInPeriod) / (decimal)balanceDays;
                    }

                    // Create period calculation
                    var periodCalc = new PeriodCalculation
                    {
                        FromDate = periodStart,
                        ToDate = periodEnd,
                        NumberOfDays = daysInPeriod,
                        NumberOfUnits = Math.Ceiling(unitsInPeriod) // Always round UP
                    };

                    // Get tariff blocks for this period
                    var tariffBlocks = GetTariffBlocks(request.Category, periodStart);

                    // Calculate block charges for this period
                    decimal periodTotalCharge = 0;

                    // Calculate prorated limit for second block (31-60) to determine special rate
                    // Special rate applies only if consumption exceeds the PRORATED second block
                    int secondBlockProratedLimit = (int)Math.Ceiling((60 * daysInPeriod / 30.0));
                    bool applySpecialRate = periodCalc.NumberOfUnits > secondBlockProratedLimit;

                    // Determine which special rate to apply based on the period date
                    // Period past to 2024-07-15 → Use 25.00
                    // Period 2024-07-16 to 2025-01-17 → Use 15.00
                    // Period 2025-01-18 to 2025-06-11 → Use 11.00
                    // Period 2025-06-12 to 2026-03-31 → Use 12.75
                    // Period 2026-04-01 onwards → Use 14.00
                    decimal specialRateForPeriod = 0;
                    if (request.Category == 11)
                    {
                        DateTime specialRate25EndDate = new DateTime(2024, 7, 15);
                        DateTime specialRate15StartDate = new DateTime(2024, 7, 16);
                        DateTime specialRate15EndDate = new DateTime(2025, 1, 17);
                        DateTime specialRate11EndDate = new DateTime(2025, 6, 11);
                        DateTime specialRate1275EndDate = new DateTime(2026, 3, 31);

                        if (periodStart <= specialRate25EndDate)
                        {
                            specialRateForPeriod = 25.00m;
                        }
                        else if (periodStart >= specialRate15StartDate && periodStart <= specialRate15EndDate)
                        {
                            specialRateForPeriod = 15.00m;
                        }
                        else if (periodStart <= specialRate11EndDate)
                        {
                            specialRateForPeriod = 11.00m;
                        }
                        else if (periodStart <= specialRate1275EndDate)
                        {
                            specialRateForPeriod = 12.75m;
                        }
                        else
                        {
                            specialRateForPeriod = 14.00m;
                        }
                    }
                    // Category 21 - Check if consumption exceeds 300 kWh/month
                    // Calculate monthly equivalent consumption
                    decimal monthlyEquivalentConsumption = (periodCalc.NumberOfUnits * 30) / daysInPeriod;
                    bool isCategory21Or41HighConsumption = (request.Category == 21 || request.Category == 41) && monthlyEquivalentConsumption > 300;
                    bool isCategory31Or33HighConsumption = (request.Category == 31 || request.Category == 33) && monthlyEquivalentConsumption > 180;

                    foreach (var tariff in tariffBlocks)
                    {
                        // Calculate prorated block range based on days in THIS period
                        int proratedFrom = tariff.FromUnits == 1 ? 1 :
                            (int)Math.Ceiling(((tariff.FromUnits - 1) * daysInPeriod / 30.0)) + 1;

                        int proratedTo = tariff.ToUnits == 0 ? 0 :
                            (int)Math.Ceiling((tariff.ToUnits * daysInPeriod / 30.0));

                        decimal blockCharge = 0;
                        decimal unitsInBlock = 0;
                        decimal effectiveRate = tariff.Rate;

                        // From 2026-05-11: new boundary condition for category 11 and 51
                        // If units exceed prorated 180 kWh threshold:
                        //   - All blocks within 1-180 kWh -> hardcoded rate (32.50 for cat 11, 11.80 for cat 51)
                        //   - Blocks above 180 kWh -> DB rate
                        //   - Special rates do NOT apply
                        // If units are within 1-180 kWh -> DB rate as-is, no special rate, no hardcoded rate
                        DateTime cat11And51HardcodedRateStartDate = new DateTime(2026, 5, 11);
                        int proratedLimit180 = (int)Math.Ceiling((180 * daysInPeriod / 30.0));
                        bool applyHardcodedRate = (request.Category == 11 || request.Category == 51)
                            && periodStart >= cat11And51HardcodedRateStartDate
                            && periodCalc.NumberOfUnits > proratedLimit180;

                        if (applyHardcodedRate)
                        {
                            // Hardcoded rate for 1-180 blocks; DB rate for above-180 blocks
                            if (tariff.FromUnits <= 180 && (tariff.ToUnits == 0 || tariff.ToUnits <= 180))
                            {
                                effectiveRate = request.Category == 11 ? 32.50m : 11.80m;
                            }
                            // else: tariff.FromUnits > 180 -> effectiveRate stays as tariff.Rate (DB rate)
                        }
                        else
                        {
                            // Apply special rate logic based on period date and consumption
                            // Only apply to first two blocks (1-30 and 31-60) when consumption exceeds prorated second block
                            if (request.Category == 11 && applySpecialRate && tariff.ToUnits <= 60 && tariff.ToUnits > 0)
                            {
                                effectiveRate = specialRateForPeriod;
                            }
                        }

                        // Category 21 - Always flat rate (no blocks)
                        // Use different flat rate based on consumption:
                        // >300 kWh/month: Use high consumption rate (typically from block 31-60 or higher)
                        // ≤300 kWh/month: Use low consumption rate (typically from block 1-30)
                        if (request.Category == 21 || request.Category == 41 || request.Category == 31 || request.Category == 33)
                        {
                            bool isHighConsumption = isCategory21Or41HighConsumption || isCategory31Or33HighConsumption;

                            // For Category 21, only use the appropriate single rate
                            if (isHighConsumption && tariff.ToUnits == 0)
                            {
                                // High consumption (>300 kWh/month) - use higher rate
                                // Typically from block 31-60 or the second tariff entry
                                unitsInBlock = periodCalc.NumberOfUnits;
                                blockCharge = unitsInBlock * effectiveRate;
                            }
                            else if (!isHighConsumption && tariff.ToUnits > 0)
                            {
                                // Low consumption (≤300 kWh/month) - use lower rate
                                // Typically from block 1-30 or the first tariff entry
                                unitsInBlock = periodCalc.NumberOfUnits;
                                blockCharge = unitsInBlock * effectiveRate;
                            }
                            // Skip all other blocks for Category 21

                        }
                        else
                        {
                            // Normal block-based calculation for Categories 11, 51, and others

                            // Determine if units fall into this block
                            if (tariff.ToUnits == 0) // Last block (unlimited)
                            {
                                // Check if we have units that exceed all previous blocks
                                if (periodCalc.NumberOfUnits >= proratedFrom)
                                {
                                    unitsInBlock = periodCalc.NumberOfUnits - proratedFrom + 1;
                                    blockCharge = unitsInBlock * effectiveRate;
                                }
                            }
                            else if (periodCalc.NumberOfUnits >= proratedFrom && periodCalc.NumberOfUnits <= proratedTo)
                            {
                                // Units fall within this block's prorated range
                                unitsInBlock = periodCalc.NumberOfUnits - proratedFrom + 1;
                                blockCharge = unitsInBlock * effectiveRate;
                            }
                            else if (periodCalc.NumberOfUnits > proratedTo)
                            {
                                // Units exceed this block, use full block
                                unitsInBlock = proratedTo - proratedFrom + 1;
                                blockCharge = unitsInBlock * effectiveRate;
                            }
                        }



                        periodCalc.BlockCharges.Add(new BlockCharge
                        {
                            BlockLimit = tariff.BlockLimit,
                            FromUnits = tariff.FromUnits,
                            ToUnits = tariff.ToUnits,
                            ProratedFrom = proratedFrom,
                            ProratedTo = proratedTo,
                            Rate = effectiveRate, // Use effective rate (may be special rate)
                            OriginalRate = tariff.Rate, // Keep original rate for reference
                            UnitsInBlock = Math.Round(unitsInBlock, 0),
                            Charge = Math.Round(blockCharge, 2)
                        });

                        periodTotalCharge += blockCharge;

                    }

                    periodCalc.KWHCharge = Math.Round(periodTotalCharge, 2);

                    // Calculate fixed charge for this period 
                    periodCalc.FixedCharge = CalculateFixedCharge(tariffBlocks, daysInPeriod, periodCalc.NumberOfUnits, request.Category, skipProRataFixed);

                    result.PeriodCalculations.Add(periodCalc);

                    // Update balance for next period
                    // bal_no_days = bal_no_days - No_days1
                    balanceDays = balanceDays - daysInPeriod;
                    // bal_units = bal_units - units1
                    balanceUnits = balanceUnits - periodCalc.NumberOfUnits;
                }

                // Calculate grand totals
                result.KWHCharge = Math.Round(result.PeriodCalculations.Sum(p => p.KWHCharge), 2);
                result.FixedCharge = Math.Round(result.PeriodCalculations.Sum(p => p.FixedCharge), 2);
                result.TotalCharge = result.KWHCharge + result.FixedCharge; // Add FAC charge later if needed

                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"Error in CalculateDetailedBill: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Get bill summary for a period
        /// </summary>
        public BillSummary GetBillSummary(int category, decimal units, DateTime fromDate, DateTime toDate)
        {
            var summary = new BillSummary
            {
                FromDate = fromDate,
                ToDate = toDate,
                NumberOfDays = (toDate - fromDate).Days,
                NumberOfUnits = units
            };

            try
            {
                var calculation = CalculateDetailedBill(new BillCalculationRequest
                {
                    Category = category,
                    FullUnits = units,
                    FromDate = fromDate,
                    ToDate = toDate
                });

                summary.KWHCharge = calculation.KWHCharge;
                summary.TotalCharge = calculation.TotalCharge;
                summary.PeriodCalculations = calculation.PeriodCalculations;

                return summary;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"Error in GetBillSummary: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Calculate fixed charge for a period based on tariff type
        /// </summary>
        private decimal CalculateFixedCharge(List<TariffInfo> tariffBlocks, int daysInPeriod, decimal unitsInPeriod, int category, bool skipProRataFixed = false)
        {
            if (tariffBlocks == null || tariffBlocks.Count == 0)
                return 0;

            // BUGFIX: Safety check to prevent division by zero
            if (daysInPeriod <= 0)
            {
                System.Diagnostics.Trace.WriteLine($"Warning: daysInPeriod is {daysInPeriod}, returning 0 for fixed charge");
                return 0;
            }

            // Calculate number of months
            decimal noMonths = daysInPeriod / 30.0m;
            bool useFullMonthCharge = skipProRataFixed; // driven by caller, not by daysInPeriod alone

            var firstBlock = tariffBlocks[0];

            // Calculate monthly equivalent consumption for threshold check
            decimal monthlyEquivalentConsumption = (unitsInPeriod * 30) / daysInPeriod;

            // Determine if this is high consumption for flat-rate categories
            bool isCategory21Or41HighConsumption = (category == 21 || category == 41) && monthlyEquivalentConsumption > 300;
            bool isCategory31Or33HighConsumption = (category == 31 || category == 33) && monthlyEquivalentConsumption > 180;
            bool isHighConsumption = isCategory21Or41HighConsumption || isCategory31Or33HighConsumption;

            // For flat-rate categories (21, 31, 33, 41), select the appropriate fixed charge based on consumption
            if (category == 21 || category == 31 || category == 33 || category == 41)
            {
                foreach (var tariff in tariffBlocks)
                {
                    if (isHighConsumption && tariff.ToUnits == 0)
                    {
                        // High consumption - use fixed charge from the unlimited block (typically higher rate)
                        return useFullMonthCharge ? tariff.FixCharge : noMonths * tariff.FixCharge;
                    }
                    else if (!isHighConsumption && tariff.ToUnits > 0)
                    {
                        // Low consumption - use fixed charge from the first block (typically lower rate)
                        return useFullMonthCharge ? tariff.FixCharge : noMonths * tariff.FixCharge;
                    }
                }

                // Fallback to first block if no match found
                return useFullMonthCharge ? firstBlock.FixCharge : noMonths * firstBlock.FixCharge;
            }

            // Type S (Single) - Simple fixed charge (for categories 11, 51, etc.)
            if (firstBlock.TypeFixed == "S" && firstBlock.BasicBlock == "BL" && firstBlock.MinCharge == "N")
            {
                return useFullMonthCharge ? firstBlock.FixCharge : noMonths * firstBlock.FixCharge;
            }

            // Type BA (Basic All) - Single rate for all units
            if (firstBlock.BasicBlock == "BA" && firstBlock.MinCharge == "N")
            {
                return useFullMonthCharge ? firstBlock.FixCharge : noMonths * firstBlock.FixCharge;
            }

            // Type V (Variable) - Block-based fixed charge (for categories 11, 51, etc.)
            if (firstBlock.TypeFixed == "V" && firstBlock.BasicBlock == "BL" && firstBlock.MinCharge == "N")
            {
                decimal totBlockUnits = 0;
                decimal fixedCharge = 0;

                foreach (var tariff in tariffBlocks)
                {
                    // Calculate prorated block units
                    int proratedBlockLimit = tariff.BlockLimit == 0 ? 0 :
                        (int)Math.Ceiling((tariff.BlockLimit * daysInPeriod / 30.0));

                    totBlockUnits += proratedBlockLimit;

                    // If accumulated block units cover the consumption
                    if (totBlockUnits >= unitsInPeriod || tariff.ToUnits == 0)
                    {
                        fixedCharge = useFullMonthCharge ? tariff.FixCharge : noMonths * tariff.FixCharge;
                        break;
                    }
                }

                return fixedCharge;
            }

            // Default case
            return useFullMonthCharge ? firstBlock.FixCharge : noMonths * firstBlock.FixCharge;
        }
    }
}