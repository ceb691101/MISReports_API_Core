using MISReports_Api.Compat;
using MISReports_Api.Models.SMC__Management_Information;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace MISReports_Api.DAL.SMC__Management_Information
{
    public class CostPaidJobProgressDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<CostPaidJobProgressModel> GetCostPaidJobProgress(
            string deptId,
            string applicationNo,
            DateTime fromDate,
            DateTime toDate)
        {
            // Validate parameters.
            if (string.IsNullOrWhiteSpace(deptId))
            {
                throw new ArgumentException(
                    "Department ID is required.");
            }

            if (string.IsNullOrWhiteSpace(applicationNo))
            {
                throw new ArgumentException(
                    "Application number search text is required.");
            }

            if (toDate.Date < fromDate.Date)
            {
                throw new ArgumentException(
                    "To date cannot be earlier than from date.");
            }

            var result = new List<CostPaidJobProgressModel>();

            string sql = @"
                SELECT
                    a.application_no AS APPLICATION_NO,
                    a.submit_date AS SUBMIT_DATE,

                    (b.first_name || ' ' || b.last_name) AS NAME,

                    (b.street_address || ' ' ||
                     b.suburb || ' ' ||
                     b.city) AS ADDRESS,

                    c1.APPROVED_DATE AS APPROVED_DATE,

                    c.Piv_no AS PIV_NO,
                    c.Paid_date AS PAID_DATE,
                    c.Piv_amount AS PIV_AMOUNT,

                    L.allocated_date AS ALLOCATED_DATE,
                    L.FINISHED_DATE AS FINISHED_DATE,
                    L.contractor_id AS CONTRACTOR_ID,

                    e.projectno AS PROJECTNO,

                    (
                        SELECT t2.etimate_dt
                        FROM pcesthtt t2
                        WHERE TRIM(t2.estimate_no) =
                              TRIM(a.application_no)
                          AND a.dept_id = t2.dept_id
                          AND t2.rev_no = 1
                    ) AS ETIMATE_DT,

                    (
                        SELECT c2.Piv_no
                        FROM piv_detail c2
                        WHERE c2.reference_no = c.reference_no
                          AND c2.reference_type = 'RIP'
                          AND c2.status IN ('C', 'P')
                    ) AS REINSPECTION_PIV_NO,

                    (
                        SELECT c2.Paid_date
                        FROM piv_detail c2
                        WHERE c2.reference_no = c.reference_no
                          AND c2.reference_type = 'RIP'
                          AND c2.status IN ('C', 'P')
                    ) AS REINSPECTION_PIV_DATE,

                    (
                        SELECT t2.prj_ass_dt
                        FROM pcesthmt t2
                        WHERE TRIM(t2.project_no) =
                              TRIM(e.projectno)
                          AND a.dept_id = t2.dept_id
                    ) AS PRJ_ASS_DT,

                    (
                        SELECT bd.bill_no
                        FROM bill_detail bd
                        WHERE bd.project_no = e.projectno
                    ) AS BILL_NO,

                    (
                        SELECT bd.add_date
                        FROM bill_detail bd
                        WHERE bd.project_no = e.projectno
                    ) AS BILL_DATE,

                    (
                        SELECT gd.dept_nm
                        FROM gldeptm gd
                        WHERE gd.dept_id = :deptId
                    ) AS CCT_NAME

                FROM
                    applications a,
                    applicant b,
                    piv_detail c,
                    approval c1,
                    (
                        application_reference e
                        LEFT OUTER JOIN spestcnd L
                            ON TRIM(e.projectno) =
                               TRIM(L.project_no)
                    )

                WHERE
                    TRIM(a.application_no) =
                        TRIM(c.reference_no)

                    AND b.Id_no = a.Id_no

                    AND c.Id_no = a.Id_no

                    AND a.dept_id = c.dept_id

                    AND a.application_id = e.application_id

                    AND a.dept_id = e.dept_id

                    AND c.reference_type = 'EST'

                    AND c.status IN ('C', 'P')

                    AND a.dept_id = :deptId

                    AND a.application_No LIKE
                        '%' || :applicationNo || '%'

                    AND c1.reference_no = a.application_No

                    AND c1.TO_STATUS = 30

                    AND a.submit_date >= :fromDate

                    AND a.submit_date < :toDateExclusive

                ORDER BY
                    a.application_no";

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.CommandType = CommandType.Text;

                // Bind by parameter name rather than position.
                cmd.BindByName = true;

                // Department ID.
                cmd.Parameters.Add(
                    "deptId",
                    OracleDbType.Varchar2
                ).Value = deptId.Trim();

                // Application number search text.
                cmd.Parameters.Add(
                    "applicationNo",
                    OracleDbType.Varchar2
                ).Value = applicationNo.Trim();

                // Inclusive start date.
                cmd.Parameters.Add(
                    "fromDate",
                    OracleDbType.Date
                ).Value = fromDate.Date;

                // Exclusive upper boundary includes the entire end date.
                cmd.Parameters.Add(
                    "toDateExclusive",
                    OracleDbType.Date
                ).Value = toDate.Date.AddDays(1);

                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var model = new CostPaidJobProgressModel
                        {
                            ApplicationNo =
                                GetString(reader, "APPLICATION_NO"),

                            SubmitDate =
                                GetNullableDateTime(
                                    reader, "SUBMIT_DATE"),

                            Name =
                                GetString(reader, "NAME"),

                            Address =
                                GetString(reader, "ADDRESS"),

                            ApprovedDate =
                                GetNullableDateTime(
                                    reader, "APPROVED_DATE"),

                            PivNo =
                                GetString(reader, "PIV_NO"),

                            PaidDate =
                                GetNullableDateTime(
                                    reader, "PAID_DATE"),

                            PivAmount =
                                GetNullableDecimal(
                                    reader, "PIV_AMOUNT"),

                            AllocatedDate =
                                GetNullableDateTime(
                                    reader, "ALLOCATED_DATE"),

                            FinishedDate =
                                GetNullableDateTime(
                                    reader, "FINISHED_DATE"),

                            ContractorId =
                                GetString(reader, "CONTRACTOR_ID"),

                            ProjectNo =
                                GetString(reader, "PROJECTNO"),

                            EstimateDate =
                                GetNullableDateTime(
                                    reader, "ETIMATE_DT"),

                            ReinspectionPivNo =
                                GetString(
                                    reader, "REINSPECTION_PIV_NO"),

                            ReinspectionPivDate =
                                GetNullableDateTime(
                                    reader, "REINSPECTION_PIV_DATE"),

                            ProjectAssignedDate =
                                GetNullableDateTime(
                                    reader, "PRJ_ASS_DT"),

                            BillNo =
                                GetString(reader, "BILL_NO"),

                            BillDate =
                                GetNullableDateTime(
                                    reader, "BILL_DATE"),

                            CctName =
                                GetString(reader, "CCT_NAME")
                        };

                        result.Add(model);
                    }
                }
            }

            return result;
        }

        private static string GetString(
            OracleDataReader reader,
            string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);

            if (reader.IsDBNull(ordinal))
            {
                return null;
            }

            string value = Convert.ToString(
                reader.GetValue(ordinal),
                CultureInfo.InvariantCulture);

            return CleanOracleString(value);
        }

        private static string CleanOracleString(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            return new string(
                value.Where(c =>
                    c == '\t' ||
                    c == '\n' ||
                    c == '\r' ||
                    (!char.IsControl(c) &&
                     !char.IsSurrogate(c))
                ).ToArray()
            ).Trim();
        }

        private static DateTime? GetNullableDateTime(
            OracleDataReader reader,
            string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);

            if (reader.IsDBNull(ordinal))
            {
                return null;
            }

            return Convert.ToDateTime(
                reader.GetValue(ordinal),
                CultureInfo.InvariantCulture);
        }

        private static decimal? GetNullableDecimal(
            OracleDataReader reader,
            string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);

            if (reader.IsDBNull(ordinal))
            {
                return null;
            }

            OracleDecimal oracleValue =
                reader.GetOracleDecimal(ordinal);

            if (oracleValue.IsNull)
            {
                return null;
            }

            try
            {
                return oracleValue.Value;
            }
            catch (OverflowException ex)
            {
                throw new InvalidOperationException(
                    "Oracle numeric column '" + columnName +
                    "' cannot be represented as a .NET decimal.",
                    ex);
            }
        }
    }
}