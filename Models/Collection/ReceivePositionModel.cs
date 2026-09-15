using System.Collections.Generic;

namespace MISReports_Api.Models.Collection
{
    // ── Report type enum ───────────────────────────────────────────────────────
    public enum ReceivablePositionReportType
    {
        Province,
        Region,
        EntireCEB
    }

    // ── Main report model ──────────────────────────────────────────────────────
    public class ReceivablePositionModel
    {
        public string AreaCode { get; set; }
        public string AreaName { get; set; }

        // Raw decimal values
        public decimal RawOpeningBalance { get; set; }
        public decimal RawMonthlyCharge { get; set; }
        public decimal RawDebits { get; set; }
        public decimal RawCredits { get; set; }
        public decimal RawUnderCharge { get; set; }
        public decimal RawOverCharge { get; set; }
        public decimal RawPayments { get; set; }
        public decimal RawClosingBalance { get; set; }
        public decimal RawClosingBalanceWithoutFinAcc { get; set; }
        public decimal RawAverageCharge { get; set; }
        public decimal RawNoOfMonthsInArrears { get; set; }
        public decimal RawNoOfMonthsInArrearsWithoutFinAcc { get; set; }

        // Formatted display values
        public string OpeningBalance { get; set; }
        public string MonthlyCharge { get; set; }
        public string Debits { get; set; }
        public string Credits { get; set; }
        public string UnderCharge { get; set; }
        public string OverCharge { get; set; }
        public string Payments { get; set; }
        public string ClosingBalance { get; set; }
        public string ClosingBalanceWithoutFinAcc { get; set; }
        public string AverageCharge { get; set; }
        public string NoOfMonthsInArrears { get; set; }
        public string NoOfMonthsInArrearsWithoutFinAcc { get; set; }

        public string BillCycle { get; set; }
        public string BillType { get; set; }
        public string ErrorMessage { get; set; }
    }

    // ── Request model ──────────────────────────────────────────────────────────
    public class ReceivablePositionRequest
    {
        public string BillCycle { get; set; }
        public string BillType { get; set; }   // "O" or "B"
        public string AreaCode { get; set; }
        public string ProvinceCode { get; set; }
        public string RegionCode { get; set; }
        public ReceivablePositionReportType ReportType { get; set; }
    }

    // ── Bill type display model ────────────────────────────────────────────────
    public class ReceivablePositionBillTypeModel
    {
        public string BillType { get; set; }
        public string DisplayName { get; set; }
    }

    // ── Area model (kept for backward compatibility with old ReceivePositionModel.cs) ──
    public class ReceivablePositionAreaModel
    {
        public string AreaCode { get; set; }
        public string AreaName { get; set; }
        public string ProvinceCode { get; set; }
        public string ProvinceName { get; set; }
        public string RegionCode { get; set; }
    }
}