using MISReports_Api.Compat;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Text.RegularExpressions;
using Oracle.ManagedDataAccess.Client;

namespace MISReports_Api.DBAccess
{
    /// <summary>
    /// Helpers for building SQL safely. Values from requests are always sent to the
    /// database as parameters and never pasted into the SQL text, which prevents
    /// SQL injection. Use these instead of string interpolation ($"... '{value}'").
    /// </summary>
    public static class DbCommandHelper
    {
        private static readonly Regex SafeIdentifierPart = new Regex("^[A-Za-z0-9_]+$", RegexOptions.Compiled);

        private static readonly HashSet<string> ComparisonOperators =
            new HashSet<string> { "=", "<>", "!=", ">", "<", ">=", "<=" };

        /// <summary>
        /// Creates an OleDb (Informix) command and binds the values, in order,
        /// to the '?' placeholders in the SQL.
        /// </summary>
        public static DbCommand CreateOleDbCommand(string sql, DbConnection connection, params object[] values)
        {
            var command = Informix.CreateCommand(sql, connection);
            if (values != null)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    command.Parameters.AddWithValue("@p" + i.ToString(CultureInfo.InvariantCulture), values[i] ?? DBNull.Value);
                }
            }
            return command;
        }

        /// <summary>
        /// Creates an Oracle command with named bind variables (":name" in the SQL).
        /// A name may appear several times in the SQL; it is bound once.
        /// </summary>
        public static OracleCommand CreateOracleCommand(string sql, OracleConnection connection, params (string Name, string Value)[] parameters)
        {
            var command = new OracleCommand(sql, connection) { BindByName = true };
            if (parameters != null)
            {
                foreach (var p in parameters)
                {
                    command.Parameters.Add(new OracleParameter(p.Name, OracleDbType.Varchar2) { Value = (object)p.Value ?? DBNull.Value });
                }
            }
            return command;
        }

        /// <summary>
        /// True when the value contains only letters, digits and underscores, so it is
        /// safe to use as part of a table or column name (which cannot be a parameter).
        /// </summary>
        public static bool IsSafeIdentifierPart(string value)
        {
            return !string.IsNullOrEmpty(value) && SafeIdentifierPart.IsMatch(value);
        }

        /// <summary>
        /// Returns the operator if it is a plain comparison operator, otherwise throws.
        /// </summary>
        public static string RequireComparisonOperator(string value, string fieldName)
        {
            string op = value?.Trim();
            if (op == null || !ComparisonOperators.Contains(op))
            {
                throw new ArgumentException($"{fieldName} must be one of: =, <>, !=, >, <, >=, <=.");
            }
            return op;
        }

        /// <summary>Parses a whole number from a request value, otherwise throws.</summary>
        public static int RequireInteger(string value, string fieldName)
        {
            if (!int.TryParse(value?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
            {
                throw new ArgumentException($"{fieldName} must be a whole number.");
            }
            return result;
        }

        /// <summary>Parses a number (decimals allowed) from a request value, otherwise throws.</summary>
        public static decimal RequireDecimal(string value, string fieldName)
        {
            if (!decimal.TryParse(value?.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out decimal result))
            {
                throw new ArgumentException($"{fieldName} must be a number.");
            }
            return result;
        }
    }
}
