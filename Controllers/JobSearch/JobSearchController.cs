using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Web.Http;
using MISReports_Api.DAL;
using MISReports_Api.Models;
using Oracle.ManagedDataAccess.Client;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/jobsearch")]
    public class JobSearchController : ApiController
    {
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
        public async Task<IHttpActionResult> Search(
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
        public async Task<IHttpActionResult> GetApplicationStatus(
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
                return Ok(new { success = false, message = "Failed to fetch application status", error = ex.Message });
            }
        }

        // ────────────────────────────────────────────────
        // 03. Application Information (PIV, wiring details, etc.)
        // ────────────────────────────────────────────────
        [HttpGet]
        [Route("application-info")]
        public async Task<IHttpActionResult> GetApplicationInfo(
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
                return Ok(new { success = false, message = "Failed to fetch application info", error = ex.Message });
            }
        }

        // ────────────────────────────────────────────────
        // 05. Contractor Info
        // ────────────────────────────────────────────────
        [HttpGet]
        [Route("contractor-info")]
        public async Task<IHttpActionResult> GetContractorInfo(
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
                return Ok(new { success = false, message = "Failed to fetch contractor info", error = ex.Message });
            }
        }

        // ────────────────────────────────────────────────
        // 06. Labour Details
        // ────────────────────────────────────────────────
        [HttpGet]
        [Route("labour-details")]
        public async Task<IHttpActionResult> GetLabourDetails(
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
                return Ok(new { success = false, message = "Failed to fetch labour details", error = ex.Message });
            }
        }

        // ────────────────────────────────────────────────
        // 07. Energizing Basic (SPExPJOB)
        // ────────────────────────────────────────────────
        [HttpGet]
        [Route("energizing-basic")]
        public async Task<IHttpActionResult> GetEnergizingBasic(
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
                return Ok(new { success = false, message = "Failed to fetch energizing basic info", error = ex.Message });
            }
        }

        // ────────────────────────────────────────────────
        // 08.  Standard Estimate (SPESTSTDH)
        // ────────────────────────────────────────────────
        [HttpGet]
        [Route("estimate-info")]
        public async Task<IHttpActionResult> GeStandardEstimate(
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
                return Ok(new { success = false, message = "Failed to fetch revised estimate", error = ex.Message });
            }
        }

        // ────────────────────────────────────────────────
        // Detail Estimate for the estimation information (list with summation)
        // ────────────────────────────────────────────────
        [HttpGet]
        [Route("estimate-cost-summary")]
        public async Task<IHttpActionResult> GetEstimateCostSummary(
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
        public async Task<IHttpActionResult> GetMaterialTransactions([FromUri] string estimateNo)
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
        public async Task<IHttpActionResult> GetJobStatusHistory(
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
                return Ok(new { success = false, message = "Failed to fetch job status history", error = ex.Message });
            }
        }

        // ────────────────────────────────────────────────
        // 10. Energized Info (meters)
        // ────────────────────────────────────────────────
        [HttpGet]
        [Route("energized-info")]
        public async Task<IHttpActionResult> GetEnergizedInfo(
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
                return Ok(new { success = false, message = "Failed to fetch energized info", error = ex.Message });
            }
        }

        // ────────────────────────────────────────────────
        // 11. Estimate Approvals
        // ────────────────────────────────────────────────
        [HttpGet]
        [Route("estimate-approvals")]
        public async Task<IHttpActionResult> GetEstimateApprovals(
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
        public async Task<IHttpActionResult> GetRevisedEstimateApprovals(
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
        public async Task<IHttpActionResult> GetJobFinalizedInfo(
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
        public async Task<IHttpActionResult> GetContractorBills(
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
        public async Task<IHttpActionResult> GetPivDetails(
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
        public async Task<IHttpActionResult> GetAppointmentInfo([FromUri] string reference_no)
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
        public async Task<IHttpActionResult> GetEstimateSummary(
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
        public async Task<IHttpActionResult> GetMaterialsforjobsAsync([FromUri] string estimateNo)
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