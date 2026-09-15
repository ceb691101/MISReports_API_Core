using System;
using System.Collections.Generic;
using System.Linq;

namespace MISReports_Api.Models.Accounts
{
    public class CompletedJobsCCModel
    {
        public string ApplicationType { get; set; }
        public string ApplicationNo { get; set; }
        public string PivNo { get; set; }
        public string PivNo3 { get; set; }
        public DateTime? PivDate3 { get; set; }
        public string PivAmount3 { get; set; }
        public DateTime? PivDate { get; set; }
        public string PivAmount { get; set; }
        public DateTime? ConfirmedDate { get; set; }
        public DateTime? SubmitDate { get; set; }
        public string ProjectNo { get; set; }
        public string Name { get; set; }
        public string Amount { get; set; }
        public DateTime? FinishedDate { get; set; }
        public string ContractorName { get; set; }
        public string MeterNo1 { get; set; }
        public string InitReading1 { get; set; }
        public string MeterNo2 { get; set; }
        public string InitReading2 { get; set; }
        public string MeterNo3 { get; set; }
        public string InitReading3 { get; set; }
        public string NeighboursAccNo { get; set; }
        public string Phase { get; set; }
        public string ConnectionType { get; set; }
        public string TariffCatCode { get; set; }
        public string TariffCode { get; set; }
        public string TotalLength { get; set; }
        public string Sin { get; set; }
        public string WiringType { get; set; }
        public DateTime? ConnectedDate { get; set; }
        public string Address { get; set; }
        public string ServiceCity { get; set; }
        public string AccountNo { get; set; }
        public DateTime? AccCreatedDate { get; set; }
        public string PivReceiptNo { get; set; }
        public string CctName { get; set; }
    }
}