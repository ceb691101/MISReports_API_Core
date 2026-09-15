//Consolidated Income & Expenditure Regional Statement(Report)

// File: IncomeExpenditureRegionDetailedModel.cs
using System;

namespace MISReports_Api.Models
{
    public class IncomeExpenditureRegionDetailedModel
    {
        public string Title_cd { get; set; }
        public string Account { get; set; }
        public decimal Actual { get; set; }
        public string Catname { get; set; }
        public string Maxrev { get; set; } // Always empty string as per query
        public string Catcode { get; set; }
        public string Catflag { get; set; }
        public string Comp_nm { get; set; }
        public string Costctr { get; set; }
    }
}