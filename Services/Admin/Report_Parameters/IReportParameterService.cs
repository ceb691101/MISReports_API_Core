using System.Collections.Generic;
using MISReports_Api.Models.Admin.Report_Parameters;

namespace MISReports_Api.Services.Admin.Report_Parameters
{
    public interface IReportParameterService
    {
        List<ParameterItemModel> GetParameters();
        ParameterUpsertResultModel SaveParameter(string name, string description);
        int DeleteParameter(string name);
        List<ReportItemModel> GetReports();
        PopulateResultModel Populate();
    }
}
