using System;
using System.Collections.Generic;
using System.Linq;

// Models/GrnRaisedForPurchasingModel.cs
namespace MISReports_Api.Models
{
    public class GrnRaisedForPurchasingModel
    {
        public string MatCd { get; set; }
        public string MatNm { get; set; }
        public decimal? Qty { get; set; }
        public decimal? Value { get; set; }
    }
}