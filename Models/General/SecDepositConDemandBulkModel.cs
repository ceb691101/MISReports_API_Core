using System;
using System.Collections.Generic;
using System.Linq;
using MISReports_Api.Models.SolarInformation;

namespace MISReports_Api.Models.General
{
    // ── Main report model ──────────────────────────────────────────────────────
    public class SecDepositConDemandBulkModel
    {
        public string AccountNumber { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string Tariff { get; set; }

        public string ContractDemand { get; set; }
        public string SecurityDeposit { get; set; }
        public string TotalKWOUnits { get; set; }
        public string TotalKWDUnits { get; set; }
        public string TotalKWPUnits { get; set; }
        public string KVA { get; set; }
        public string MonthlyCharge { get; set; }

        public string ProvinceCode { get; set; }
        public string AreaCode { get; set; }
        public string Province { get; set; }
        public string Area { get; set; }

        public string BillCycle { get; set; }
        public string ErrorMessage { get; set; }

        public decimal RawContractDemand { get; set; }
        public decimal RawSecurityDeposit { get; set; }
        public decimal RawTotalKWOUnits { get; set; }
        public decimal RawTotalKWDUnits { get; set; }
        public decimal RawTotalKWPUnits { get; set; }
        public decimal RawKVA { get; set; }
        public decimal RawMonthlyCharge { get; set; }

        // Additional properties for display formatting
        public string FormattedContractDemand => string.IsNullOrEmpty(ContractDemand) ? "0.00" : ContractDemand;
        public string FormattedSecurityDeposit => string.IsNullOrEmpty(SecurityDeposit) ? "0.00" : SecurityDeposit;
        public string FormattedTotalKWOUnits => string.IsNullOrEmpty(TotalKWOUnits) ? "0" : TotalKWOUnits;
        public string FormattedTotalKWDUnits => string.IsNullOrEmpty(TotalKWDUnits) ? "0" : TotalKWDUnits;
        public string FormattedTotalKWPUnits => string.IsNullOrEmpty(TotalKWPUnits) ? "0" : TotalKWPUnits;
        public string FormattedKVA => string.IsNullOrEmpty(KVA) ? "0.00" : KVA;
        public string FormattedMonthlyCharge => string.IsNullOrEmpty(MonthlyCharge) ? "0.00" : MonthlyCharge;
    }

    // ── Request model ──────────────────────────────────────────────────────────
    public class SecDepositConDemandRequest
    {
        public string BillCycle { get; set; }
        public SolarReportType ReportType { get; set; }
        public string AreaCode { get; set; }
        public string ProvCode { get; set; }
        public string Region { get; set; }
    }

    // ── Area model (returned by GET api/contract-demand/areas) ───────────────
    public class AreaModel
    {
        public string AreaCode { get; set; }
        public string AreaName { get; set; }
    }

    // ── Province model (returned by GET api/contract-demand/provinces) ───────
    public class ProvinceModel
    {
        public string ProvinceCode { get; set; }
        public string ProvinceName { get; set; }
    }

    // ── Helper (used internally by province queries) ───────────────────────────
    public class ProvinceAreaInfo
    {
        public string AccountNumber { get; set; }
        public string AreaCode { get; set; }
        public string ProvinceCode { get; set; }
        public string AreaName { get; set; }
        public string ProvinceName { get; set; }
    }

    // ── Response model for report data with metadata ───────────────────────────
    public class SecDepositConDemandResponse
    {
        public string Title { get; set; }
        public string Area { get; set; }
        public string Province { get; set; }
        public string BillMonth { get; set; }
        public List<SecDepositConDemandBulkModel> Data { get; set; }
        public ReportSummary Summary { get; set; }
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
    }

    // ── Summary model for report totals ─────────────────────────────────────────
    public class ReportSummary
    {
        public int TotalRecords { get; set; }
        public decimal TotalContractDemand { get; set; }
        public decimal TotalSecurityDeposit { get; set; }
        public decimal TotalKWOUnits { get; set; }
        public decimal TotalKWDUnits { get; set; }
        public decimal TotalKWPUnits { get; set; }
        public decimal TotalKVA { get; set; }
        public decimal TotalMonthlyCharge { get; set; }
    }

    // ── Filter options model for dropdowns ──────────────────────────────────────
    public class FilterOptions
    {
        public List<ProvinceModel> Provinces { get; set; }
        public List<AreaModel> Areas { get; set; }
        public List<string> BillCycles { get; set; }
    }

    // ── Export model for Excel/PDF export ───────────────────────────────────────
    public class ExportModel
    {
        public string Format { get; set; } // "Excel", "PDF", "CSV"
        public SecDepositConDemandRequest Filters { get; set; }
    }
}