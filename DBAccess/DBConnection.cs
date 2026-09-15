using System;
using System.Configuration;
using System.Data.OleDb;

namespace MISReports_Api.DBAccess
{
    public class DBConnection
    {
        private readonly string bulkConnectionString;
        private readonly string ordinaryConnectionString;

        public DBConnection()
        {
            try
            {
                // Initialize connection strings with proper error handling
                var bulkConnection = ConfigurationManager.ConnectionStrings["InformixBulkConnection"];
                var ordinaryConnection = ConfigurationManager.ConnectionStrings["InformixConnection"];

                if (bulkConnection == null)
                    throw new ConfigurationErrorsException("InformixBulkConnection string is missing from configuration");

                if (ordinaryConnection == null)
                    throw new ConfigurationErrorsException("InformixConnection string is missing from configuration");

                bulkConnectionString = bulkConnection.ConnectionString;
                ordinaryConnectionString = ordinaryConnection.ConnectionString;

                if (string.IsNullOrEmpty(bulkConnectionString))
                    throw new ConfigurationErrorsException("InformixBulkConnection string is empty");

                if (string.IsNullOrEmpty(ordinaryConnectionString))
                    throw new ConfigurationErrorsException("InformixConnection string is empty");

                System.Diagnostics.Trace.WriteLine("DBConnection initialized successfully");
                System.Diagnostics.Trace.WriteLine($"InformixConnection string length: {ordinaryConnectionString.Length}");
                System.Diagnostics.Trace.WriteLine($"InformixBulkConnection string length: {bulkConnectionString.Length}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"DBConnection initialization error: {ex.Message}");
                System.Diagnostics.Trace.WriteLine($"Stack Trace: {ex.StackTrace}");
                throw;
            }
        }

        // Test a specific connection type
        public bool TestConnection(out string errorMessage, bool useBulkConnection = true)
        {
            string connString = useBulkConnection ? bulkConnectionString : ordinaryConnectionString;
            errorMessage = null;

            if (string.IsNullOrEmpty(connString))
            {
                errorMessage = "Connection string is null or empty";
                System.Diagnostics.Trace.WriteLine(errorMessage);
                return false;
            }

            try
            {
                using (var conn = new OleDbConnection(connString))
                {
                    System.Diagnostics.Trace.WriteLine($"Attempting to open {(useBulkConnection ? "Bulk" : "Ordinary")} connection");
                    System.Diagnostics.Trace.WriteLine($"Connection string: {GetMaskedConnectionString(connString)}");

                    conn.Open();

                    System.Diagnostics.Trace.WriteLine($"{(useBulkConnection ? "Bulk" : "Ordinary")} connection opened successfully");
                    System.Diagnostics.Trace.WriteLine($"Connection state: {conn.State}, Server version: {conn.ServerVersion}");

                    return true;
                }
            }
            catch (OleDbException oleEx)
            {
                errorMessage = $"OleDb Error: {oleEx.Message}, Error Code: {oleEx.ErrorCode}";
                System.Diagnostics.Trace.WriteLine(errorMessage);
                return false;
            }
            catch (Exception ex)
            {
                errorMessage = $"Connection failed: {ex.Message}";
                System.Diagnostics.Trace.WriteLine(errorMessage);
                System.Diagnostics.Trace.WriteLine($"Stack Trace: {ex.StackTrace}");
                return false;
            }
        }

        // Test both connections
        public bool TestAllConnections(out string errorMessage)
        {
            errorMessage = string.Empty;
            bool bulkSuccess = TestConnection(out string bulkError, true);
            bool ordinarySuccess = TestConnection(out string ordinaryError, false);

            if (!bulkSuccess || !ordinarySuccess)
            {
                errorMessage = $"Bulk Connection: {(bulkSuccess ? "OK" : bulkError)} | " +
                               $"Ordinary Connection: {(ordinarySuccess ? "OK" : ordinaryError)}";
                System.Diagnostics.Trace.WriteLine($"Connection test failed: {errorMessage}");
                return false;
            }

            System.Diagnostics.Trace.WriteLine("Both connections tested successfully");
            return true;
        }

        // Get a specific connection type
        public OleDbConnection GetConnection(bool useBulkConnection = true)
        {
            string connString = useBulkConnection ? bulkConnectionString : ordinaryConnectionString;

            if (string.IsNullOrEmpty(connString))
            {
                throw new InvalidOperationException(
                    useBulkConnection ?
                    "Bulk connection string is not initialized" :
                    "Ordinary connection string is not initialized");
            }

            System.Diagnostics.Trace.WriteLine($"Creating {(useBulkConnection ? "Bulk" : "Ordinary")} connection");
            return new OleDbConnection(connString);
        }

        // Get connection string for a specific type
        public string GetConnectionString(bool useBulkConnection = true)
        {
            string connString = useBulkConnection ? bulkConnectionString : ordinaryConnectionString;

            if (string.IsNullOrEmpty(connString))
            {
                throw new InvalidOperationException(
                    useBulkConnection ?
                    "Bulk connection string is not initialized" :
                    "Ordinary connection string is not initialized");
            }

            return connString;
        }

        // Properties for backward compatibility
        public string BulkConnectionString
        {
            get
            {
                if (string.IsNullOrEmpty(bulkConnectionString))
                    throw new InvalidOperationException("Bulk connection string is not initialized");
                return bulkConnectionString;
            }
        }

        public string OrdinaryConnectionString
        {
            get
            {
                if (string.IsNullOrEmpty(ordinaryConnectionString))
                    throw new InvalidOperationException("Ordinary connection string is not initialized");
                return ordinaryConnectionString;
            }
        }

        // Helper method to mask sensitive information in connection string for logging
        private string GetMaskedConnectionString(string connectionString)
        {
            try
            {
                var builder = new OleDbConnectionStringBuilder(connectionString);

                // Mask sensitive information
                if (builder.ContainsKey("Password"))
                    builder["Password"] = "***";
                if (builder.ContainsKey("Pwd"))
                    builder["Pwd"] = "***";
                if (builder.ContainsKey("User ID"))
                    builder["User ID"] = "***";
                if (builder.ContainsKey("UID"))
                    builder["UID"] = "***";

                return builder.ConnectionString;
            }
            catch
            {
                // If parsing fails, return a safe version
                return "Connection string parsing failed";
            }
        }

        // Additional diagnostic method
        public string GetConnectionStatus()
        {
            bool bulkOk = TestConnection(out string bulkError, true);
            bool ordinaryOk = TestConnection(out string ordinaryError, false);

            return $"Bulk Connection: {(bulkOk ? "OK" : bulkError)}, " +
                   $"Ordinary Connection: {(ordinaryOk ? "OK" : ordinaryError)}";
        }
    }
}