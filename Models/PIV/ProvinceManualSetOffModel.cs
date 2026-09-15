using System;

namespace MISReports_Api.Models.PIV
{
    public class ProvinceManualSetOffModel
    {
        public string DeptId { get; set; }

        public string SPivNo { get; set; }
        public DateTime? SPivDate { get; set; }
        public decimal? SPivAmount { get; set; }
        public string SAccountCode { get; set; }
        public decimal? SAccountAmount { get; set; }

        public string OPivNo { get; set; }
        public DateTime? PaidDate { get; set; }

        public string CompNm { get; set; }
    }
}