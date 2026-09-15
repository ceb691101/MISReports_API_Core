using System;
using System.Collections.Generic;

namespace MISReports_Api.Models.Admin.Report_Parameters
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }

        public static ApiResponse<T> Ok(T data, string message)
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message,
                Data = data
            };
        }

        public static ApiResponse<T> Fail(string message, T data = default(T))
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Data = data
            };
        }
    }

    public class ParameterRequestModel
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class ParameterItemModel
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class ReportItemModel
    {
        public string RepId { get; set; }
        public int ParameterCount { get; set; }
    }

    public class ParameterUpsertResultModel
    {
        public string Name { get; set; }
        public bool Inserted { get; set; }
        public bool Updated { get; set; }
    }

    public class PopulateResultModel
    {
        public int ReportsCount { get; set; }
        public int ParametersCount { get; set; }
        public int InsertedRows { get; set; }
        public int AlreadyExistingRows { get; set; }
    }
}
