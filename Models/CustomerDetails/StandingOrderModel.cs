using System.Collections.Generic;

namespace MISReports_Api.Models.CustomerDetails
{
    public class StandingOrderRequest
    {
        public string AcctNo { get; set; }
    }

    public class StandingOrderBillRecord
    {
        public string BillMonth { get; set; }
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public double KwhUnits { get; set; }
        public decimal KwhCharge { get; set; }
        public decimal Payments { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal ClosingBalance { get; set; }
        public string BillProcessTime { get; set; }
        public string RequestTimeBank { get; set; }
        public string RequestStatus { get; set; }
    }

    public class StandingOrderResponse
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public string AccountNumber { get; set; }
        public string Status { get; set; }
        public bool IsRegistered { get; set; }
        public List<StandingOrderBillRecord> Records { get; set; } = new List<StandingOrderBillRecord>();
        public string ErrorMessage { get; set; }
    }
}

