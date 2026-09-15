using MISReports_Api.DAL.Analysis;
using MISReports_Api.DAL.Shared;
using MISReports_Api.Models.Analysis;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
	[RoutePrefix("api/analysis/solar-age")]
	public class AnalysisController : ApiController
	{
		private readonly AreasDao _areasDao = new AreasDao();
		private readonly SolarAgeAnalysisDao _solarAgeAnalysisDao = new SolarAgeAnalysisDao();

		[HttpGet]
		[Route("areas")]
		public IHttpActionResult GetAreas()
		{
			try
			{
				var areas = _areasDao.GetAreas();

				return Ok(JObject.FromObject(new
				{
					data = areas,
					errorMessage = (string)null
				}));
			}
			catch (Exception ex)
			{
				return Ok(JObject.FromObject(new
				{
					data = (object)null,
					errorMessage = "Cannot get solar age areas.",
					errorDetails = ex.Message
				}));
			}
		}

		[HttpGet]
		[Route("db-check")]
		public IHttpActionResult CheckDatabase([FromUri] string areaCode)
		{
			if (string.IsNullOrWhiteSpace(areaCode))
			{
				return Ok(JObject.FromObject(new
				{
					data = (object)null,
					errorMessage = "Area code parameter is required."
				}));
			}

			try
			{
				var isWorking = _solarAgeAnalysisDao.TestAreaConnection(areaCode.Trim(), out var errorMessage);

				return Ok(JObject.FromObject(new
				{
					data = new
					{
						areaCode = areaCode.Trim(),
						isWorking
					},
					errorMessage
				}));
			}
			catch (Exception ex)
			{
				return Ok(JObject.FromObject(new
				{
					data = (object)null,
					errorMessage = "Cannot test the solar age database connection.",
					errorDetails = ex.Message
				}));
			}
		}

		// Age Analysis of Solar Power Consumers
		/*
		[HttpGet]
		[Route("bill-cycles")]
		public IHttpActionResult GetBillCycles([FromUri] string areaCode, [FromUri] int take = 20)
		{
			// This endpoint was originally implemented here to return bill cycles
			// for the solar-age analysis. The project now exposes a shared
			// bill cycle endpoint at `api/billcycle/max` (see
			// Controllers/Debtors/BillCycleController.cs). To avoid duplicate
			// implementations we are commenting out this action. The original
			// implementation is preserved below for reference.
			if (string.IsNullOrWhiteSpace(areaCode))
			{
				return Ok(JObject.FromObject(new
				{
					data = (object)null,
					errorMessage = "Area code parameter is required."
				}));
			}

			try
			{
				var result = _solarAgeAnalysisDao.GetBillCycles(areaCode.Trim(), take);

				return Ok(JObject.FromObject(new
				{
					data = result,
					errorMessage = result.ErrorMessage
				}));
			}
			catch (Exception ex)
			{
				return Ok(JObject.FromObject(new
				{
					data = (object)null,
					errorMessage = "Cannot get solar age bill cycles.",
					errorDetails = ex.Message
				}));
			}
		}
		*/
		// view details - Age Analysis of Solar Power Consumers
		[HttpGet]
		[Route("view")]
		public IHttpActionResult GetAgeAnalysis(
			[FromUri] string areaCode,
			[FromUri] string billCycle,
			[FromUri] string ageBand)
		{
			var validationErrors = new List<string>();

			if (string.IsNullOrWhiteSpace(areaCode))
			{
				validationErrors.Add("Area code is required.");
			}

			if (string.IsNullOrWhiteSpace(billCycle))
			{
				validationErrors.Add("Bill cycle is required.");
			}

			if (string.IsNullOrWhiteSpace(ageBand))
			{
				validationErrors.Add("Age band is required.");
			}

			if (validationErrors.Count > 0)
			{
				return Ok(JObject.FromObject(new
				{
					data = (object)null,
					errorMessage = string.Join("; ", validationErrors)
				}));
			}

			try
			{
				var request = new SolarAgeAnalysisRequest
				{
					AreaCode = areaCode.Trim(),
					BillCycle = billCycle.Trim(),
					AgeBand = ageBand.Trim()
				};

				var result = _solarAgeAnalysisDao.GetAgeAnalysis(request);

				return Ok(JObject.FromObject(new
				{
					data = result,
					errorMessage = result.ErrorMessage
				}));
			}
			catch (Exception ex)
			{
				return Ok(JObject.FromObject(new
				{
					data = (object)null,
					errorMessage = "Cannot get solar age analysis data.",
					errorDetails = ex.Message
				}));
			}
		}

		//full report - Age Analysis of Solar Power Consumers
		[HttpGet]
		[Route("full-report")]
		public IHttpActionResult GetFullReport(
			[FromUri] string areaCode,
			[FromUri] string billCycle)
		{
			var validationErrors = new List<string>();

			if (string.IsNullOrWhiteSpace(areaCode))
			{
				validationErrors.Add("Area code is required.");
			}

			if (string.IsNullOrWhiteSpace(billCycle))
			{
				validationErrors.Add("Bill cycle is required.");
			}

			if (validationErrors.Count > 0)
			{
				return Ok(JObject.FromObject(new
				{
					data = (object)null,
					errorMessage = string.Join("; ", validationErrors)
				}));
			}

			try
			{
				var request = new SolarAgeAnalysisRequest
				{
					AreaCode = areaCode.Trim(),
					BillCycle = billCycle.Trim(),
					AgeBand = "all"
				};

				var result = _solarAgeAnalysisDao.GetFullReport(request);

				return Ok(JObject.FromObject(new
				{
					data = result,
					errorMessage = result.ErrorMessage
				}));
			}
			catch (Exception ex)
			{
				return Ok(JObject.FromObject(new
				{
					data = (object)null,
					errorMessage = "Cannot get solar age full report data.",
					errorDetails = ex.Message
				}));
			}
		}
	}
}
