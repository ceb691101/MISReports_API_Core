using MISReports_Api.Compat;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MISReports_Api.Services.Reporting
{
    public class PHVObsoleteIdleJasperReportService
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private const int ProcessTimeoutMs = 10 * 60 * 1000;
        private readonly string _javaExecutable;
        private readonly string _jarPath;
        private readonly string _templatePath;

        public PHVObsoleteIdleJasperReportService()
        {
            _javaExecutable = ResolveJavaExecutable();
            _jarPath = ResolveAppRelativePath(
                "PHV_OBSOLETE_IDLE_JASPER_JAR",
                "~/JasperTools/PHVObsoleteIdleReportTool/target/phv-obsolete-idle-report.jar");
            _templatePath = ResolveAppRelativePath(
                "PHV_OBSOLETE_IDLE_JRXML_TEMPLATE",
                "~/JasperTools/PHVObsoleteIdleReportTool/src/main/resources/reports/phv_obsolete_idle.jrxml");
        }

        public Task<byte[]> GeneratePhvObsoleteIdlePdfAsync(
            IEnumerable<object> rows,
            string costCenterLabel,
            string warehouseCode,
            int repYear,
            int repMonth)
        {
            return GeneratePhvObsoleteIdleReportAsync(
                rows,
                costCenterLabel,
                warehouseCode,
                repYear,
                repMonth,
                "~/JasperTools/PHVObsoleteIdleReportTool/src/main/resources/reports/phv_obsolete_idle.jrxml",
                "pdf");
        }

        public async Task<byte[]> GeneratePhvObsoleteIdleReportAsync(
            IEnumerable<object> rows,
            string costCenterLabel,
            string warehouseCode,
            int repYear,
            int repMonth,
            string templateRelativePath,
            string format = "pdf")
        {
            if (rows == null)
                throw new ArgumentNullException(nameof(rows));

            var rowList = rows.ToList();
            if (rowList.Count == 0)
                throw new JasperReportExecutionException("No data was supplied to the report renderer.");

            if (!File.Exists(_jarPath))
                throw new JasperReportExecutionException($"Jasper report JAR was not found at '{_jarPath}'. Build and deploy the standalone Java tool first.");

            var templateFullPath = ResolveAppRelativePath("PHV_OBSOLETE_IDLE_JRXML_TEMPLATE", templateRelativePath);
            if (!File.Exists(templateFullPath))
                throw new JasperReportExecutionException($"Jasper template was not found at '{templateFullPath}'.");

            var workingDirectory = Path.Combine(Path.GetTempPath(), "ceb-reporting", "phv-obsolete-idle", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(workingDirectory);

            var inputJsonPath = Path.Combine(workingDirectory, "input.json");
            var isCsv = format.ToLower() == "csv";
            var outputFileName = isCsv ? "output.csv" : "output.pdf";
            var outputReportPath = Path.Combine(workingDirectory, outputFileName);

            try
            {
                var payload = rowList.Select(row => new
                {
                    DOC_NO = GetStringValue(row, "DocumentNo", "DOC_NO"),
                    MAT_CD = GetStringValue(row, "MaterialCode", "MAT_CD"),
                    MAT_NM = GetStringValue(row, "MaterialName", "MAT_NM"),
                    GRADE_CD = GetStringValue(row, "GradeCode", "GRADE_CD"),
                    PHV_DT = FormatDateValue(GetValue(row, "PhvDate", "PHV_DT")),
                    QTY_ON_HAND = GetDecimalValue(row, "QtyOnHand", "QTY_ON_HAND"),
                    STOCKBOOK = GetDecimalValue(row, "StockBook", "STOCKBOOK"),
                    REASON = GetStringValue(row, "Reason", "REASON"),
                    CCT_NAME = GetStringValue(row, "CostCentreName", "CCT_NAME")
                }).ToList();

                var json = JsonConvert.SerializeObject(payload, Formatting.None);
                File.WriteAllText(inputJsonPath, json, new UTF8Encoding(false));

                var arguments = new StringBuilder()
                    .Append("-jar ").Append(QuoteArgument(_jarPath))
                    .Append(" --input ").Append(QuoteArgument(inputJsonPath))
                    .Append(" --output ").Append(QuoteArgument(outputReportPath))
                    .Append(" --template ").Append(QuoteArgument(templateFullPath))
                    .Append(" --costctr ").Append(QuoteArgument(costCenterLabel ?? string.Empty))
                    .Append(" --whcode ").Append(QuoteArgument(warehouseCode ?? string.Empty))
                    .Append(" --repyear ").Append(repYear.ToString())
                    .Append(" --repmonth ").Append(repMonth.ToString())
                    .ToString();

                var startInfo = new ProcessStartInfo
                {
                    FileName = _javaExecutable,
                    Arguments = arguments,
                    WorkingDirectory = workingDirectory,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };

                var stdout = new StringBuilder();
                var stderr = new StringBuilder();

                using (var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true })
                {
                    process.OutputDataReceived += (_, args) =>
                    {
                        if (!string.IsNullOrWhiteSpace(args.Data))
                        {
                            stdout.AppendLine(args.Data);
                        }
                    };

                    process.ErrorDataReceived += (_, args) =>
                    {
                        if (!string.IsNullOrWhiteSpace(args.Data))
                        {
                            stderr.AppendLine(args.Data);
                        }
                    };

                    if (!process.Start())
                        throw new JasperReportExecutionException("Unable to start the Jasper report process.");

                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    if (!await WaitForExitAsync(process, ProcessTimeoutMs).ConfigureAwait(false))
                    {
                        TryKillProcess(process);
                        throw new JasperReportExecutionException("The Jasper report process timed out.");
                    }

                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        throw new JasperReportExecutionException(
                            $"Jasper report process failed with exit code {process.ExitCode}.\nSTDOUT:\n{stdout}\nSTDERR:\n{stderr}");
                    }
                }

                if (!File.Exists(outputReportPath))
                {
                    throw new JasperReportExecutionException(
                        $"The Jasper report process completed, but no report was created at '{outputReportPath}'.\nSTDOUT:\n{stdout}\nSTDERR:\n{stderr}");
                }

                var reportBytes = File.ReadAllBytes(outputReportPath);
                if (!isCsv && !LooksLikePdf(reportBytes))
                {
                    throw new JasperReportExecutionException(
                        $"The generated file is not a valid PDF.\nSTDOUT:\n{stdout}\nSTDERR:\n{stderr}");
                }

                return reportBytes;
            }
            catch (JasperReportExecutionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GeneratePhvObsoleteIdleReportAsync failed (exception rethrown). costCenterLabel={costCenterLabel}, warehouseCode={warehouseCode}, repYear={repYear}, repMonth={repMonth}, templateRelativePath={templateRelativePath}, format={format}", costCenterLabel, warehouseCode, repYear, repMonth, templateRelativePath, format);
                throw new JasperReportExecutionException("Unexpected error while generating the report.", ex);
            }
            finally
            {
                TryDeleteDirectory(workingDirectory);
            }
        }

        private static string ResolveAppRelativePath(string envVarName, string appRelativePath)
        {
            var configuredPath = Environment.GetEnvironmentVariable(envVarName);
            if (!string.IsNullOrWhiteSpace(configuredPath))
            {
                return configuredPath;
            }

            var mappedPath = HostingEnvironment.MapPath(appRelativePath);
            if (!string.IsNullOrWhiteSpace(mappedPath))
            {
                return mappedPath;
            }

            throw new JasperReportExecutionException($"Unable to resolve the path for {envVarName}.");
        }

        private static string ResolveJavaExecutable()
        {
            var javaHome = Environment.GetEnvironmentVariable("JAVA_HOME");
            if (!string.IsNullOrWhiteSpace(javaHome))
            {
                var javaExe = Path.Combine(javaHome, "bin", "java.exe");
                if (File.Exists(javaExe))
                {
                    return javaExe;
                }
            }

            return "java";
        }

        private static string QuoteArgument(string value)
        {
            if (value == null)
            {
                return "\"\"";
            }

            return "\"" + value.Replace("\"", "\\\"") + "\"";
        }

        private static bool LooksLikePdf(byte[] bytes)
        {
            return bytes != null && bytes.Length >= 5
                && bytes[0] == (byte)'%'
                && bytes[1] == (byte)'P'
                && bytes[2] == (byte)'D'
                && bytes[3] == (byte)'F'
                && bytes[4] == (byte)'-';
        }

        private static object GetValue(object source, params string[] propertyNames)
        {
            if (source == null)
            {
                return null;
            }

            var sourceType = source.GetType();
            foreach (var propertyName in propertyNames)
            {
                var property = sourceType.GetProperty(propertyName);
                if (property != null)
                {
                    return property.GetValue(source);
                }
            }

            return null;
        }

        private static string GetStringValue(object source, params string[] propertyNames)
        {
            var value = GetValue(source, propertyNames);
            return value?.ToString()?.Trim();
        }

        private static decimal GetDecimalValue(object source, params string[] propertyNames)
        {
            var value = GetValue(source, propertyNames);
            if (value == null || value == DBNull.Value)
            {
                return 0m;
            }

            if (value is decimal decimalValue)
            {
                return decimalValue;
            }

            if (value is double doubleValue)
            {
                return Convert.ToDecimal(doubleValue);
            }

            if (value is float floatValue)
            {
                return Convert.ToDecimal(floatValue);
            }

            if (decimal.TryParse(value.ToString(), out var parsedDecimal))
            {
                return parsedDecimal;
            }

            return 0m;
        }

        private static string FormatDateValue(object value)
        {
            if (value == null || value == DBNull.Value)
            {
                return null;
            }

            if (value is DateTime dateTime)
            {
                return dateTime.ToString("dd/MM/yyyy");
            }

            if (DateTime.TryParse(value.ToString(), out var parsedDate))
            {
                return parsedDate.ToString("dd/MM/yyyy");
            }

            return value.ToString();
        }

        private static async Task<bool> WaitForExitAsync(Process process, int timeoutMs)
        {
            return await Task.Run(() => process.WaitForExit(timeoutMs)).ConfigureAwait(false);
        }

        private static void TryKillProcess(Process process)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill();
                }
            }
            catch
            {
            }
        }

        private static void TryDeleteDirectory(string directoryPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
            {
                return;
            }

            try
            {
                Directory.Delete(directoryPath, true);
            }
            catch
            {
            }
        }
    }
}
