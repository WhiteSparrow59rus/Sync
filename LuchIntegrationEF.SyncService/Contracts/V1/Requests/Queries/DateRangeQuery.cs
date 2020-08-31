using System;

namespace LuchIntegrationEF.SyncService.Contracts.V1.Requests.Queries
{
    public class DateRangeQuery
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
    }
}