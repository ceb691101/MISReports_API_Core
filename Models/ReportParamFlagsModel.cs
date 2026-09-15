using System;
using System.Collections.Generic;
using System.Linq;

namespace MISReports_Api.Models
{
    public class ReportParamFlagsModel
    {
        public string RepId { get; set; }
        public string RepName { get; set; }
        public Dictionary<string, int> Params { get; set; }
    }
}