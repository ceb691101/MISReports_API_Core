using System;
using System.Collections.Generic;
using System.Linq;


namespace MISReports_Api.Models
{
    public class PHVValidationModel
    {
        public string MatCd { get; set; }
        public string MatNm { get; set; }
        public string UomCd { get; set; }
        public string GradeCd { get; set; }
        public decimal QtyOnHand { get; set; }
        public decimal CntedQty { get; set; }
        public decimal UnitPrice { get; set; }
        public string Reason { get; set; }   
    }
}
