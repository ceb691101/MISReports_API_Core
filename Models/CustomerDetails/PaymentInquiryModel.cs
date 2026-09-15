using System.Collections.Generic;

namespace MISReports_Api.Models.CustomerDetails
{
    public class PaymentInquiryRequest
    {
        public string AcctNo { get; set; }
        public string FromDate { get; set; }
    }

    public class LatestUpdateTimeRecord
    {
        public string Agent { get; set; }
        public string Center { get; set; }
        public string LastUpdate { get; set; }
        public string AgentName { get; set; }
        public string CenterName { get; set; }
    }

    public class PaymentInquiryPaymentRecord
    {
        public string TransDate { get; set; }
        public string TransAmt { get; set; }
        public string Center { get; set; }
        public string CountNo { get; set; }
        public string PayMode { get; set; }
        public string TransTime { get; set; }
        public string TransType { get; set; }
        public string StubNo { get; set; }
        public string Agent { get; set; }
        public string UsrLot { get; set; }
        public string AgentName { get; set; }
        public string CenterName { get; set; }
        public string CounterName { get; set; }
        public string CodeDescription { get; set; }
        public string ChequeMoneyOrderNo { get; set; }
    }

    public class PaymentInquiryResponse
    {
        public string AccountNumber { get; set; }
        public string AreaName { get; set; }
        public string CustomerName { get; set; }
        public string CustomerType { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string Address3 { get; set; }
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public decimal TotalAmount { get; set; }
        public List<PaymentInquiryPaymentRecord> PaymentRecords { get; set; } = new List<PaymentInquiryPaymentRecord>();
        public string ErrorMessage { get; set; }
    }

    public class LatestUpdateTimeResponse
    {
        public List<LatestUpdateTimeRecord> Records { get; set; }
        public string ErrorMessage { get; set; }
    }

    // POS Counter Collection Breakup Models
    public class ProvinceData
    {
        public string ProvName { get; set; }
        public string ProvCode { get; set; }
        public string ProvSvrName { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class ProvinceListResponse
    {
        public List<ProvinceData> Provinces { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class AreaData
    {
        public string AreaCode { get; set; }
        public string AreaName { get; set; }
    }

    public class AreaListResponse
    {
        public List<AreaData> Areas { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class CounterData
    {
        public string CounterNo { get; set; }
        public string CounterName { get; set; }
    }

    public class CounterListResponse
    {
        public List<CounterData> Counters { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class PayModeData
    {
        public string PayMode { get; set; }
        public string CodeDescription { get; set; }
    }

    public class PayModeListResponse
    {
        public List<PayModeData> PayModes { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class BillTypeData
    {
        public string BillType { get; set; }
    }

    public class BillTypeListResponse
    {
        public List<BillTypeData> BillTypes { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class POSCounterCollectionRequest
    {
        public string TransDate { get; set; }
        public string ProvCode { get; set; }
        public string AreaCode { get; set; }
        public string CounterNo { get; set; }
        public string PayMode { get; set; }
        public string PayType { get; set; }
    }

    public class POSCounterCollectionRecord
    {
        public string AccountNo { get; set; }
        public string CounterNo { get; set; }
        public string StubNo { get; set; }
        public string TransAmount { get; set; }
        public string PayMode { get; set; }
        public string TransType { get; set; }
        public string PIVNo { get; set; }
        public string AreaCode { get; set; }
        public string CounterName { get; set; }
        public string PayModeDescription { get; set; }
    }

    public class POSCounterCollectionResponse
    {
        public string TransDate { get; set; }
        public string Province { get; set; }
        public string Area { get; set; }
        public string Counter { get; set; }
        public decimal TotalAmount { get; set; }
        public int RecordCount { get; set; }
        public List<POSCounterCollectionRecord> Records { get; set; } = new List<POSCounterCollectionRecord>();
        public string ErrorMessage { get; set; }
    }
}
