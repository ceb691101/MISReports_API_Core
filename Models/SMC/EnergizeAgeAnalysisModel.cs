using System;
using System.Collections.Generic;
using System.Linq;

namespace MISReports_Api.Models.Accounts
{
    public class EnergizeAgeAnalysisModel
    {
        public string Period { get; set; }

        // Matches the query's "count(T1.project_no) as SUM" - a count of projects falling
        // into this age-bucket, not a monetary/quantity sum. Named Sum to mirror the SQL
        // alias exactly.
        public int Sum { get; set; }

        // Constant across every row (a single scalar subquery, not correlated to Period) -
        // the total number of confirmed/paid EST PIVs for the cost center in the date range.
        public int NoOfJobs { get; set; }

        public string CctName { get; set; }
    }
}