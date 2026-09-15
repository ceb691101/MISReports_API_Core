using System;
using System.Collections.Generic;

namespace MISReports_Api.Models.Catalog
{
    public class ReportCatalogItemModel
    {
        public string RepIdNo { get; set; }
        public string RepId { get; set; }
        public string ReportName { get; set; }
        public string CatCode { get; set; }
        public string CategoryName { get; set; }
        public string Description { get; set; }
        public string ParamList { get; set; }
        public List<string> ParameterDescriptions { get; set; }
        public string Path { get; set; }
        public int Favorite { get; set; }
        public int Active { get; set; }
        public bool HasAccess { get; set; }

        public ReportCatalogItemModel()
        {
            ParameterDescriptions = new List<string>();
        }
    }

    public class ReportCategorySummaryModel
    {
        public string CatCode { get; set; }
        public string CategoryName { get; set; }
        public int TotalReports { get; set; }
    }

    public class ReportCatalogResponseModel
    {
        public List<ReportCategorySummaryModel> Categories { get; set; }
        public List<ReportCatalogItemModel> Reports { get; set; }

        public ReportCatalogResponseModel()
        {
            Categories = new List<ReportCategorySummaryModel>();
            Reports = new List<ReportCatalogItemModel>();
        }
    }
}
