using System;

namespace MISReports_Api.Models.PIV
{
    public class ConsolidatedOutputVATModel
    {
        public string Name { get; set; }                // a.name
        public string TitleCd { get; set; }             // T1.title_cd
        public string Description { get; set; }         // CASE + subquery
        public string PivType { get; set; }             // gltitlm.title_nm (piv_type)
        public string VatNo { get; set; }               // substr(a.vat_reg_no,0,9)
        public DateTime? PivDate { get; set; }          // T1.paid_date
        public string PivNo { get; set; }               // T1.piv_no
        public decimal? VatAmt { get; set; }            // T2.amount (L5225)
        public decimal? PivAmount { get; set; }         // T1.PIV_amount
    }
}