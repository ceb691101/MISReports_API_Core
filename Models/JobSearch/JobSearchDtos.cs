using System;

namespace MISReports_Api.Models
{
    // 02. Application Status Details
    public class ApplicationStatusDto
    {
        public string ApplicationNo { get; set; }       // ap.application_id as application_no
        public string FullName { get; set; }            // a.first_name || ' ' || a.last_name
        public string IdNo { get; set; }
        public string Address { get; set; }             // street + suburb + city
        public string ServiceAddress { get; set; }      // w.service_street_address + service_suburb + service_city
        public string TelephoneNo { get; set; }
        public string MobileNo { get; set; }
        public string Email { get; set; }
    }

    // 03. Application Information (PIV + wiring details)
    public class ApplicationInfoDto
    {
        public string PivNo { get; set; }
        public string TitleCd { get; set; }
        public decimal? PivAmount { get; set; }
        public DateTime? PivDate { get; set; }
        public DateTime? PaidDate { get; set; }
        public string ApplicationType { get; set; }         // t.description
        public string Phase { get; set; }
        public string ConnectionType { get; set; }
        public string TariffCatCode { get; set; }
        public string TariffCode { get; set; }
        public DateTime? ApplicationDate { get; set; }      // ap.submit_date
        public string NeighborsAccNo { get; set; }
        public string ExistingAccNo { get; set; }
        public string AssessmentNo { get; set; }
    }

    // 05. Job information - Contractor Bill Info
    public class ContractorInfoDto
    {
        public string ContractorName { get; set; }
        public DateTime? AllocatedDate { get; set; }
    }

    // 06. Estimation Information - Labour details
    public class LabourDetailDto
    {
        public string LabourCode { get; set; }
        public string ActivityDescription { get; set; }
        public decimal? UnitLabourHrs { get; set; }
        public decimal? UnitPrice { get; set; }
        public decimal? ItemQty { get; set; }
        public decimal? CebUnitPrice { get; set; }
        public decimal? CebLabourCost { get; set; }
    }

    //  Material Transaction Details
    public class MaterialTransactionDto
    {
        public string MatCode { get; set; }
        public decimal? UnitPrice { get; set; }
        public decimal? EstimateQty { get; set; }
        public decimal? EstimateCost { get; set; }
    }

    // 07. Job information - energizing (exported date, account no, acc created date)
    public class EnergizingBasicDto
    {
        public DateTime? ExportedDate { get; set; }
        public string AccountNo { get; set; }
        public DateTime? AccCreatedDate { get; set; }
    }

    // 08. Job Information - Revised Standard Estimate
    public class StandardEstimateDto
    {
        public decimal? FixedCost { get; set; }
        public decimal? VariableCost { get; set; }
        public decimal? SecurityDeposit { get; set; }
        public decimal? TemporaryDeposit { get; set; }
        public decimal? ConversionCost { get; set; }
        public decimal? LabourCost { get; set; }
        public decimal? TransportCost { get; set; }
        public decimal? OverheadCost { get; set; }
        public decimal? DamageCost { get; set; }
        public decimal? ContingencyCost { get; set; }
        public decimal? BoardCharge { get; set; }
        public decimal? Sscl { get; set; }
        public decimal? TotalCost { get; set; }
    }

    // 09. Job Information - energizing physical_closedate, status, jobcreated date + res_type grouping
    public class JobStatusHistoryDto
    {
        public string Status { get; set; }
        public DateTime? PhysicalClose { get; set; }     // apr_dt1
        public DateTime? HardClose { get; set; }         // conf_dt
        public DateTime? JobCreatedDate { get; set; }    // prj_ass_dt
        public string ResourceType { get; set; }         // MAT / LAB / OTHER
        public decimal? CommittedCost { get; set; }
    }

    // ────────────────────────────────────────────────
    // 12. Job Finalized Info (PCESTHMT)
    // ────────────────────────────────────────────────
    public class JobFinalizedDto
    {
        public DateTime? JobFinalizedDate { get; set; }   // conf_dt
        public DateTime? EstimatedDate { get; set; }   // estimate date
    }

    // 10. energizing date + meter information
    public class EnergizedInfoDto
    {
        public DateTime? EnergizedDate { get; set; }     // energized date
        public DateTime? finalized_date { get; set; }     // finalized date
    }

    // 11. Estimate Approval Information
    public class EstimateApprovalDto
    {
        public string ApprovedLevel { get; set; }     // approved_level
        public DateTime? ApprovedDate { get; set; }   // approved_date
        public string ApprovedTime { get; set; }      // approved_time
        public decimal? StandardCost { get; set; }    // standard_cost
        public decimal? DetailedCost { get; set; }    // detailed_cost
    }

    // ────────────────────────────────────────────────
    // 13. Contractor Bill Info (BILL_DETAIL)
    // ────────────────────────────────────────────────
    public class ContractorBillDto
    {
        public string ContractorBillNo { get; set; }     // bill_no
        public DateTime? ContractorBillDate { get; set; } // bill_date
    }

    // ────────────────────────────────────────────────
    // 14. PIV Detail DTO
    // ────────────────────────────────────────────────
    public class PivDetailDto
    {
        public string PivNo { get; set; }
        public string PivType { get; set; }
        public decimal? PivAmount { get; set; }
        public DateTime? PivDate { get; set; }
        public DateTime? PaidDate { get; set; }
        public string Status { get; set; }
    }

    // ────────────────────────────────────────────────
    // 11. Appointment Info (SPESTEDY)
    // ────────────────────────────────────────────────
    public class AppointmentInfoDto
    {
        public DateTime? AppointmentDate { get; set; }
    }

    public class ResourceCostSummaryDto // this is used to get the detailed summation related to the detail estimation
    {
        public string ResType { get; set; }              // res_type
        public decimal TotalEstimateCost { get; set; }   // SUM(estimate_cost)
    }
}