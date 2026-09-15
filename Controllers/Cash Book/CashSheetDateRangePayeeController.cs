using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Web.Http;
using ChqApp.DAL;
using ChqApp.Models;

namespace ChqApp.Controllers
{
    public class CashSheetDateRangePayeeController : ApiController
    {
        [HttpGet]
        public IHttpActionResult Get(string costCtr, string fromDate, string toDate, string payee)
        {
            if (string.IsNullOrWhiteSpace(costCtr))
                return BadRequest("costCtr is required.");

            if (!TryParseRequestDate(fromDate, out DateTime parsedFromDate))
                return BadRequest("fromDate must be a valid date in yyyy/MM/dd or yyyy-MM-dd format.");

            if (!TryParseRequestDate(toDate, out DateTime parsedToDate))
                return BadRequest("toDate must be a valid date in yyyy/MM/dd or yyyy-MM-dd format.");

            if (parsedToDate < parsedFromDate)
                return BadRequest("toDate must be on or after fromDate.");

            try
            {
                string connectionString = GetConnectionString();
                var dal = new CashSheetDateRangePayeeDAL(connectionString);
                List<CashSheetDateRangePayeeModel> data = dal.GetCashSheetDateRangePayeeModel(costCtr.Trim(), parsedFromDate, parsedToDate, payee);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        private static bool TryParseRequestDate(string value, out DateTime parsedDate)
        {
            parsedDate = default(DateTime);

            if (string.IsNullOrWhiteSpace(value))
                return false;

            string[] formats = { "yyyy/MM/dd", "yyyy-MM-dd", "yyyyMMdd", "MM/dd/yyyy", "dd/MM/yyyy" };

            if (DateTime.TryParseExact(value.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate))
                return true;

            return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out parsedDate);
        }

        private static string GetConnectionString()
        {
            var connectionStringSetting = ConfigurationManager.ConnectionStrings["OracleDb"];
            if (connectionStringSetting != null && !string.IsNullOrWhiteSpace(connectionStringSetting.ConnectionString))
                return connectionStringSetting.ConnectionString;

            string[] fallbackNames = { "HQOracle", "OracleTest", "THQOracle", "wareHQOracle" };
            foreach (string name in fallbackNames)
            {
                var fallbackSetting = ConfigurationManager.ConnectionStrings[name];
                if (fallbackSetting != null && !string.IsNullOrWhiteSpace(fallbackSetting.ConnectionString))
                    return fallbackSetting.ConnectionString;
            }

            throw new InvalidOperationException("No Oracle connection string is configured. Please set 'OracleDb' in web.config.");
        }
    }
}