using System;

namespace MISReports_Api.Models.FinancialDashboard
{
    public class CachedValue<T>
    {
        public T Value { get; set; }
        public DateTimeOffset FetchedAt { get; set; }
    }
}
