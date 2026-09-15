using MISReports_Api.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MISReports_Api.DAL
{
    public class JobSearchRepository
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public async Task<List<JobSearchModel>> GetJobSearchResultsAsync(
          string applicationId = null,
          string applicationNo = null,
          string projectNo = null,
          string idNo = null,
          string accountNo = null,
          string telephone = null)   // ← NEW parameter
        {
            var results = new List<JobSearchModel>();

            try
            {
                using (var conn = new OracleConnection(connectionString))
                {
                    await conn.OpenAsync();

                    const string sql = @"
WITH REF_MATCH AS (
    -- Branch 1: Standard parameters
    SELECT ar.application_id, ar.application_no, ap.id_no, ar.projectno
    FROM APPLICATION_REFERENCE ar
    JOIN APPLICATIONS ap ON ap.application_id = ar.application_id
    WHERE 1=1
      AND (:application_Id IS NULL OR ar.application_id = :application_Id)
      AND (:application_No IS NULL OR ar.application_no = :application_No)
      AND (:project_no IS NULL OR ar.projectno = :project_no)
      AND (:Id_No IS NULL OR UPPER(TRIM(ap.id_no)) = UPPER(TRIM(:Id_No)))
      AND :Account_NO IS NULL
      AND :tele IS NULL

    UNION ALL

    -- Branch 2: Account number – wiring land
    SELECT DISTINCT ar.application_id, ar.application_no, ap.id_no, ar.projectno
    FROM WIRING_LAND_DETAIL wld
    JOIN APPLICATION_REFERENCE ar ON ar.application_id = wld.application_id
    JOIN APPLICATIONS ap ON ap.application_id = ar.application_id
    WHERE :Account_NO IS NOT NULL
      AND TRIM(wld.EXISTING_ACC_NO) = TRIM(:Account_NO)

    UNION ALL

    -- Branch 3: Account number – special job
    SELECT DISTINCT ar.application_id, ar.application_no, ap.id_no, ar.projectno
    FROM SPEXPJOB sj
    JOIN APPLICATION_REFERENCE ar ON ar.projectno = sj.project_no
    JOIN APPLICATIONS ap ON ap.application_id = ar.application_id
    WHERE :Account_NO IS NOT NULL
      AND TRIM(sj.ACCOUNT_NO) = TRIM(:Account_NO)

    UNION ALL

    -- Branch 4: Telephone or Mobile number
    SELECT DISTINCT ar.application_id, ar.application_no, app.id_no, ar.projectno
    FROM APPLICATION_REFERENCE ar
    JOIN APPLICANT app ON app.id_no = ar.id_no
    WHERE :tele IS NOT NULL
      AND (
          TRIM(app.telephone_no) = TRIM(:tele)
          OR TRIM(app.mobile_no) = TRIM(:tele)
      )
      AND :application_Id IS NULL
      AND :application_No IS NULL
      AND :project_no     IS NULL
      AND :Id_No          IS NULL
      AND :Account_NO     IS NULL
),
BASE AS (
    SELECT
        rm.application_no,
        rm.projectno,
        rm.id_no,
        ap.application_id,
        ap.application_type,
        ap.submit_date,
        ap.status AS app_base_status,
        est.estimate_no AS estimation_no,
        job.project_no AS job_no
    FROM REF_MATCH rm
    JOIN APPLICATIONS ap ON ap.application_id = rm.application_id
    LEFT JOIN pcesthtt est
           ON TRIM(est.estimate_no) = TRIM(rm.application_no)
    LEFT JOIN pcesthmt job
           ON TRIM(job.project_no) = TRIM(rm.projectno)
),
STATUS_LOGIC AS (
    SELECT
        b.*,
        COALESCE(
            (SELECT CASE
                WHEN t2.status IN (3) THEN 'JOB Hard Closed'
                WHEN t2.status IN (4) THEN 'JOB Soft Closed'
                WHEN t2.status = 1 AND L.account_no IS NOT NULL
                     THEN 'Account Created. Acc No: ' || L.account_no
                WHEN t2.status = 1 AND L.exported_date IS NOT NULL
                     THEN 'Ready to open Account'
                WHEN t2.status = 1 AND f.connected_date IS NOT NULL
                     THEN 'Connection Given ' || TO_CHAR(f.connected_date, 'YYYY/MM/DD')
                WHEN t2.status = 1 THEN 'JOB Ongoing'
                WHEN t2.status IN (5,41) THEN 'JOB on revising'
                WHEN t2.status = 55 THEN 'Revised → ES approval'
                WHEN t2.status = 56 THEN 'Revised → EA approval'
                WHEN t2.status = 57 THEN 'Revised → EE approval'
                WHEN t2.status = 58 THEN 'Revised → DGM approval'
                WHEN t2.status = 61 THEN 'Revised → CE approval'
                WHEN t2.status = 60 THEN 'PIV3 Supplementary Fee to be paid'
             END
             FROM pcesthmt t2
             LEFT JOIN spodrcrd f ON TRIM(f.project_no) = TRIM(t2.project_no)
             LEFT JOIN spexpjob L ON TRIM(L.project_no) = TRIM(t2.project_no)
             WHERE TRIM(t2.project_no) = TRIM(b.projectno)
               AND ROWNUM = 1
            ),
            (SELECT CASE
                WHEN t2.status = 22 THEN 'Ready to start Job'
                WHEN t2.status = 1 THEN 'JOB Ongoing'
                WHEN t2.status = 41 THEN 'Job Rejected'
                WHEN t2.status = 60 THEN 'PIV3 Supplementary Fee pending'
             END
             FROM pcesthmt t2
             WHERE TRIM(t2.project_no) = TRIM(b.projectno)
               AND ROWNUM = 1
            ),
            (SELECT CASE
                WHEN t2.status = 75 THEN 'Estimation on progress'
                WHEN t2.status BETWEEN 44 AND 49 THEN 'Estimation approval pending'
                WHEN t2.status = 31 THEN 'Estimation Rejected'
                WHEN t2.status = 30 THEN 'Estimation Approved – PIV to issue'
                WHEN t2.status = 33 THEN 'PIV2 Estimation Fee Paid'
                WHEN t2.status = 22 THEN 'Job started'
             END
             FROM pcesthtt t2
             WHERE TRIM(t2.estimate_no) = TRIM(b.application_no)
               AND ROWNUM = 1
            ),
            CASE
                WHEN b.app_base_status IN ('C','A') THEN 'Site visit pending (ESV)'
                WHEN b.app_base_status = 'S' THEN 'Site visited – estimation pending'
                WHEN b.app_base_status = 'E' THEN 'Estimation in progress'
                WHEN b.app_base_status = 'N' THEN 'Application submitted – PIV1 pending'
                WHEN b.app_base_status = 'P' THEN 'Application fee paid'
                ELSE 'Unknown / Other stage'
            END
        ) AS derived_status,
        COALESCE(
            (SELECT CASE
                WHEN L.account_no IS NOT NULL THEN 'Account Created. Account No - ' || L.account_no
                WHEN L.exported_date IS NOT NULL THEN 'Ready to open an Account'
                WHEN f.connected_date IS NOT NULL THEN 'Connection Given on ' || TO_CHAR(f.connected_date, 'YYYY/MM/DD')
                WHEN t2.status = 3 THEN 'Account Created. Account No - ' || L.account_no
                WHEN t2.status = 1 THEN 'Job Ongoing'
                WHEN t2.status = 5 THEN 'Job on revising'
                WHEN t2.status = 55 THEN 'Revised Job to be approved by Electrical Superintend(ES)'
                WHEN t2.status = 56 THEN 'Revised Job to be approved by Engineer Assistant(EA)'
                WHEN t2.status = 57 THEN 'Revised Job to be approved by Electrical Engineer(EE)'
                WHEN t2.status = 58 THEN 'Revised Job to be approved by Deputy General Manager(DGM)'
                WHEN t2.status = 61 THEN 'Revised Job to be approved by Chief Engineer(CE)'
                WHEN t2.status = 41 THEN 'Job Ongoing'
                WHEN t2.status = 60 THEN 'Supplementary Estimation Fee to be paid by the Customer'
                WHEN t2.status = 22 THEN 'Ready to start the Job'
             END
             FROM pcesthmt t2
             LEFT JOIN spodrcrd f ON TRIM(f.project_no) = TRIM(t2.project_no)
             LEFT JOIN spexpjob L ON TRIM(L.project_no) = TRIM(t2.project_no)
             WHERE TRIM(t2.project_no) = TRIM(b.projectno)
               AND ROWNUM = 1
            ),
            (SELECT CASE
                WHEN t2.status = 22 THEN 'Ready to start the Job'
                WHEN t2.status = 1 THEN 'Job Ongoing'
                WHEN t2.status = 41 THEN 'Job Ongoing'
                WHEN t2.status = 60 THEN 'Supplementary Estimation Fee to be paid by the Customer'
             END
             FROM pcesthmt t2
             WHERE TRIM(t2.project_no) = TRIM(b.projectno)
               AND ROWNUM = 1
            ),
            (SELECT CASE
                WHEN t2.status = 75 THEN 'Estimation on progress'
                WHEN t2.status BETWEEN 44 AND 49 THEN 'Estimation approval pending'
                WHEN t2.status = 31 THEN 'Estimation Rejected'
                WHEN t2.status = 30 THEN 'Estimation Approved – PIV to issue'
                WHEN t2.status = 33 THEN 'PIV2 Estimation Fee Paid'
                WHEN t2.status = 22 THEN 'Job started'
             END
             FROM pcesthtt t2
             WHERE TRIM(t2.estimate_no) = TRIM(b.application_no)
               AND ROWNUM = 1
            ),
            CASE
                WHEN b.app_base_status IN ('C','A') THEN 'Site visit pending (ESV)'
                WHEN b.app_base_status = 'S' THEN 'Site visited – estimation pending'
                WHEN b.app_base_status = 'E' THEN 'Estimation in progress'
                WHEN b.app_base_status = 'N' THEN 'Application submitted – PIV1 pending'
                WHEN b.app_base_status = 'P' THEN 'Application fee paid'
                ELSE 'Unknown / Other stage'
            END
        ) AS customer_status
    FROM BASE b
)
SELECT
    sl.application_id,
    sl.application_no,
    sl.projectno,
    sl.id_no,
    a.first_name,
    a.last_name,
    a.street_address,
    a.suburb,
    a.city,
    t.description AS application_type_desc,
    sl.submit_date,
    sl.derived_status,
    sl.customer_status,
    ap.status AS application_status,
    a.telephone_no AS TEL,
    a.mobile_no   AS MOBILE
FROM STATUS_LOGIC sl
JOIN APPLICATIONS ap ON ap.application_id = sl.application_id
LEFT JOIN APPLICANT a   ON a.id_no = sl.id_no
JOIN APPLICATIONTYPES t ON t.apptype = ap.application_type
ORDER BY sl.submit_date DESC";

                    using (var cmd = new OracleCommand(sql, conn))
                    {
                        cmd.BindByName = true;

                        // Existing parameters
                        cmd.Parameters.Add("application_Id", OracleDbType.Varchar2).Value = applicationId ?? (object)DBNull.Value;
                        cmd.Parameters.Add("application_No", OracleDbType.Varchar2).Value = applicationNo ?? (object)DBNull.Value;
                        cmd.Parameters.Add("project_no", OracleDbType.Varchar2).Value = projectNo ?? (object)DBNull.Value;
                        cmd.Parameters.Add("Id_No", OracleDbType.Varchar2).Value = idNo ?? (object)DBNull.Value;
                        cmd.Parameters.Add("Account_NO", OracleDbType.Varchar2).Value = accountNo ?? (object)DBNull.Value;

                        // New parameter
                        cmd.Parameters.Add("tele", OracleDbType.Varchar2).Value = telephone ?? (object)DBNull.Value;

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                results.Add(new JobSearchModel
                                {
                                    ApplicationId = SafeGetString(reader, "application_id"),
                                    ApplicationNo = SafeGetString(reader, "application_no"),
                                    ProjectNo = SafeGetString(reader, "projectno"),
                                    IdNo = SafeGetString(reader, "id_no"),
                                    FirstName = SafeGetString(reader, "first_name"),
                                    LastName = SafeGetString(reader, "last_name"),
                                    StreetAddress = SafeGetString(reader, "street_address"),
                                    Suburb = SafeGetString(reader, "suburb"),
                                    City = SafeGetString(reader, "city"),
                                    ApplicationTypeDesc = SafeGetString(reader, "application_type_desc"),
                                    SubmitDate = SafeGetDateTime(reader, "submit_date"),
                                    Status = SafeGetString(reader, "derived_status"),
                                    CustomerStatus = SafeGetString(reader, "customer_status"),
                                    ApplicationStatus = SafeGetString(reader, "application_status"),

                                    // ── New fields ───────────────────────────────────────────────
                                    Telephone = SafeGetString(reader, "TEL"),
                                    Mobile = SafeGetString(reader, "MOBILE"),
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetJobSearchResultsAsync error: {ex.Message}");
                throw;
            }

            return results;
        }

        // ────────────────────────────────────────────────
        // 02. Application Status Details
        // ────────────────────────────────────────────────
        public async Task<ApplicationStatusDto> GetApplicationStatusAsync(string applicationId)
        {
            if (string.IsNullOrWhiteSpace(applicationId)) return null;

            try
            {
                using (var conn = new OracleConnection(connectionString))
                {
                    await conn.OpenAsync();

                    const string sql = @"
                        SELECT
                            ap.application_id               AS application_no,
                            (a.first_name || ' ' || a.last_name) AS full_name,
                            a.id_no                         AS id_no,
                            (a.street_address || ' ' || a.suburb || ' ' || a.city) AS address,
                            (w.service_street_address || ' ' || w.service_suburb || ' ' || w.service_city) AS service_address,
                            a.telephone_no                  AS telephone_no,
                            a.mobile_no                     AS mobile_no,
                            a.email                         AS email
                        FROM applications ap
                        JOIN applicant a ON a.id_no = ap.id_no
                        JOIN wiring_land_detail w ON w.application_id = ap.application_id
                        WHERE trim(ap.application_id) = :application_id";

                    using (var cmd = new OracleCommand(sql, conn))
                    {
                        cmd.BindByName = true;
                        cmd.Parameters.Add("application_id", OracleDbType.Varchar2).Value = applicationId;

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new ApplicationStatusDto
                                {
                                    ApplicationNo = SafeGetString(reader, "application_no"),
                                    FullName = SafeGetString(reader, "full_name"),
                                    IdNo = SafeGetString(reader, "id_no"),
                                    Address = SafeGetString(reader, "address"),
                                    ServiceAddress = SafeGetString(reader, "service_address"),
                                    TelephoneNo = SafeGetString(reader, "telephone_no"),
                                    MobileNo = SafeGetString(reader, "mobile_no"),
                                    Email = SafeGetString(reader, "email")
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetApplicationStatusAsync error: {ex.Message}");
                throw;
            }

            return null;
        }

        // ────────────────────────────────────────────────
        // 03. Application Information (PIV + wiring)
        // ────────────────────────────────────────────────
        public async Task<List<ApplicationInfoDto>> GetApplicationInfoAsync(string applicationId)
        {
            var results = new List<ApplicationInfoDto>();

            try
            {
                using (var conn = new OracleConnection(connectionString))
                {
                    await conn.OpenAsync();

                    const string sql = @"
                        SELECT
                            p.piv_no,
                            p.title_cd,
                            p.piv_amount,
                            p.piv_date,
                            p.paid_date,
                            t.description            AS application_type,
                            w.phase,
                            w.connection_type,
                            w.tariff_cat_code,
                            w.tariff_code,
                            ap.submit_date           AS application_date,
                            w.neighbours_acc_no      AS neighbors_acc_no,
                            w.existing_acc_no,
                            w.assessment_no
                        FROM wiring_land_detail w
                        JOIN applications ap ON ap.application_id = w.application_id
                                            AND ap.dept_id = w.dept_id
                        JOIN applicationtypes t ON t.apptype = ap.application_type
                        LEFT JOIN piv_detail p ON p.reference_no = ap.application_no
                                              AND p.dept_id = w.dept_id
                        WHERE trim(ap.application_id) = :application_id";

                    using (var cmd = new OracleCommand(sql, conn))
                    {
                        cmd.BindByName = true;
                        cmd.Parameters.Add("application_id", OracleDbType.Varchar2).Value = applicationId;

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                results.Add(new ApplicationInfoDto
                                {
                                    PivNo = SafeGetString(reader, "piv_no"),
                                    TitleCd = SafeGetString(reader, "title_cd"),
                                    PivAmount = SafeGetDecimal(reader, "piv_amount"),
                                    PivDate = SafeGetDateTime(reader, "piv_date"),
                                    PaidDate = SafeGetDateTime(reader, "paid_date"),
                                    ApplicationType = SafeGetString(reader, "application_type"),
                                    Phase = SafeGetString(reader, "phase"),
                                    ConnectionType = SafeGetString(reader, "connection_type"),
                                    TariffCatCode = SafeGetString(reader, "tariff_cat_code"),
                                    TariffCode = SafeGetString(reader, "tariff_code"),
                                    ApplicationDate = SafeGetDateTime(reader, "application_date"),
                                    NeighborsAccNo = SafeGetString(reader, "neighbors_acc_no"),
                                    ExistingAccNo = SafeGetString(reader, "existing_acc_no"),
                                    AssessmentNo = SafeGetString(reader, "assessment_no")
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetApplicationInfoAsync error: {ex.Message}");
                throw;
            }

            return results;
        }

        // ────────────────────────────────────────────────
        // 05. Contractor Info
        // ────────────────────────────────────────────────
        public async Task<List<ContractorInfoDto>> GetContractorInfoAsync(string projectNo)
        {
            var results = new List<ContractorInfoDto>();

            try
            {
                using (var conn = new OracleConnection(connectionString))
                {
                    await conn.OpenAsync();

                    const string sql = @"
                        SELECT
                            T.contractor_name,
                            D.allocated_date
                        FROM spestcnd D
                        JOIN SPESTCNT T ON D.contractor_id = T.contractor_id
                                       AND D.dept_id = T.dept_id
                        WHERE trim(D.project_no) = :project_no";

                    using (var cmd = new OracleCommand(sql, conn))
                    {
                        cmd.BindByName = true;
                        cmd.Parameters.Add("project_no", OracleDbType.Varchar2).Value = projectNo;

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                results.Add(new ContractorInfoDto
                                {
                                    ContractorName = SafeGetString(reader, "contractor_name"),
                                    AllocatedDate = SafeGetDateTime(reader, "allocated_date")
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetContractorInfoAsync error: {ex.Message}");
                throw;
            }

            return results;
        }

        // ────────────────────────────────────────────────
        // 06. Labour Details
        // ────────────────────────────────────────────────
        public async Task<List<LabourDetailDto>> GetLabourDetailsAsync(string estimateNo)
        {
            var results = new List<LabourDetailDto>();

            try
            {
                using (var conn = new OracleConnection(connectionString))
                {
                    await conn.OpenAsync();

                    const string sql = @"
                        SELECT
                            labour_code,
                            activity_description,
                            unit_labour_hrs,
                            unit_price,
                            item_qty,
                            ceb_unit_price,
                            ceb_labour_cost
                        FROM SPESTLAB
                        WHERE trim(estimate_no) = :estimate_no";

                    using (var cmd = new OracleCommand(sql, conn))
                    {
                        cmd.BindByName = true;
                        cmd.Parameters.Add("estimate_no", OracleDbType.Varchar2).Value = estimateNo;

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                results.Add(new LabourDetailDto
                                {
                                    LabourCode = SafeGetString(reader, "labour_code"),
                                    ActivityDescription = SafeGetString(reader, "activity_description"),
                                    UnitLabourHrs = SafeGetDecimal(reader, "unit_labour_hrs"),
                                    UnitPrice = SafeGetDecimal(reader, "unit_price"),
                                    ItemQty = SafeGetDecimal(reader, "item_qty"),
                                    CebUnitPrice = SafeGetDecimal(reader, "ceb_unit_price"),
                                    CebLabourCost = SafeGetDecimal(reader, "ceb_labour_cost")
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetLabourDetailsAsync error: {ex.Message}");
                throw;
            }

            return results;
        }

        // ────────────────────────────────────────────────
        // 07. Energizing Basic (SPExPJOB)
        // ────────────────────────────────────────────────
        public async Task<EnergizingBasicDto> GetEnergizingBasicAsync(string projectNo)
        {
            try
            {
                using (var conn = new OracleConnection(connectionString))
                {
                    await conn.OpenAsync();

                    const string sql = @"
                        SELECT
                            exported_date,
                            account_no,
                            acc_created_date
                        FROM SPExPJOB

                        WHERE trim(project_no) = :project_no";

                    using (var cmd = new OracleCommand(sql, conn))
                    {
                        cmd.BindByName = true;
                        cmd.Parameters.Add("project_no", OracleDbType.Varchar2).Value = projectNo;

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new EnergizingBasicDto
                                {
                                    ExportedDate = SafeGetDateTime(reader, "exported_date"),
                                    AccountNo = SafeGetString(reader, "account_no"),
                                    AccCreatedDate = SafeGetDateTime(reader, "acc_created_date")
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetEnergizingBasicAsync error: {ex.Message}");
                throw;
            }

            return null;
        }

        // ────────────────────────────────────────────────
        // 08.  Standard Estimate
        // ────────────────────────────────────────────────
        public async Task<StandardEstimateDto> GetStandardEstimateAsync(string estimateNo)
        {
            try
            {
                using (var conn = new OracleConnection(connectionString))
                {
                    await conn.OpenAsync();

                    const string sql = @"
                         SELECT
                            fixed_cost,
                            variable_cost,
                            security_deposit,
                            temporary_deposit,
                            conversion_cost,
                            labour_cost,
                            transport_cost,
                            overhead_cost,
                            damage_cost,
                            contingency_cost,
                            board_charge,
                            sscl,
                            security_deposit,
                            total_cost
                        FROM SPESTSTDH
                        WHERE trim(estimate_no) ='511.20/ENC/26/0362'";

                    using (var cmd = new OracleCommand(sql, conn))
                    {
                        cmd.BindByName = true;
                        cmd.Parameters.Add("estimate_no", OracleDbType.Varchar2).Value = estimateNo;

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new StandardEstimateDto
                                {
                                    FixedCost = SafeGetDecimal(reader, "fixed_cost"),
                                    VariableCost = SafeGetDecimal(reader, "variable_cost"),
                                    SecurityDeposit = SafeGetDecimal(reader, "security_deposit"),
                                    TemporaryDeposit = SafeGetDecimal(reader, "temporary_deposit"),
                                    ConversionCost = SafeGetDecimal(reader, "conversion_cost"),
                                    LabourCost = SafeGetDecimal(reader, "labour_cost"),
                                    TransportCost = SafeGetDecimal(reader, "transport_cost"),
                                    OverheadCost = SafeGetDecimal(reader, "overhead_cost"),
                                    DamageCost = SafeGetDecimal(reader, "damage_cost"),
                                    ContingencyCost = SafeGetDecimal(reader, "contingency_cost"),
                                    BoardCharge = SafeGetDecimal(reader, "board_charge"),
                                    Sscl = SafeGetDecimal(reader, "sscl"),
                                    TotalCost = SafeGetDecimal(reader, "total_cost")
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetStandardEstimateAsync error: {ex.Message}");
                throw;
            }

            return null;
        }

        // ────────────────────────────────────────────────
        // 09. Job Status History
        // ────────────────────────────────────────────────
        public async Task<List<JobStatusHistoryDto>> GetJobStatusHistoryAsync(string projectNo)
        {
            var results = new List<JobStatusHistoryDto>();

            try
            {
                using (var conn = new OracleConnection(connectionString))
                {
                    await conn.OpenAsync();

                    const string sql = @"
                        SELECT
                            T2.status,
                            T2.apr_dt1               AS physical_close,
                            T2.conf_dt               AS hard_close,
                            T2.prj_ass_dt            AS job_created_date,
                            CASE
                                WHEN T1.res_type IS NULL OR T1.res_type LIKE '%MAT%' THEN 'MAT'
                                WHEN T1.res_type LIKE 'LABOUR%' THEN 'LAB'
                                ELSE 'OTHER'
                            END AS res_type,
                            SUM(T1.commited_cost)    AS commited_cost
                        FROM pcesthmt T2
                        JOIN pcestdmt T1 ON T2.estimate_no = T1.estimate_no
                                        AND T2.dept_id = T1.dept_id
                        WHERE trim(T2.project_no) = :project_no
                        GROUP BY
                            CASE
                                WHEN T1.res_type IS NULL OR T1.res_type LIKE '%MAT%' THEN 'MAT'
                                WHEN T1.res_type LIKE 'LABOUR%' THEN 'LAB'
                                ELSE 'OTHER'
                            END,
                            T2.status, T2.apr_dt1, T2.conf_dt, T2.prj_ass_dt
                        ORDER BY 1, 2";

                    using (var cmd = new OracleCommand(sql, conn))
                    {
                        cmd.BindByName = true;
                        cmd.Parameters.Add("project_no", OracleDbType.Varchar2).Value = projectNo;

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                results.Add(new JobStatusHistoryDto
                                {
                                    Status = SafeGetString(reader, "status"),
                                    PhysicalClose = SafeGetDateTime(reader, "physical_close"),
                                    HardClose = SafeGetDateTime(reader, "hard_close"),
                                    JobCreatedDate = SafeGetDateTime(reader, "job_created_date"),
                                    ResourceType = SafeGetString(reader, "res_type"),
                                    CommittedCost = SafeGetDecimal(reader, "commited_cost")
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetJobStatusHistoryAsync error: {ex.Message}");
                throw;
            }

            return results;
        }

        // ────────────────────────────────────────────────
        // 10. Energized Info (meters)
        // ────────────────────────────────────────────────
        public async Task<List<EnergizedInfoDto>> GetEnergizedInfoAsync(string projectNo)
        {
            var results = new List<EnergizedInfoDto>();

            try
            {
                using (var conn = new OracleConnection(connectionString))
                {
                    await conn.OpenAsync();

                    const string sql = @"
                        SELECT
                            connected_date,
                            upd_date as finalized_date
                        FROM spodrcrd
                        WHERE trim(project_no) = :project_no";

                    using (var cmd = new OracleCommand(sql, conn))
                    {
                        cmd.BindByName = true;
                        cmd.Parameters.Add("project_no", OracleDbType.Varchar2).Value = projectNo;

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                results.Add(new EnergizedInfoDto
                                {
                                    EnergizedDate = SafeGetDateTime(reader, "connected_date"),
                                    finalized_date = SafeGetDateTime(reader, "finalized_date"),
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetEnergizedInfoAsync error: {ex.Message}");
                throw;
            }

            return results;
        }

        // ────────────────────────────────────────────────
        // Detail Estimate for the estimation information (list with summation)
        // ────────────────────────────────────────────────
        public async Task<List<ResourceCostSummaryDto>> GetResourceCostSummaryAsync(string estimateNo)
        {
            var results = new List<ResourceCostSummaryDto>();

            if (string.IsNullOrWhiteSpace(estimateNo))
                return results;

            try
            {
                using (var conn = new OracleConnection(connectionString))
                {
                    await conn.OpenAsync();

                    const string sql = @"
                SELECT DISTINCT res_type,
                       SUM(estimate_cost) AS total_estimate_cost
                FROM pcestdtt
                WHERE TRIM(estimate_no) = :estimateNo
                GROUP BY res_type
                ORDER BY res_type";

                    using (var cmd = new OracleCommand(sql, conn))
                    {
                        cmd.BindByName = true;
                        cmd.Parameters.Add("estimateNo", OracleDbType.Varchar2).Value = estimateNo.Trim();

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                results.Add(new ResourceCostSummaryDto
                                {
                                    ResType = SafeGetString(reader, "res_type"),
                                    TotalEstimateCost = (decimal)SafeGetDecimal(reader, "total_estimate_cost")
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetResourceCostSummaryAsync error: {ex.Message}");
                throw;
            }

            return results;
        }

        //___________________________________
        // MAterial Details
        //_________________________________

        public async Task<List<MaterialTransactionDto>> GetMaterialTransactionsAsync(string estimateNo)
        {
            var results = new List<MaterialTransactionDto>();

            try
            {
                using (var conn = new OracleConnection(connectionString))
                {
                    await conn.OpenAsync();

                    const string sql = @"
             Select DISTINCT
                res_cd,
                unit_price,
                estimate_qty,
                estimate_cost
             from pcestdtt
            where trim(estimate_no)=:estimate_no and res_cat=1 ORDER BY res_cd ASC";

                    using (var cmd = new OracleCommand(sql, conn))
                    {
                        cmd.BindByName = true;
                        cmd.Parameters.Add("estimate_no", OracleDbType.Varchar2).Value = estimateNo;

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                results.Add(new MaterialTransactionDto
                                {
                                    MatCode = SafeGetString(reader, "res_cd"),
                                    UnitPrice = SafeGetDecimal(reader, "unit_price"),
                                    EstimateQty = SafeGetDecimal(reader, "estimate_qty"),
                                    EstimateCost = SafeGetDecimal(reader, "estimate_cost")
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetMaterialTransactionsAsync error: {ex.Message}");
                throw;
            }

            return results;
        }

        //deatiled estimation for the job informations
        // ────────────────────────────────────────────────

        public async Task<List<ResourceCostSummaryDto>> GetResourceCostAsync(string estimateNo)
        {
            var results = new List<ResourceCostSummaryDto>();

            if (string.IsNullOrWhiteSpace(estimateNo))
                return results;

            try
            {
                using (var conn = new OracleConnection(connectionString))
                {
                    await conn.OpenAsync();

                    const string sql = @"
                SELECT res_type,
                       SUM(estimate_cost) AS total_estimate_cost
                FROM pcestdmt
                WHERE TRIM(estimate_no) = :estimateNo
                GROUP BY res_type
                ORDER BY res_type";

                    using (var cmd = new OracleCommand(sql, conn))
                    {
                        cmd.BindByName = true;
                        cmd.Parameters.Add("estimateNo", OracleDbType.Varchar2).Value = estimateNo.Trim();

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                results.Add(new ResourceCostSummaryDto
                                {
                                    ResType = SafeGetString(reader, "res_type"),
                                    TotalEstimateCost = (decimal)SafeGetDecimal(reader, "total_estimate_cost")
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetResourceCostSummaryAsync error: {ex.Message}");
                throw;
            }

            return results;
        }

        //___________________________________
        // MAterial Details
        //_________________________________

        public async Task<List<MaterialTransactionDto>> GetMaterialsforjobsAsync(string estimateNo)
        {
            var results = new List<MaterialTransactionDto>();

            try
            {
                using (var conn = new OracleConnection(connectionString))
                {
                    await conn.OpenAsync();

                    const string sql = @"
             Select
                res_cd,
                unit_price,
                estimate_qty,
                estimate_cost
             from pcestdmt
            where trim(estimate_no)=:estimate_no and res_cat=1 ORDER BY res_cd ASC";

                    using (var cmd = new OracleCommand(sql, conn))
                    {
                        cmd.BindByName = true;
                        cmd.Parameters.Add("estimate_no", OracleDbType.Varchar2).Value = estimateNo;

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                results.Add(new MaterialTransactionDto
                                {
                                    MatCode = SafeGetString(reader, "res_cd"),
                                    UnitPrice = SafeGetDecimal(reader, "unit_price"),
                                    EstimateQty = SafeGetDecimal(reader, "estimate_qty"),
                                    EstimateCost = SafeGetDecimal(reader, "estimate_cost")
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetMaterialTransactionsAsync error: {ex.Message}");
                throw;
            }

            return results;
        }

        // ────────────────────────────────────────────────
        // 10. Estimate Approvals
        // ────────────────────────────────────────────────
        public async Task<List<EstimateApprovalDto>> GetEstimateApprovalsAsync(string estimateNo)
        {
            var results = new List<EstimateApprovalDto>();

            try
            {
                using (var conn = new OracleConnection(connectionString))
                {
                    await conn.OpenAsync();

                    const string sql = @"
                SELECT
                    approved_level,
                    approved_date,
                    approved_time,
                    standard_cost,
                    detailed_cost
                FROM APPROVAL
                WHERE trim(reference_no) = :estimateNO
                AND to_status in (30,45)";  // there is an issue in this status double check --

                    using (var cmd = new OracleCommand(sql, conn))
                    {
                        cmd.BindByName = true;
                        cmd.Parameters.Add("estimateNO", OracleDbType.Varchar2).Value = estimateNo;

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                results.Add(new EstimateApprovalDto
                                {
                                    ApprovedLevel = SafeGetString(reader, "approved_level"),
                                    ApprovedDate = SafeGetDateTime(reader, "approved_date"),
                                    ApprovedTime = SafeGetString(reader, "approved_time"),
                                    StandardCost = SafeGetDecimal(reader, "standard_cost"),
                                    DetailedCost = SafeGetDecimal(reader, "detailed_cost")
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetEstimateApprovalsAsync error: {ex.Message}");
                throw;
            }

            return results;
        }

        // ────────────────────────────────────────────────
        // 10. Revised Estimate Approvals
        // ────────────────────────────────────────────────
        public async Task<List<EstimateApprovalDto>> GetRevisedEstimateApprovalsAsync(string projectNO)
        {
            var results = new List<EstimateApprovalDto>();

            try
            {
                using (var conn = new OracleConnection(connectionString))
                {
                    await conn.OpenAsync();

                    const string sql = @"
                SELECT
                    approved_level,
                    approved_date,
                    approved_time,
                    standard_cost,
                    detailed_cost
                FROM APPROVAL
                WHERE trim(reference_no) = :projectNO
                AND to_status in  ( 1,61)"; // here there is a mismatch of the status, double check

                    using (var cmd = new OracleCommand(sql, conn))
                    {
                        cmd.BindByName = true;
                        cmd.Parameters.Add("projectNO", OracleDbType.Varchar2).Value = projectNO;

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                results.Add(new EstimateApprovalDto
                                {
                                    ApprovedLevel = SafeGetString(reader, "approved_level"),
                                    ApprovedDate = SafeGetDateTime(reader, "approved_date"),
                                    ApprovedTime = SafeGetString(reader, "approved_time"),
                                    StandardCost = SafeGetDecimal(reader, "standard_cost"),
                                    DetailedCost = SafeGetDecimal(reader, "detailed_cost")
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetEstimateApprovalsAsync error: {ex.Message}");
                throw;
            }

            return results;
        }

        // ────────────────────────────────────────────────
        // 12. Job Finalized Info (PCESTHMT)
        // ────────────────────────────────────────────────
        public async Task<JobFinalizedDto> GetJobFinalizedAsync(string projectNo)
        {
            try
            {
                using (var conn = new OracleConnection(connectionString))
                {
                    await conn.OpenAsync();

                    const string sql = @"
                SELECT conf_dt,etimate_dt
                FROM pcesthmt
                WHERE status = 3
                AND TRIM(project_no) = :project_no";

                    using (var cmd = new OracleCommand(sql, conn))
                    {
                        cmd.BindByName = true;
                        cmd.Parameters.Add("project_no", OracleDbType.Varchar2).Value = projectNo;

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new JobFinalizedDto
                                {
                                    JobFinalizedDate = SafeGetDateTime(reader, "conf_dt"),
                                    EstimatedDate = SafeGetDateTime(reader, "etimate_dt")
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetJobFinalizedAsync error: {ex.Message}");
                throw;
            }

            return null;
        }

        // ────────────────────────────────────────────────
        // 13. Contractor Bill Info (BILL_DETAIL)
        // ────────────────────────────────────────────────
        public async Task<List<ContractorBillDto>> GetContractorBillsAsync(string projectNo)
        {
            var list = new List<ContractorBillDto>();

            try
            {
                using (var conn = new OracleConnection(connectionString))
                {
                    await conn.OpenAsync();

                    const string sql = @"
                SELECT bill_no,
                       bill_date
                FROM bill_detail
                WHERE TRIM(project_no) = :project_no";

                    using (var cmd = new OracleCommand(sql, conn))
                    {
                        cmd.BindByName = true;
                        cmd.Parameters.Add("project_no", OracleDbType.Varchar2).Value = projectNo;

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                list.Add(new ContractorBillDto
                                {
                                    ContractorBillNo = SafeGetString(reader, "bill_no"),
                                    ContractorBillDate = SafeGetDateTime(reader, "bill_date")
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetContractorBillsAsync error: {ex.Message}");
                throw;
            }

            return list;
        }

        // ────────────────────────────────────────────────
        // 14. Get PIV Details
        // ────────────────────────────────────────────────
        public async Task<List<PivDetailDto>> GetPivDetailsAsync(string referenceNo)
        {
            var list = new List<PivDetailDto>();

            try
            {
                using (var conn = new OracleConnection(connectionString))
                {
                    await conn.OpenAsync();

                    const string sql = @"
                SELECT
                    p.piv_no,
                    t.description AS piv_type,
                    p.piv_amount,
                    p.piv_date,
                    p.paid_date,
                    ac.description_1 AS status
                FROM piv_detail p
                JOIN applications ap
                    ON p.reference_no = ap.application_no
                JOIN piv_activity ac
                    ON ac.activity_code = p.status
                JOIN piv_type t
                    ON t.type_id = p.reference_type
                WHERE TRIM(p.reference_no) = :referenceNo";

                    using (var cmd = new OracleCommand(sql, conn))
                    {
                        cmd.BindByName = true;
                        cmd.Parameters.Add("referenceNo", OracleDbType.Varchar2).Value = referenceNo;

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                list.Add(new PivDetailDto
                                {
                                    PivNo = SafeGetString(reader, "piv_no"),
                                    PivType = SafeGetString(reader, "piv_type"),
                                    PivAmount = SafeGetDecimal(reader, "piv_amount"),
                                    PivDate = SafeGetDateTime(reader, "piv_date"),
                                    PaidDate = SafeGetDateTime(reader, "paid_date"),
                                    Status = SafeGetString(reader, "status")
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetPivDetailsAsync error: {ex.Message}");
                throw;
            }

            return list;
        }

        // ────────────────────────────────────────────────
        // 11. Appointment Info (SPESTEDY)
        // ────────────────────────────────────────────────
        public async Task<List<AppointmentInfoDto>> GetAppointmentInfoAsync(string referenceNo)
        {
            var results = new List<AppointmentInfoDto>();

            try
            {
                using (var conn = new OracleConnection(connectionString))
                {
                    await conn.OpenAsync();

                    const string sql = @"
                SELECT
                    appointment_date
                FROM SPESTEDY
                WHERE TRIM(reference_no) = :reference_no";

                    using (var cmd = new OracleCommand(sql, conn))
                    {
                        cmd.BindByName = true;
                        cmd.Parameters.Add("reference_no", OracleDbType.Varchar2).Value = referenceNo;

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                results.Add(new AppointmentInfoDto
                                {
                                    AppointmentDate = SafeGetDateTime(reader, "appointment_date"),
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetAppointmentInfoAsync error: {ex.Message}");
                throw;
            }

            return results;
        }

        // ────────────────────────────────────────────────
        // Helper methods (safer reading of DB values)
        // ────────────────────────────────────────────────
        private static string SafeGetString(OracleDataReader reader, string column)
        {
            return reader[column] != DBNull.Value ? reader[column].ToString() : null;
        }

        private static DateTime? SafeGetDateTime(OracleDataReader reader, string column)
        {
            return reader[column] != DBNull.Value ? (DateTime?)reader.GetDateTime(reader.GetOrdinal(column)) : null;
        }

        private static decimal? SafeGetDecimal(OracleDataReader reader, string column)
        {
            return reader[column] != DBNull.Value ? (decimal?)reader.GetDecimal(reader.GetOrdinal(column)) : null;
        }

        private static int? SafeGetInt(OracleDataReader reader, string column)
        {
            return reader[column] != DBNull.Value ? (int?)reader.GetInt32(reader.GetOrdinal(column)) : null;
        }
    }
}