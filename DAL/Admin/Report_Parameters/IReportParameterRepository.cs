using System.Collections.Generic;
using MISReports_Api.Models.Admin.Report_Parameters;

namespace MISReports_Api.DAL.Admin.Report_Parameters
{
    public interface IReportParameterRepository
    {
        List<ParameterItemModel> GetParameters();
        ParameterUpsertResultModel UpsertParameter(string name, string description);
        int DeleteParameter(string name);
        List<ReportItemModel> GetReports();
        PopulateResultModel PopulateMissingParameters();
    }
}
