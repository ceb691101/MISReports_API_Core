#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
// Replacement for System.Configuration.ConfigurationManager, backed by ASP.NET Core
// configuration (appsettings.json, environment variables such as
// ConnectionStrings__InformixConnection, mounted secret files).
using System.Collections.Specialized;
using Microsoft.Extensions.Configuration;

namespace MISReports_Api.Compat
{
    public static class ConfigurationManager
    {
        private static IConfiguration? _configuration;

        public static void Initialize(IConfiguration configuration) => _configuration = configuration;

        private static IConfiguration Config =>
            _configuration ?? throw new InvalidOperationException("ConfigurationManager.Initialize was not called at startup.");

        /// <summary>Section "ConnectionStrings". Provider names (optional): section "ConnectionStringProviders".</summary>
        public static ConnectionStringSettingsCollection ConnectionStrings => new(Config);

        /// <summary>Section "AppSettings" (keys may contain ':' as in Web.config, written as "Gateway:Mode").</summary>
        public static NameValueCollection AppSettings
        {
            get
            {
                var values = new NameValueCollection();
                foreach (var item in Config.GetSection("AppSettings").AsEnumerable(makePathsRelative: true))
                    if (item.Value != null) values[item.Key] = item.Value;
                return values;
            }
        }
    }

    public sealed class ConnectionStringSettingsCollection
    {
        private readonly IConfiguration _config;
        internal ConnectionStringSettingsCollection(IConfiguration config) => _config = config;

        public ConnectionStringSettings? this[string name]
        {
            get
            {
                string? value = _config.GetConnectionString(name);
                return value == null ? null : new ConnectionStringSettings(name, value, _config[$"ConnectionStringProviders:{name}"]);
            }
        }
    }

    public sealed class ConnectionStringSettings
    {
        public ConnectionStringSettings(string name, string connectionString, string? providerName)
        { Name = name; ConnectionString = connectionString; ProviderName = providerName; }
        public string Name { get; }
        public string ConnectionString { get; }
        public string? ProviderName { get; }
    }

    public class ConfigurationErrorsException : Exception
    {
        public ConfigurationErrorsException(string message) : base(message) { }
        public ConfigurationErrorsException(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>Replacement for System.Web.Hosting.HostingEnvironment.</summary>
    public static class HostingEnvironment
    {
        private static string _root = AppContext.BaseDirectory;
        public static void Initialize(string contentRoot) => _root = contentRoot;
        public static string ApplicationPhysicalPath => _root.EndsWith(Path.DirectorySeparatorChar) ? _root : _root + Path.DirectorySeparatorChar;

        /// <summary>"~/App_Data/x.jar" -> absolute path under the application folder.</summary>
        public static string MapPath(string virtualPath)
        {
            string relative = virtualPath.TrimStart('~').TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
            return Path.Combine(_root, relative);
        }
    }
}
