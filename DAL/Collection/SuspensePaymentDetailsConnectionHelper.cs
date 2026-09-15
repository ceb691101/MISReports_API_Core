using MISReports_Api.DBAccess;
using System.Data.OleDb;

namespace MISReports_Api.DAL.Collection
{
    internal static class SuspensePaymentDetailsConnectionHelper
    {
        public static OleDbConnection GetPaymentConsoleConnection(DBConnection dbConnection)
        {
            var connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["InformixPmntConsld"]?.ConnectionString;
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new System.Configuration.ConfigurationErrorsException("InformixPmntConsld connection string is missing from configuration");

            return new OleDbConnection(connectionString);
        }
    }
}
