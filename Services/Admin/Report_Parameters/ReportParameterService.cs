using System;
using System.Collections.Generic;
using MISReports_Api.DAL.Admin.Report_Parameters;
using MISReports_Api.Models.Admin.Report_Parameters;
using NLog;

namespace MISReports_Api.Services.Admin.Report_Parameters
{
    public class ReportParameterService : IReportParameterService
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly IReportParameterRepository _repository;

        public ReportParameterService() : this(new ReportParameterRepository())
        {
        }

        public ReportParameterService(IReportParameterRepository repository)
        {
            _repository = repository;
        }

        public List<ParameterItemModel> GetParameters()
        {
            return _repository.GetParameters();
        }

        public ParameterUpsertResultModel SaveParameter(string name, string description)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Parameter name is required.");
            }

            var trimmedName = name.Trim();
            if (trimmedName.Length > 100)
            {
                throw new ArgumentException("Parameter name cannot exceed 100 characters.");
            }

            return _repository.UpsertParameter(trimmedName, description?.Trim());
        }

        public int DeleteParameter(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Parameter name is required.");
            }

            return _repository.DeleteParameter(name.Trim());
        }

        public List<ReportItemModel> GetReports()
        {
            return _repository.GetReports();
        }

        public PopulateResultModel Populate()
        {
            try
            {
                return _repository.PopulateMissingParameters();
            }
            catch (Oracle.ManagedDataAccess.Client.OracleException ex) when (ex.Number == 1)
            {
                Logger.Warn(ex, "Populate encountered concurrent insert conflict. Safe to retry.");
                throw new InvalidOperationException("Populate encountered concurrent updates. Please retry.");
            }
        }
    }
}
