using System;
using System.Collections.Generic;
using System.Linq;
using System.Configuration;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using MISReports_Api.Models.Accounts;

namespace MISReports_Api.DAL
{
    public class CompletedJobsCCDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;
        private static string SafeGetString(OracleDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            if (reader.IsDBNull(ordinal)) return null;

            Type fieldType = reader.GetFieldType(ordinal);
            if (fieldType == typeof(decimal))
            {
                OracleDecimal od = reader.GetOracleDecimal(ordinal);
                try
                {
                    od = OracleDecimal.SetPrecision(od, 28);
                    return od.Value.ToString();
                }
                catch (OverflowException)
                {
                    return ((double)od).ToString();
                }
            }

            return reader.GetValue(ordinal)?.ToString();
        }

        private static DateTime? SafeGetDate(OracleDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            if (reader.IsDBNull(ordinal)) return null;
            return Convert.ToDateTime(reader.GetValue(ordinal));
        }

        public List<CompletedJobsCCModel> GetCompletedJobsCC(DateTime fromDate, DateTime toDate, string costCtr, string jobType)
        {
            var result = new List<CompletedJobsCCModel>();

            const string query = @"
                SELECT DISTINCT
                        APPLICATIONS.APPLICATION_TYPE,
                        APPLICATIONS.APPLICATION_NO,
                        PIV_DETAIL.PIV_NO,
                        (SELECT p1.piv_no
                           FROM piv_detail p1
                          WHERE p1.REFERENCE_NO = TRIM(PCESTHMT.project_NO)
                            AND PCESTHMT.DEPT_ID = p1.dept_id
                            AND p1.status = 'P') AS piv_no3,
                        (SELECT p1.piv_date
                           FROM piv_detail p1
                          WHERE p1.REFERENCE_NO = TRIM(PCESTHMT.project_NO)
                            AND PCESTHMT.DEPT_ID = p1.dept_id
                            AND p1.status = 'P') AS piv_date3,
                        (SELECT p1.piv_amount
                           FROM piv_detail p1
                          WHERE TRIM(p1.REFERENCE_NO) = TRIM(PCESTHMT.project_NO)
                            AND PCESTHMT.DEPT_ID = p1.dept_id
                            AND p1.status = 'P') AS piv_amount3,
                        PIV_DETAIL.PIV_DATE,
                        PIV_DETAIL.PIV_AMOUNT,
                        PIV_DETAIL.CONFIRMED_DATE,
                        APPLICATIONS.SUBMIT_DATE,
                        PCESTHMT.PROJECT_NO,
                        (APPLICANT.FIRST_NAME ||''||APPLICANT.LAST_NAME) AS NAME,
                        SPESTCND.AMOUNT,
                        SPESTCND.FINISHED_DATE,
                        SPESTCNT.CONTRACTOR_NAME,
                        SPODRCRD.METER_NO_1,
                        SPODRCRD.INIT_READING_1,
                        SPODRCRD.METER_NO_2,
                        SPODRCRD.INIT_READING_2,
                        SPODRCRD.METER_NO_3,
                        SPODRCRD.INIT_READING_3,
                        WIRING_LAND_DETAIL.NEIGHBOURS_ACC_NO,
                        WIRING_LAND_DETAIL.PHASE,
                        WIRING_LAND_DETAIL.CONNECTION_TYPE,
                        WIRING_LAND_DETAIL.TARIFF_CAT_CODE,
                        WIRING_LAND_DETAIL.TARIFF_CODE,
                        SPSEREST.TOTAL_LENGTH,
                        SPSEREST.SIN,
                        SPSEREST.WIRING_TYPE,
                        SPODRCRD.CONNECTED_DATE,
                        TRIM(WIRING_LAND_DETAIL.SERVICE_STREET_ADDRESS) || ',' || 
                        TRIM(WIRING_LAND_DETAIL.SERVICE_SUBURB) || ',' || 
                        TRIM(WIRING_LAND_DETAIL.SERVICE_CITY) AS SERVICE_ADDRESS,
                        SPEXPJOB.ACCOUNT_NO,
                        SPEXPJOB.ACC_CREATED_DATE,
                        PIV_DETAIL.PIV_RECEIPT_NO,
                        (SELECT dept_nm FROM gldeptm WHERE TRIM(dept_id) = TRIM(:costctr)) AS cct_name
                FROM      PCESTHMT
                JOIN      APPLICATIONS   ON TRIM(PCESTHMT.ESTIMATE_NO) = TRIM(APPLICATIONS.APPLICATION_NO)
                JOIN      PIV_DETAIL     ON APPLICATIONS.APPLICATION_ID = PIV_DETAIL.REFERENCE_NO
                                         OR APPLICATIONS.APPLICATION_NO = PIV_DETAIL.REFERENCE_NO
                JOIN      APPLICANT      ON APPLICATIONS.ID_NO = APPLICANT.ID_NO
                JOIN      SPESTCND       ON PCESTHMT.PROJECT_NO = SPESTCND.PROJECT_NO
                JOIN      SPESTCNT       ON SPESTCND.CONTRACTOR_ID = SPESTCNT.CONTRACTOR_ID
                                        AND SPESTCND.DEPT_ID = SPESTCNT.DEPT_ID
                JOIN      SPODRCRD       ON SPESTCND.PROJECT_NO = SPODRCRD.PROJECT_NO
                JOIN      WIRING_LAND_DETAIL ON APPLICATIONS.APPLICATION_ID = WIRING_LAND_DETAIL.APPLICATION_ID
                JOIN      SPSEREST       ON APPLICATIONS.APPLICATION_NO = SPSEREST.APPLICATION_NO
                LEFT OUTER JOIN SPEXPJOB ON PCESTHMT.PROJECT_NO = SPEXPJOB.PROJECT_NO
                WHERE     TRIM(APPLICATIONS.DEPT_ID) = TRIM(:costctr)
                  AND     (SPESTCND.STATUS = 'F' OR SPESTCND.STATUS = 'B')
                  AND     SPESTCND.CONTRACTOR_ID IN
                          (SELECT SPESTCNT.CONTRACTOR_ID
                             FROM SPESTCNT
                            WHERE TRIM(SPESTCNT.DEPT_ID) = TRIM(:costctr))
                  AND     TRIM(PCESTHMT.DEPT_ID) = TRIM(:costctr)
                  AND     SPESTCND.FINISHED_DATE >= :fromdate
                  AND     SPESTCND.FINISHED_DATE <  :todateexcl
                  AND     APPLICATIONS.APPLICATION_TYPE = :jobtype
                ORDER BY  APPLICATIONS.APPLICATION_TYPE DESC, PCESTHMT.PROJECT_NO ASC";

            string costCtrTrimmed = (costCtr ?? string.Empty).Trim();
            string jobTypeTrimmed = (jobType ?? string.Empty).Trim();
            DateTime toDateExclusive = toDate.Date.AddDays(1);

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(query, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.BindByName = true; // required so :costctr (used multiple times) binds correctly by name

                cmd.Parameters.Add(new OracleParameter("costctr", OracleDbType.Varchar2) { Value = costCtrTrimmed });
                cmd.Parameters.Add(new OracleParameter("fromdate", OracleDbType.Date) { Value = fromDate.Date });
                cmd.Parameters.Add(new OracleParameter("todateexcl", OracleDbType.Date) { Value = toDateExclusive });
                cmd.Parameters.Add(new OracleParameter("jobtype", OracleDbType.Varchar2) { Value = jobTypeTrimmed });

                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new CompletedJobsCCModel
                        {
                            ApplicationType = SafeGetString(reader, "APPLICATION_TYPE"),
                            ApplicationNo = SafeGetString(reader, "APPLICATION_NO"),
                            PivNo = SafeGetString(reader, "PIV_NO"),
                            PivNo3 = SafeGetString(reader, "PIV_NO3"),
                            PivDate3 = SafeGetDate(reader, "PIV_DATE3"),
                            PivAmount3 = SafeGetString(reader, "PIV_AMOUNT3"),
                            PivDate = SafeGetDate(reader, "PIV_DATE"),
                            PivAmount = SafeGetString(reader, "PIV_AMOUNT"),
                            ConfirmedDate = SafeGetDate(reader, "CONFIRMED_DATE"),
                            SubmitDate = SafeGetDate(reader, "SUBMIT_DATE"),
                            ProjectNo = SafeGetString(reader, "PROJECT_NO"),
                            Name = SafeGetString(reader, "NAME"),
                            Amount = SafeGetString(reader, "AMOUNT"),
                            FinishedDate = SafeGetDate(reader, "FINISHED_DATE"),
                            ContractorName = SafeGetString(reader, "CONTRACTOR_NAME"),
                            MeterNo1 = SafeGetString(reader, "METER_NO_1"),
                            InitReading1 = SafeGetString(reader, "INIT_READING_1"),
                            MeterNo2 = SafeGetString(reader, "METER_NO_2"),
                            InitReading2 = SafeGetString(reader, "INIT_READING_2"),
                            MeterNo3 = SafeGetString(reader, "METER_NO_3"),
                            InitReading3 = SafeGetString(reader, "INIT_READING_3"),
                            NeighboursAccNo = SafeGetString(reader, "NEIGHBOURS_ACC_NO"),
                            Phase = SafeGetString(reader, "PHASE"),
                            ConnectionType = SafeGetString(reader, "CONNECTION_TYPE"),
                            TariffCatCode = SafeGetString(reader, "TARIFF_CAT_CODE"),
                            TariffCode = SafeGetString(reader, "TARIFF_CODE"),
                            TotalLength = SafeGetString(reader, "TOTAL_LENGTH"),
                            Sin = SafeGetString(reader, "SIN"),
                            WiringType = SafeGetString(reader, "WIRING_TYPE"),
                            ConnectedDate = SafeGetDate(reader, "CONNECTED_DATE"),
                            Address = SafeGetString(reader, "SERVICE_ADDRESS"),
                            AccountNo = SafeGetString(reader, "ACCOUNT_NO"),
                            AccCreatedDate = SafeGetDate(reader, "ACC_CREATED_DATE"),
                            PivReceiptNo = SafeGetString(reader, "PIV_RECEIPT_NO"),
                            CctName = SafeGetString(reader, "CCT_NAME")
                        });
                    }
                }
            }

            return result;
        }
    }
}