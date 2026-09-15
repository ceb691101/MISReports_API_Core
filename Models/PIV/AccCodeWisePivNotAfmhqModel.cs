using System;

namespace MISReports_Api.Models.PIV
{
    public class AccCodeWisePivNotAfmhqModel
    {
        public string Company { get; set; }           // the mapped group: GENE, TRANS, DISCO1, ...
        public string DeptId { get; set; }
        public string PivNo { get; set; }
        public string PivReceiptNo { get; set; }
        public DateTime? PivDate { get; set; }
        public DateTime? PaidDate { get; set; }
        public string AccountCode { get; set; }
        public decimal? Amount { get; set; }
        public string CctName { get; set; }           // name of issuing dept
        public string CctName1 { get; set; }          // name of paid dept (:costctr)
    }
}