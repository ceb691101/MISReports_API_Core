#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
// Informix access through standard .NET database interfaces (System.Data.Common).
// The actual driver is chosen in configuration ("Informix:ProviderFactory"), so the same
// code runs with:
//   - IBM's DRDA driver on Linux:  "IBM.Data.Db2.DB2Factory, IBM.Data.Db2"   (package Net.IBM.Data.Db2-lnx)
//   - the OLE DB driver on Windows: "System.Data.OleDb.OleDbFactory, System.Data.OleDb" (package System.Data.OleDb, 32-bit Ifxoledbc)
// Connection strings must be in the chosen driver's format (see appsettings.json).
using System.Collections;
using System.Data;
using System.Data.Common;
using System.Reflection;

namespace MISReports_Api.Compat
{
    public static class Informix
    {
        private static DbProviderFactory? _factory;

        public static DbProviderFactory Factory =>
            _factory ?? throw new InvalidOperationException("Informix.Initialize was not called at startup (setting Informix:ProviderFactory).");

        /// <summary>Loads the driver named by an assembly-qualified factory type, e.g. "IBM.Data.Db2.DB2Factory, IBM.Data.Db2".</summary>
        public static void Initialize(string factoryTypeName)
        {
            Type type;
            try
            {
                type = Type.GetType(factoryTypeName, throwOnError: true)!;
            }
            catch (Exception ex) when (ex is System.IO.FileNotFoundException or TypeLoadException)
            {
                throw new InvalidOperationException(
                    $"The Informix driver '{factoryTypeName}' is not part of this build. " +
                    "Remove the setting Informix:ProviderFactory to use the driver this build includes, or rebuild with " +
                    "-p:InformixDriver=OleDb (Windows, 32-bit Ifxoledbc) or -p:InformixDriver=Db2 (IBM DRDA driver).", ex);
            }
            var instance = type.GetField("Instance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null)
                           ?? throw new InvalidOperationException($"{factoryTypeName} has no static Instance field.");
            _factory = (DbProviderFactory)instance;
        }

        /// <summary>For tests: use a specific factory instance.</summary>
        public static void Initialize(DbProviderFactory factory) => _factory = factory;

        public static DbConnection CreateConnection(string? connectionString)
        {
            var c = Factory.CreateConnection()!;
            c.ConnectionString = connectionString;
            return c;
        }

        public static DbCommand CreateCommand(string? commandText = null, DbConnection? connection = null, DbTransaction? transaction = null)
        {
            var cmd = Factory.CreateCommand()!;
            cmd.CommandText = commandText ?? "";
            cmd.Connection = connection;
            cmd.Transaction = transaction;
            return cmd;
        }

        public static DbParameter CreateParameter(string name, object? value)
        {
            var p = Factory.CreateParameter()!;
            p.ParameterName = name;
            p.Value = value ?? DBNull.Value;
            return p;
        }

        public static DbParameter CreateParameter(string name, OleDbType type)
        {
            var p = Factory.CreateParameter()!;
            p.ParameterName = name;
            p.DbType = type.ToDbType();
            return p;
        }

        public static DbParameter CreateParameter(string name, OleDbType type, int size)
        {
            var p = CreateParameter(name, type);
            p.Size = size;
            return p;
        }

        public static DbDataAdapter CreateDataAdapter(DbCommand selectCommand)
        {
            var a = Factory.CreateDataAdapter()!;
            a.SelectCommand = selectCommand;
            return a;
        }
    }

    /// <summary>The OLE DB parameter types used by this code base, mapped to standard DbType.</summary>
    public enum OleDbType { VarChar, Char, WChar, VarWChar, Integer, SmallInt, BigInt, Decimal, Numeric, Double, Single, Date, DBDate, DBTimeStamp, Boolean }

    public static class OleDbTypeExtensions
    {
        public static DbType ToDbType(this OleDbType t) => t switch
        {
            OleDbType.VarChar or OleDbType.VarWChar => DbType.String,
            OleDbType.Char or OleDbType.WChar => DbType.StringFixedLength,
            OleDbType.Integer => DbType.Int32,
            OleDbType.SmallInt => DbType.Int16,
            OleDbType.BigInt => DbType.Int64,
            OleDbType.Decimal or OleDbType.Numeric => DbType.Decimal,
            OleDbType.Double => DbType.Double,
            OleDbType.Single => DbType.Single,
            OleDbType.Date or OleDbType.DBDate => DbType.Date,
            OleDbType.DBTimeStamp => DbType.DateTime,
            OleDbType.Boolean => DbType.Boolean,
            _ => DbType.String
        };
    }

    /// <summary>OLE DB style helpers on standard parameter collections (Informix code only).</summary>
    public static class DbParameterCollectionExtensions
    {
        public static DbParameter AddWithValue(this DbParameterCollection parameters, string name, object? value)
        {
            var p = Informix.CreateParameter(name, value);
            parameters.Add(p);
            return p;
        }

        public static DbParameter Add(this DbParameterCollection parameters, string name, OleDbType type)
        {
            var p = Informix.CreateParameter(name, type);
            parameters.Add(p);
            return p;
        }

        public static DbParameter Add(this DbParameterCollection parameters, string name, OleDbType type, int size)
        {
            var p = Informix.CreateParameter(name, type, size);
            parameters.Add(p);
            return p;
        }
    }

    /// <summary>One error reported by the Informix driver (OLE DB or DB2 provider).</summary>
    public sealed class InformixError
    {
        public string Message { get; init; } = "";
        public int NativeError { get; init; }
        public string SQLState { get; init; } = "";
        public string Source { get; init; } = "";
        public override string ToString() => $"{SQLState} ({NativeError}): {Message}";
    }

    public static class DbExceptionExtensions
    {
        /// <summary>
        /// The driver's detailed error list (OleDbException.Errors / DB2Exception.Errors),
        /// read in a provider-neutral way. Empty if the driver has none.
        /// </summary>
        public static IList<InformixError> ProviderErrors(this DbException ex)
        {
            var list = new List<InformixError>();
            if (ex.GetType().GetProperty("Errors")?.GetValue(ex) is IEnumerable errors)
            {
                foreach (var e in errors)
                {
                    if (e == null) continue;
                    var t = e.GetType();
                    list.Add(new InformixError
                    {
                        Message = t.GetProperty("Message")?.GetValue(e) as string ?? "",
                        NativeError = Convert.ToInt32(t.GetProperty("NativeError")?.GetValue(e) ?? 0),
                        SQLState = (t.GetProperty("SQLState") ?? t.GetProperty("SqlState"))?.GetValue(e) as string ?? "",
                        Source = t.GetProperty("Source")?.GetValue(e) as string ?? "",
                    });
                }
            }
            if (list.Count == 0) list.Add(new InformixError { Message = ex.Message, NativeError = ex.ErrorCode });
            return list;
        }
    }

    /// <summary>
    /// Provider-neutral replacement for OleDbConnectionStringBuilder (used for diagnostics).
    /// Understands both "Data Source=db@server" (OLE DB) and "Database=..;Server=.." (DB2) forms.
    /// </summary>
    public class OleDbConnectionStringBuilder : DbConnectionStringBuilder
    {
        public OleDbConnectionStringBuilder(string? connectionString) { ConnectionString = connectionString ?? ""; }

        private string Get(string key) => TryGetValue(key, out var v) ? v?.ToString() ?? "" : "";

        public string DataSource
        {
            get
            {
                string ds = Get("Data Source");
                if (ds.Length > 0) return ds;
                string db = Get("Database"), server = Get("Server");
                return db.Length > 0 || server.Length > 0 ? $"{db}@{server}" : "";
            }
            set => this["Data Source"] = value;
        }

        public string Provider
        {
            get { string p = Get("Provider"); return p.Length > 0 ? p : (ContainsKey("Database") ? "IBM.Data.Db2" : ""); }
            set => this["Provider"] = value;
        }
    }

    public static class ObjectInitExtensions
    {
        /// <summary>Applies property settings that used to be an object initializer: new X(..) { A = 1 }.</summary>
        public static T With<T>(this T target, Action<T> init) { init(target); return target; }
    }
}
