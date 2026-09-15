using System;

namespace MISReports_Api.Models.Dashboard
{
    /// <summary>
    /// Represents a single daily row for Sales and Collection chart output.
    /// </summary>
    public class SalesAndCollectionModel
    {
        public string Date { get; set; }
        public decimal Collection { get; set; }

        // Populated by the API — not from DB
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Wraps both Ordinary (bill_type='O') and Bulk (bill_type='B') daily result sets.
    /// </summary>
    public class SalesAndCollectionRangeResult
    {
        /// <summary>
        /// Ordinary customer results (bill_type = 'O'), ordered by date ASC.
        /// </summary>
        public System.Collections.Generic.List<SalesAndCollectionModel> OrdinaryData { get; set; }
            = new System.Collections.Generic.List<SalesAndCollectionModel>();

        /// <summary>
        /// Bulk customer results (bill_type = 'B'), ordered by date ASC.
        /// </summary>
        public System.Collections.Generic.List<SalesAndCollectionModel> BulkData { get; set; }
            = new System.Collections.Generic.List<SalesAndCollectionModel>();
    }
}