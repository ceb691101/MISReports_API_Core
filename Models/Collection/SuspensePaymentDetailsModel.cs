using System;

namespace MISReports_Api.Models.Collection
{
    /// <summary>
    /// Response model for Suspense Payment Details report (Ordinary + Bulk)
    /// </summary>
    public class SuspensePaymentDetailsModel
    {
        public string Province { get; set; }
        public string AreaCode { get; set; }

        /// <summary>
        /// Display value in "{AreaCode} - {AreaName}" format, matching the sample report.
        /// </summary>
        public string AreaName { get; set; }

        public string AccountNumber { get; set; }
        public string BillCycle { get; set; }

        /// <summary>
        /// Only populated for Ordinary reports (crdt_code). Bulk source has no equivalent column.
        /// </summary>
        public string CreditCode { get; set; }

        public decimal SuspenseAmount { get; set; }
        public string TransacDate { get; set; }
        public string PaymentDate { get; set; }

        /// <summary>
        /// Only populated for Ordinary reports (post_date). Bulk source has no equivalent column.
        /// </summary>
        public string PostDate { get; set; }

        public string StubNo { get; set; }
        public string CounterNo { get; set; }

        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Request model for Suspense Payment Details reports
    /// </summary>
    public class SuspensePaymentDetailsRequest
    {
        public string FromDate { get; set; }   // transac_date / actl_pay_date lower bound
        public string ToDate { get; set; }     // transac_date / actl_pay_date upper bound
        public bool IsBulk { get; set; }       // true = Bulk (Billhsbhq), false = Ordinary (Billsmry)
    }
}