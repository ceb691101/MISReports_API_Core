using System;

namespace MISReports_Api.Models.PIV
{
    public class ProvincialConsolidatedOutputVATModel
    {
        public string Name { get; set; }
        public string TitleCd { get; set; }
        public string Description { get; set; }
        public string PivType { get; set; }
        public string VatNo { get; set; }
        public DateTime? PivDate { get; set; }
        public string PivNo { get; set; }
        public decimal? VatAmt { get; set; }
        public decimal? PivAmount { get; set; }
        public string CompNm { get; set; }     // added from your original SQL
    }
}