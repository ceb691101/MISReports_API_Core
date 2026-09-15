using System;
using System.Collections.Generic;
using System.Linq;

namespace MISReports_Api.Models.General
{
    /// <summary>
    /// Tariff period information
    /// </summary>
    public class TariffPeriod
    {
        public int Category { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime EffectiveTo { get; set; }
    }

    /// <summary>
    /// Tariff information from tariff_table
    /// </summary>
    public class TariffInfo
    {
        public int Category { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime EffectiveTo { get; set; }
        public int FromUnits { get; set; }
        public int ToUnits { get; set; }
        public int BlockLimit { get; set; }
        public decimal Rate { get; set; }

        // Fixed charge fields
        public string TypeFixed { get; set; }      // "S" or "V"
        public string BasicBlock { get; set; }     // "BA" or "BL"
        public string MinCharge { get; set; }      // "Y" or "N"
        public decimal FixCharge { get; set; }     // Fixed charge amount
    }

    /// <summary>
    /// Request model for bill calculation
    /// </summary>
    public class BillCalculationRequest
    {
        public int Category { get; set; } = 11;
        public decimal FullUnits { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
    }

    /// <summary>
    /// Calculation for a single tariff period
    /// </summary>
    public class PeriodCalculation
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int NumberOfDays { get; set; }
        public decimal NumberOfUnits { get; set; }
        public decimal KWHCharge { get; set; }
        public decimal FixedCharge { get; set; }
        public decimal FacCharge { get; set; }
        public decimal TotalCharge => KWHCharge + FixedCharge + FacCharge;

        public List<BlockCharge> BlockCharges { get; set; } = new List<BlockCharge>();

        // Formatted date strings for display
        public string FromDateDisplay => FromDate.ToString("dd-MM-yy");
        public string ToDateDisplay => ToDate.ToString("dd-MM-yy");
        public string PeriodDisplay => $"From {FromDateDisplay} To {ToDateDisplay}";
    }

    /// <summary>
    /// Detailed bill calculation with multiple period support
    /// </summary>
    public class DetailedBillCalculation
    {
        public int Category { get; set; }
        public decimal FullUnits { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        // Calculated fields
        public int NumberOfDays { get; set; }

        // Period-wise calculations
        public List<PeriodCalculation> PeriodCalculations { get; set; } = new List<PeriodCalculation>();

        // Grand totals
        public decimal KWHCharge { get; set; }
        public decimal FixedCharge { get; set; }
        public decimal FacCharge { get; set; }
        public decimal TotalCharge { get; set; }

        // Summary properties
        public int TotalPeriods => PeriodCalculations.Count;
        public decimal TotalUnitsProcessed => PeriodCalculations.Sum(p => p.NumberOfUnits);
    }

    /// <summary>
    /// Individual block charge details matching the report format
    /// </summary>
    public class BlockCharge
    {
        // Original block limits from tariff table
        public int BlockLimit { get; set; }  // e.g., 30, 60, 90, etc.
        public int FromUnits { get; set; }    // e.g., 1, 31, 61, etc.
        public int ToUnits { get; set; }      // e.g., 30, 60, 90, etc. (0 for last block)

        // Prorated blocks based on number of days in the period
        public int ProratedFrom { get; set; }  // e.g., 1, 128, 255, etc.
        public int ProratedTo { get; set; }    // e.g., 127, 254, 381, etc. (0 for last block)

        // Rate and charge
        public decimal Rate { get; set; }
        public decimal OriginalRate { get; set; }

        public decimal UnitsInBlock { get; set; }
        public decimal Charge { get; set; }

        // Calculated charge display (e.g., "127 * 4.00 = 508.00")
        public string ChargeCalculation
        {
            get
            {
                if (UnitsInBlock > 0 && Charge > 0)
                    return $"{UnitsInBlock} * {Rate:F2} = {Charge:F2}";
                return string.Empty;
            }
        }

        // Block limit display (e.g., "1 - 30", "31 - 60", "181 - 0")
        public string BlockLimitDisplay => $"{FromUnits} - {ToUnits}";

        // Prorated blocks display (e.g., "1 - 127", "128 - 254", "763 - 0")
        public string ProratedBlocksDisplay => $"{ProratedFrom} - {ProratedTo}";
    }

    /// <summary>
    /// Bill summary matching the top section of the report
    /// </summary>
    public class BillSummary
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int NumberOfDays { get; set; }
        public decimal NumberOfUnits { get; set; }
        public decimal KWHCharge { get; set; }
        public decimal FixedCharge { get; set; }
        public decimal FacCharge { get; set; }
        public decimal TotalCharge { get; set; }

        // Period-wise breakdown
        public List<PeriodCalculation> PeriodCalculations { get; set; } = new List<PeriodCalculation>();

        // Summary display properties
        public string FromDateDisplay => FromDate.ToString("dd-MM-yy");
        public string ToDateDisplay => ToDate.ToString("dd-MM-yy");
    }

    /// <summary>
    /// Main model for bill calculation details (legacy support)
    /// </summary>
    public class BillCalculationDetails
    {
        public int Category { get; set; }
        public decimal FullUnits { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        // Calculated fields
        public decimal UnitsInBlock { get; set; }
        public int BalanceDays { get; set; }
        public decimal BalanceUnits { get; set; }
        public int NumberOfDays { get; set; }
    }
}