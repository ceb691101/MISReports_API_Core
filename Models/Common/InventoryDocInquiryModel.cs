using System;
using System.Collections.Generic;
using System.Linq;
namespace MISReports_Api.Models.Accounts
{
    public class InventoryDocInquiryModel
    {
        public string DocNo { get; set; }
        public string DocPf { get; set; }
        public DateTime? TrxDt { get; set; }
        public string EntBy { get; set; }
        public string ModiBy { get; set; }
        public string ApprvUid1 { get; set; }
        public string IsRef { get; set; }
        public string DesDeptId { get; set; }
        public string IssueTo { get; set; }
        public string RcRef { get; set; }
        public string SrcDocNo { get; set; }
        public string SrcDeptId { get; set; }
        public string Ref1 { get; set; }
        public string Ref2 { get; set; }
        public string Ref3 { get; set; }
        public string Ref4 { get; set; }
        public decimal? TrxnVal { get; set; }
        public string Remarks { get; set; }
        public decimal? YrInd { get; set; }
        public decimal? MthInd { get; set; }
        public string TrxType { get; set; }
        public string MatCd { get; set; }
        public decimal? TrxQty { get; set; }
        public decimal? UnitCost { get; set; }
        public decimal? TrxVal { get; set; }
        public string WrhCd { get; set; }
        public string GradeCd { get; set; }
        public string TranStatus { get; set; }
    }
}