using System;

namespace MISReports_Api.Models.PIV
{
    public class ProvinceSetOffModel
    {
        public string DeptId { get; set; }              // c.dept_id
        public string SPivNo { get; set; }              // c.piv_no as s_piv_no
        public DateTime? SPivDate { get; set; }         // c.piv_date as s_piv_date
        public decimal? SPivAmount { get; set; }        // c.piv_amount as s_piv_amount
        public string SAccountCode { get; set; }        // a.account_code as s_account_code
        public decimal? SAccountAmount { get; set; }    // a.amount as s_account_amount

        public string OPivNo { get; set; }              // c.SETOFF_FROM as o_piv_no
        public DateTime? OPivDate { get; set; }         // c1.piv_date as o_piv_date
        public decimal? OPivAmount { get; set; }        // c1.piv_amount as o_piv_amount
        public string OAccountCode { get; set; }        // a1.account_code as o_account_code
        public decimal? OAccountAmount { get; set; }    // a1.amount as o_account_amount

        public string CompNm { get; set; }              // company name from subquery
    }
}