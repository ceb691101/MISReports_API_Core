using System;
using System.Collections.Generic;

namespace MISReports_Api.Models.General
{
    public class MeterReadingCompletionStatusItem
    {
        public string Province { get; set; }
        public string AreaCode { get; set; }
        public string AreaName { get; set; }
        public string LastCompletedCycle { get; set; }
        public string LastReadingCompletedPack { get; set; }
        public string LastPackCompletedDate { get; set; }
        public string CurrentCycle { get; set; }
        public string CurrentReadingCompletedPack { get; set; }
        public string CurrentPackCompletedDate { get; set; }
        public string Remarks { get; set; }
    }

    public class MeterReadingCompletionStatusResponse
    {
        public int CurrentCycle { get; set; }
        public string CurrentCycleLabel { get; set; }
        public int PreviousCycle { get; set; }
        public string PreviousCycleLabel { get; set; }
        public List<MeterReadingCompletionStatusItem> Rows { get; set; } = new List<MeterReadingCompletionStatusItem>();
    }
}

