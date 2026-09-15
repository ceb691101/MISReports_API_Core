using System;
using System.Collections.Generic;
using System.Linq;

namespace MISReports_Api.Models.Admin.Report_Parameters
{
    public class RepParaModel
    {
        public string ParaId { get; set; }
        public string ParaDesc { get; set; }
    }

    public class PopulateParamTsModel
    {
        public string RepId { get; set; }
        public string ParamList { get; set; }
    }

    public class SaveReportParamsResultModel
    {
        public string ParaName { get; set; }
        public bool Found { get; set; }
        public bool Inserted { get; set; }
        public bool Updated { get; set; }
    }

    public class PopulateParamTsResultModel
    {
        public int UpdatedRows { get; set; }
        public bool Success { get; set; }
        public int ProcessedReports { get; set; }
        public int ProcessedParams { get; set; }
        public int AppendedParams { get; set; }
        public int AlreadyExistingParams { get; set; }
    }

    public class ReportListItemModel
    {
        public string RepId { get; set; }
        public string ParamList { get; set; }
    }

    public class DeleteReportParamResultModel
    {
        public int DeletedRows { get; set; }
    }

    public class ReportParameterListItemModel
    {
        public string ParaId { get; set; }
        public string ParaName { get; set; }
        public string Description { get; set; }
        public bool Populated { get; set; }
    }
}