using MISReports_Api.Compat;
using MISReports_Api.Models.SolarJobs;
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
    public class CcApplicationJasperReportService
    {
        private const int ProcessTimeoutMs = 10 * 60 * 1000;
        private readonly string _javaExecutable;
        private readonly string _jarPath;
        private readonly string _templatePath;

        public CcApplicationJasperReportService()
        {
            _javaExecutable = ResolveJavaExecutable();
            _jarPath = ResolveAppRelativePath(
                "CC_APP_JASPER_JAR",
                "~/JasperTools/CcApplicationReportTool/target/cc-application-report.jar");
            _templatePath = ResolveAppRelativePath(
                "CC_APP_JRXML_TEMPLATE",
                "~/JasperTools/CcApplicationReportTool/src/main/resources/reports/cc_application_progress.jrxml");
        }

        public async Task<byte[]> GenerateCcApplicationPdfAsync(
            IEnumerable<CcApplicationModel> rows,
            string costCenterLabel,
            string fromDate,
            string toDate)
        {
            if (rows == null)
                throw new ArgumentNullException(nameof(rows));

            var rowList = rows.ToList();
            if (rowList.Count == 0)
                throw new Exception("No data was supplied to the Cc Application report renderer.");

            if (!File.Exists(_jarPath))
            {
                throw new Exception(
                    $"Jasper report JAR was not found at '{_jarPath}'. Build and deploy the standalone Java tool first.");
            }

            // We don't strictly check for template existence if it's supposed to be inside the JAR, 
            // but the PHV service checks it. Let's keep it for consistency but be aware it might fail if file is missing.
            // if (!File.Exists(_templatePath))
            // {
            //     throw new Exception($"Jasper template was not found at '{_templatePath}'.");
            // }

            var workingDirectory = Path.Combine(
                Path.GetTempPath(),
                "ceb-reporting",
                "cc-application",
                Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(workingDirectory);

            var inputJsonPath = Path.Combine(workingDirectory, "input.json");
            var outputPdfPath = Path.Combine(workingDirectory, "output.pdf");

            try
            {
                var json = JsonConvert.SerializeObject(rowList, Formatting.None);
                File.WriteAllText(inputJsonPath, json, new UTF8Encoding(false));

                var arguments = new StringBuilder()
                    .Append("-jar ").Append(QuoteArgument(_jarPath))
                    .Append(" --input ").Append(QuoteArgument(inputJsonPath))
                    .Append(" --output ").Append(QuoteArgument(outputPdfPath))
                    .Append(" --template ").Append(QuoteArgument(_templatePath))
                    .Append(" --costctr ").Append(QuoteArgument(costCenterLabel ?? string.Empty))
                    .Append(" --fromdate ").Append(QuoteArgument(fromDate))
                    .Append(" --todate ").Append(QuoteArgument(toDate))
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
                        if (!string.IsNullOrWhiteSpace(args.Data)) stdout.AppendLine(args.Data);
                    };

                    process.ErrorDataReceived += (_, args) =>
                    {
                        if (!string.IsNullOrWhiteSpace(args.Data)) stderr.AppendLine(args.Data);
                    };

                    if (!process.Start())
                    {
                        throw new Exception("Unable to start the Jasper report process.");
                    }

                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    if (!await Task.Run(() => process.WaitForExit(ProcessTimeoutMs)).ConfigureAwait(false))
                    {
                        try { process.Kill(); } catch { }
                        throw new Exception("The Jasper report process timed out.");
                    }

                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        throw new Exception(
                            $"Jasper report process failed with exit code {process.ExitCode}.\nSTDOUT:\n{stdout}\nSTDERR:\n{stderr}");
                    }
                }

                if (!File.Exists(outputPdfPath))
                {
                    throw new Exception(
                        $"The Jasper report process completed, but no PDF was created.\nSTDOUT:\n{stdout}\nSTDERR:\n{stderr}");
                }

                return File.ReadAllBytes(outputPdfPath);
            }
            finally
            {
                try { Directory.Delete(workingDirectory, true); } catch { }
            }
        }

        private static string ResolveAppRelativePath(string envVarName, string appRelativePath)
        {
            var configuredPath = Environment.GetEnvironmentVariable(envVarName);
            if (!string.IsNullOrWhiteSpace(configuredPath)) return configuredPath;

            var mappedPath = HostingEnvironment.MapPath(appRelativePath);
            if (!string.IsNullOrWhiteSpace(mappedPath)) return mappedPath;

            return appRelativePath.Replace("~", AppDomain.CurrentDomain.BaseDirectory);
        }

        private static string ResolveJavaExecutable()
        {
            var javaHome = Environment.GetEnvironmentVariable("JAVA_HOME");
            if (!string.IsNullOrWhiteSpace(javaHome))
            {
                var javaExe = Path.Combine(javaHome, "bin", "java.exe");
                if (File.Exists(javaExe)) return javaExe;
            }
            return "java";
        }

        private static string QuoteArgument(string value)
        {
            if (value == null) return "\"\"";
            return "\"" + value.Replace("\"", "\\\"") + "\"";
        }
    }
}
