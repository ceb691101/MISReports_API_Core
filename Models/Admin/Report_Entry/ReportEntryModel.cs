using System;

namespace MISReports_Api.Models
{
    public class ReportEntryModel
    {
        public int RepIdNo { get; set; }
        public string RepId { get; set; }
        public string CatCode { get; set; }
        public string RepName { get; set; }
        public string ParamList { get; set; }
        public int Favorite { get; set; }
        public int Active { get; set; }
        public string Description { get; set; }
        public string Path { get; set; }
    }
}




