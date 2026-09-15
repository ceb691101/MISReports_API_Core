using System;

namespace MISReports_Api.Models.PIV
{
    public class AreaWiseSRPApplicationPIVtobePaidReportModel
    {
        public string Division_Name { get; set; }
        public string Province { get; set; }
        public string Area_nm { get; set; }
        public string Dept_nm { get; set; }
        public string Category { get; set; }
        public int No_of_pending_estimation { get; set; }
        public string Comp_nm { get; set; }
    }
}