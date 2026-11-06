using MISReports_Api.Compat;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MISReports_Api.DAL;
using MISReports_Api.Models;
using Oracle.ManagedDataAccess.Client;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/jobsearch")]
    public class JobSearchController : ApiController
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly JobSearchRepository _repository = new JobSearchRepository();

        // ────────────────────────────────────────────────
        // Multi-parameter search
        // ────────────────────────────────────────────────
        /// <summary>
        /// Search applications by one or more criteria
        /// GET api/jobsearch?application_Id=ABC123&application_No=2024-00123&project_no=PRJ/456&...
        /// </summary>
        [HttpGet]
        [Route("")]
        public async Task<IActionResult> Search(
     [FromUri] string applicationId = null,
     [FromUri] string applicationNo = null,
     [FromUri] string projectNo = null,
     [FromUri] string idNo = null,
     [FromUri] string accountNo = null,
     [FromUri] string phone = null)     // shorter and clearer than "tele"
        {
            if (string.IsNullOrWhiteSpace(applicationId) &&
                string.IsNullOrWhiteSpace(applicationNo) &&
                string.IsNullOrWhiteSpace(projectNo) &&
                string.IsNullOrWhiteSpace(idNo) &&
                string.IsNullOrWhiteSpace(accountNo) &&
                string.IsNullOrWhiteSpace(phone))
            {
                return BadRequest("At least one search parameter is required.");
            }

            var results = await _repository.GetJobSearchResultsAsync(
                applicationId,
                applicationNo,
                projectNo,
                idNo,
                accountNo,
                phone);   // ← matches the repository method parameter name

            return Ok(new { success = true, count = results.Count, data = results });
        }

        // ────────────────────────────────────────────────
        // 02. Application Status Details
        // ────────────────────────────────────────────────
        [HttpGet]
        [Route("application-status")]
        public async Task<IActionResult> GetApplicationStatus(
            [FromUri] string application_id)
        {
            if (string.IsNullOrWhiteSpace(application_id))
                return BadRequest("application_id is required");

            try
            {
                var result = await _repository.GetApplicationStatusAsync(application_id);
                if (result == null) return NotFound();

                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetApplicationStatus failed. application_id={application_id}", application_id);
                return Ok(new { success = false, message = "Failed to fetch application status", error = ex.Message });
            }
        }

        // ────────────────────────────────────────────────
        // 03. Application Information (PIV, wiring details, etc.)
        // ────────────────────────────────────────────────
        [HttpGet]
        [Route("application-info")]
        public async Task<IActionResult> GetApplicationInfo(
            [FromUri] string application_id)
        {
            if (string.IsNullOrWhiteSpace(application_id))
                return BadRequest("application_id is required");

            try
            {
                var results = await _repository.GetApplicationInfoAsync(application_id);
                return Ok(new { success = true, count = results.Count, data = results });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetApplicationInfo failed. application_id={application_id}", application_id);
                return Ok(new { success = false, message = "Failed to fetch application info", error = ex.Message });
            }
        }

        // ────────────────────────────────────────────────
        // 05. Contractor Info
        // ────────────────────────────────────────────────
        [HttpGet]
        [Route("contractor-info")]
        public async Task<IActionResult> GetContractorInfo(
            [FromUri] string project_no)
        {
            if (string.IsNullOrWhiteSpace(project_no))
                return BadRequest("project_no is required");

            try
            {
                var results = await _repository.GetContractorInfoAsync(project_no);
                return Ok(new { success = true, data = results });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetContractorInfo failed. project_no={project_no}", project_no);
                return Ok(new { success = false, message = "Failed to fetch contractor info", error = ex.Message });
            }
        }

        // ────────────────────────────────────────────────
        // 06. Labour Details
        // ────────────────────────────────────────────────
        [HttpGet]
        [Route("labour-details")]
        public async Task<IActionResult> GetLabourDetails(
            [FromUri] string estimate_no)
        {
            if (string.IsNullOrWhiteSpace(estimate_no))
                return BadRequest("estimate_no is required");

            try
            {
                var results = await _repository.GetLabourDetailsAsync(estimate_no);
                return Ok(new { success = true, data = results });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetLabourDetails failed. estimate_no={estimate_no}", estimate_no);
                return Ok(new { success = false, message = "Failed to fetch labour details", error = ex.Message });
            }
        }

        // ────────────────────────────────────────────────
        // 07. Energizing Basic (SPExPJOB)
        // ────────────────────────────────────────────────
        [HttpGet]
        [Route("energizing-basic")]
        public async Task<IActionResult> GetEnergizingBasic(
            [FromUri] string project_no)
        {
            if (string.IsNullOrWhiteSpace(project_no))
                return BadRequest("project_no is required");

            try
            {
                var result = await _repository.GetEnergizingBasicAsync(project_no);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetEnergizingBasic failed. project_no={project_no}", project_no);
                return Ok(new { success = false, message = "Failed to fetch energizing basic info", error = ex.Message });
            }
        }

        // ────────────────────────────────────────────────
        // 08.  Standard Estimate (SPESTSTDH)
        // ────────────────────────────────────────────────
        [HttpGet]
        [Route("estimate-info")]
        public async Task<IActionResult> GeStandardEstimate(
            [FromUri] string estimate_no)
        {
            if (string.IsNullOrWhiteSpace(estimate_no))
                return BadRequest("estimate_no is required");

            try
            {
                var result = await _repository.GetStandardEstimateAsync(estimate_no);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GeStandardEstimate failed. estimate_no={estimate_no}", estimate_no);
                return Ok(new { success = false, message = "Failed to fetch revised estimate", error = ex.Message });
            }
        }

        // ────────────────────────────────────────────────
        // Detail Estimate for the estimation information (list with summation)
        // ────────────────────────────────────────────────
        [HttpGet]
        [Route("estimate-cost-summary")]
        public async Task<IActionResult> GetEstimateCostSummary(
            [FromUri] string estimateNo)
        {
            if (string.IsNullOrWhiteSpace(estimateNo))
                return BadRequest("estimateNo is required");

            try
            {
                var result = await _repository.GetResourceCostSummaryAsync(estimateNo);

                return Ok(new
                {
                    success = true,
                    data = result
                });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetEstimateCostSummary failed. estimateNo={estimateNo}", estimateNo);
                return Ok(new
                {
                    success = false,
                    message = "Failed to fetch estimate cost summary",
                    error = ex.Message
                });
            }
        }

        //----------------------------------------------
        // material transactions
        //_______________________________________________
        [HttpGet]
        [Route("material-transactions")]
        public async Task<IActionResult> GetMaterialTransactions([FromUri] string estimateNo)
        {
            if (string.IsNullOrWhiteSpace(estimateNo))
                return BadRequest("estimateNo is required");

            try
            {
                var result = await _repository.GetMaterialTransactionsAsync(estimateNo);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetMaterialTransactions failed. estimateNo={estimateNo}", estimateNo);
                return Ok(new
                {
                    success = false,
                    message = "Failed to fetch material transactions",
                    error = ex.Message
                });
            }
        }

        // ────────────────────────────────────────────────
        // 09. Job Status History
        // ────────────────────────────────────────────────
        [HttpGet]
        [Route("job-status-history")]
        public async Task<IActionResult> GetJobStatusHistory(
            [FromUri] string project_no)
        {
            if (string.IsNullOrWhiteSpace(project_no))
                return BadRequest("project_no is required");

            try
            {
                var results = await _repository.GetJobStatusHistoryAsync(project_no);
                return Ok(new { success = true, data = results });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetJobStatusHistory failed. project_no={project_no}", project_no);
                return Ok(new { success = false, message = "Failed to fetch job status history", error = ex.Message });
            }
        }

        // ────────────────────────────────────────────────
        // 10. Energized Info (meters)
        // ────────────────────────────────────────────────
        [HttpGet]
        [Route("energized-info")]
        public async Task<IActionResult> GetEnergizedInfo(
            [FromUri] string project_no)
        {
            if (string.IsNullOrWhiteSpace(project_no))
                return BadRequest("project_no is required");

            try
            {
                var results = await _repository.GetEnergizedInfoAsync(project_no);
                return Ok(new { success = true, data = results });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetEnergizedInfo failed. project_no={project_no}", project_no);
                return Ok(new { success = false, message = "Failed to fetch energized info", error = ex.Message });
            }
        }

        // ────────────────────────────────────────────────
        // 11. Estimate Approvals
        // ────────────────────────────────────────────────
        [HttpGet]
        [Route("estimate-approvals")]
        public async Task<IActionResult> GetEstimateApprovals(
            [FromUri] string estimate_no)
        {
            if (string.IsNullOrWhiteSpace(estimate_no))
                return BadRequest("estimate_no is required");

            try
            {
                var results = await _repository.GetEstimateApprovalsAsync(estimate_no);
                return Ok(new { success = true, data = results });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetEstimateApprovals failed. estimate_no={estimate_no}", estimate_no);
                return Ok(new
                {
                    success = false,
                    message = "Failed to fetch estimate approvals",
                    error = ex.Message
                });
            }
        }

        // ────────────────────────────────────────────────
        // 11. Revised Estimate Approvals
        // ────────────────────────────────────────────────
        [HttpGet]
        [Route("revised-estimate-approvals")]
        public async Task<IActionResult> GetRevisedEstimateApprovals(
            [FromUri] string projectNO)
        {
            if (string.IsNullOrWhiteSpace(projectNO))
                return BadRequest("projectNO is required");

            try
            {
                var results = await _repository.GetRevisedEstimateApprovalsAsync(projectNO);
                return Ok(new { success = true, data = results });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetRevisedEstimateApprovals failed. projectNO={projectNO}", projectNO);
                return Ok(new
                {
                    success = false,
                    message = "Failed to fetch revised estimate approvals",
                    error = ex.Message
                });
            }
        }

        // ────────────────────────────────────────────────
        // 12. Job Finalized API
        // ────────────────────────────────────────────────
        [HttpGet]
        [Route("job-finalized-info")]
        public async Task<IActionResult> GetJobFinalizedInfo(
            [FromUri] string project_no)
        {
            if (string.IsNullOrWhiteSpace(project_no))
                return BadRequest("project_no is required");

            try
            {
                var result = await _repository.GetJobFinalizedAsync(project_no);

                return Ok(new
                {
                    success = true,
                    data = result
                });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetJobFinalizedInfo failed. project_no={project_no}", project_no);
                return Ok(new
                {
                    success = false,
                    message = "Failed to fetch job finalized info",
                    error = ex.Message
                });
            }
        }

        // ────────────────────────────────────────────────
        // 13. Contractor Bill API
        // ────────────────────────────────────────────────
        [HttpGet]
        [Route("contractor-bills")]
        public async Task<IActionResult> GetContractorBills(
            [FromUri] string project_no)
        {
            if (string.IsNullOrWhiteSpace(project_no))
                return BadRequest("project_no is required");

            try
            {
                var result = await _repository.GetContractorBillsAsync(project_no);

                return Ok(new
                {
                    success = true,
                    data = result
                });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetContractorBills failed. project_no={project_no}", project_no);
                return Ok(new
                {
                    success = false,
                    message = "Failed to fetch contractor bills",
                    error = ex.Message
                });
            }
        }

        // ────────────────────────────────────────────────
        // 14. PIV Detail API
        // ────────────────────────────────────────────────
        [HttpGet]
        [Route("piv-details")]
        public async Task<IActionResult> GetPivDetails(
            [FromUri] string referenceNo)
        {
            if (string.IsNullOrWhiteSpace(referenceNo))
                return BadRequest("referenceNo is required");

            try
            {
                var result = await _repository.GetPivDetailsAsync(referenceNo);

                return Ok(new
                {
                    success = true,
                    data = result
                });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetPivDetails failed. referenceNo={referenceNo}", referenceNo);
                return Ok(new
                {
                    success = false,
                    message = "Failed to fetch PIV details",
                    error = ex.Message
                });
            }
        }

        [HttpGet]
        [Route("appointment-info")]
        public async Task<IActionResult> GetAppointmentInfo([FromUri] string reference_no)
        {
            if (string.IsNullOrWhiteSpace(reference_no))
                return BadRequest("reference_no is required");

            try
            {
                var result = await _repository.GetAppointmentInfoAsync(reference_no);

                return Ok(new
                {
                    success = true,
                    data = result
                });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetAppointmentInfo failed. reference_no={reference_no}", reference_no);
                return Ok(new
                {
                    success = false,
                    message = "Failed to fetch appointment info",
                    error = ex.Message
                });
            }
        }

        // ────────────────────────────────────────────────
        // Detail Estimate for the estimation information (list with summation)
        // ────────────────────────────────────────────────
        [HttpGet]
        [Route("estimate-cost-job")]
        public async Task<IActionResult> GetEstimateSummary(
            [FromUri] string estimateNo)
        {
            if (string.IsNullOrWhiteSpace(estimateNo))
                return BadRequest("estimateNo is required");

            try
            {
                var result = await _repository.GetResourceCostAsync(estimateNo);

                return Ok(new
                {
                    success = true,
                    data = result
                });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetEstimateSummary failed. estimateNo={estimateNo}", estimateNo);
                return Ok(new
                {
                    success = false,
                    message = "Failed to fetch estimate cost summary",
                    error = ex.Message
                });
            }
        }

        //----------------------------------------------
        // material transactions
        //_______________________________________________
        [HttpGet]
        [Route("material-transactions-for-job")]
        public async Task<IActionResult> GetMaterialsforjobsAsync([FromUri] string estimateNo)
        {
            if (string.IsNullOrWhiteSpace(estimateNo))
                return BadRequest("estimateNo is required");

            try
            {
                var result = await _repository.GetMaterialsforjobsAsync(estimateNo);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetMaterialsforjobsAsync failed. estimateNo={estimateNo}", estimateNo);
                return Ok(new
                {
                    success = false,
                    message = "Failed to fetch material transactions",
                    error = ex.Message
                });
            }
        }
    }
}