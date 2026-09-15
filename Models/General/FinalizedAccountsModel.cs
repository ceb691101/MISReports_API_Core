using System.Collections.Generic;

namespace MISReports_Api.Models.Collection
{
    public class FinalizedAccountsRequest
    {
        public string ProvinceCode { get; set; }
        public string AreaCode { get; set; }
        public string BillCycle { get; set; }
        public bool BalanceChecked { get; set; }
        public string BalanceOperator { get; set; }
        public string BalanceValue { get; set; }
        public bool DaysChecked { get; set; }
        public string DaysOperator { get; set; }
        public string DaysValue { get; set; }
    }

    public class FinalizedAccountsRecord
    {
        public string AccountNumber { get; set; }
        public decimal CurrentBalance { get; set; }
        public string CustomerName { get; set; }
        public string Address { get; set; }
        public string LastReadDate { get; set; }
        public string FinalizedDate { get; set; }
        public string MeterNo1 { get; set; }
        public string LastRead1 { get; set; }
        public string MeterNo2 { get; set; }
        public string LastRead2 { get; set; }
        public string MeterNo3 { get; set; }
        public string LastRead3 { get; set; }
        public decimal SecurityDeposit { get; set; }
    }

    public class FinalizedAccountsResponse
    {
        public List<FinalizedAccountsRecord> Records { get; set; }
        public string ErrorMessage { get; set; }
        public int RecordCount { get; set; }
    }

    public class FinalizedAccountsDropdowns
    {
        public List<ProvinceOption> Provinces { get; set; }
        public List<AreaOption> Areas { get; set; }
        public List<string> BillCycles { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class ProvinceOption
    {
        public string ProvCode { get; set; }
        public string ProvName { get; set; }
    }

    public class AreaOption
    {
        public string AreaCode { get; set; }
        public string AreaName { get; set; }
    }
}