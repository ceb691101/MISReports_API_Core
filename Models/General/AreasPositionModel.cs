using System;

namespace MISReports_Api.Models.General
{
    /// <summary>
    /// Represents a single reader-level row in the Areas Position report.
    /// Columns: Reader Code | Monthly Bill | Total Balance | No. of Months in Arrears | No. of Accounts
    /// </summary>
    public class AreasPositionModel
    {
        /// <summary>Reader code (reader_code from prn_dat_1)</summary>
        public string ReaderCode { get; set; }

        /// <summary>
        /// Net monthly charge = (kwh_charge + fuel_charge) - NR transaction amount,
        /// formatted as ###,###,###.#0
        /// </summary>
        public string MonthlyBill { get; set; }

        /// <summary>
        /// Sum of crnt_balance for all accounts under this reader,
        /// formatted as ###,###,###.#0
        /// </summary>
        public string TotalBalance { get; set; }

        /// <summary>
        /// Ratio = TotalBalance / MonthlyBill (months in arrears),
        /// formatted as ##0.#0
        /// </summary>
        public string NoOfMonthsInArrears { get; set; }

        /// <summary>Count of accounts under this reader, formatted as ###,###,##0</summary>
        public string NoOfAccounts { get; set; }

        /// <summary>Non-null when the DAO encountered a recoverable error for this row.</summary>
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Request parameters for the Areas Position report.
    /// </summary>
    public class AreasPositionRequest
    {
        /// <summary>Area code used to look up the max bill cycle and query prn_dat_1.</summary>
        public string AreaCode { get; set; }

        /// <summary>
        /// Bill cycle to report on.  When null/empty the DAO will resolve the
        /// max bill cycle automatically from the areas table.
        /// </summary>
        public string BillCycle { get; set; }
    }

    /// <summary>
    /// Wraps the resolved bill cycle that is returned alongside the report rows,
    /// so the frontend can display which cycle was actually used.
    /// </summary>
    public class AreasPositionResult
    {
        public string BillCycle { get; set; }
        public System.Collections.Generic.List<AreasPositionModel> Rows { get; set; }
    }
}