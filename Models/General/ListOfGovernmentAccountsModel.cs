namespace MISReports_Api.Models.General
{
    // ── Request ───────────────────────────────────────────────────────────────
    /// <summary>
    /// Passed from GeneralController into GovernmentAccountsDao.
    /// ReportType: "area" | "department"
    /// </summary>
    public class GovernmentAccountsRequest
    {
        public string BillCycle { get; set; }
        public string ReportType { get; set; }
        public string AreaCode { get; set; }
        public string DepartmentCode { get; set; }
    }

    // ── Report row ────────────────────────────────────────────────────────────
    /// <summary>
    /// One row returned by GetGovernmentAccountsReport().
    /// Raw numeric fields allow the frontend to sort/aggregate;
    /// formatted string fields are ready for display.
    /// </summary>
    public class GovernmentAccountsModel
    {
        // Context (set by DAO after mapping)
        public string AreaCode { get; set; }
        public string BillCycle { get; set; }
        public string DepartmentCode { get; set; }

        // From prn_dat_1
        public string AccountNumber { get; set; }
        public string CustomerName { get; set; }   // cust_fname + cust_lname
        public string Address { get; set; }   // address_1 + address_2 + address_3

        // Raw numeric values (for sorting / calculations on frontend)
        public decimal RawCurrentBalance { get; set; }
        public decimal RawKwhCharge { get; set; }
        public decimal RawAverageConsumption { get; set; }

        // Formatted display strings
        public string CurrentBalance { get; set; }
        public string KwhCharge { get; set; }
        public string AverageConsumption { get; set; }

        public string ErrorMessage { get; set; }
    }

    // ── Max bill cycle ────────────────────────────────────────────────────────
    /// <summary>
    /// Returned by GovernmentAccountsDao.GetMaxBillCycle().
    /// </summary>
    public class GovMaxBillCycleModel
    {
        public string MaxBillCycle { get; set; }
        public string ErrorMessage { get; set; }
    }

    // ── Areas dropdown ────────────────────────────────────────────────────────
    /// <summary>
    /// One row from GovernmentAccountsDao.GetAreas().
    /// </summary>
    public class GovAreaModel
    {
        public string AreaCode { get; set; }
        public string AreaName { get; set; }
    }

    // ── Departments dropdown ──────────────────────────────────────────────────
    /// <summary>
    /// One row from GovernmentAccountsDao.GetDepartments().
    /// </summary>
    public class DepartmentModel
    {
        public string DepartmentCode { get; set; }
        public string DepartmentName { get; set; }
    }
}