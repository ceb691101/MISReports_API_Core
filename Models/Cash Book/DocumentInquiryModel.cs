using System;

namespace MISReports_Api.Models
{
    public class DocumentInquiryModel
    {
        public string Category { get; set; }
        public DateTime? DocDt { get; set; }
        public string DocDate => DocDt?.ToString("yyyy/MM/dd");
        public string NonTaxabl { get; set; }
        public string DocNo { get; set; }
        public string ApprvUid1 { get; set; }
        public string ApprDt1 { get; set; }
        public string TranStatus { get; set; }
        public string Payee { get; set; }
        public string ChqDt { get; set; }
        public string ChqNo { get; set; }
        public string PymtDocno { get; set; }
        public string PpStatus { get; set; }
        public string CctName { get; set; }
    }
}