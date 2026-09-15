using System;
using System.Collections.Generic;

namespace MISReports_Api.Models.General
{
    /// <summary>
    /// Represents a single customer record returned by the Listing of Customers report.
    /// Columns map directly to the prn_dat_1 fields surfaced in the legacy VB report.
    /// </summary>
    public class ListingOfCustomerModel
    {
        // ── Identity ────────────────────────────────────────────────────────
        public string AccountNumber { get; set; }   // acct_number
        public string MeterNumbers { get; set; }   // met_no1 + met_no2 + met_no3 (comma-separated)
        public string CustomerName { get; set; }   // cust_fname + cust_lname
        public string Address { get; set; }   // address_1 + address_2 + address_3

        // ── Tariff / Classification ──────────────────────────────────────────
        public string Tariff { get; set; }   // tariff_code
        public string CurrentDepot { get; set; }   // crnt_depot
        public string Transformer { get; set; }   // substn_code
        public string ReaderCode { get; set; }   // reader_code

        // ── Billing ─────────────────────────────────────────────────────────
        public string KwhCharge { get; set; }   // kwh_charge  (formatted)
        public string CurrentBalance { get; set; }   // crnt_balance (formatted with commas)
        public decimal RawKwhCharge { get; set; }   // raw numeric value
        public decimal RawCurrentBalance { get; set; } // raw numeric value

        // ── Supply Characteristics ───────────────────────────────────────────
        public string NoOfPhase { get; set; }   // no_of_phase
        public string ConnectionType { get; set; }   // conect_type
        public string DailyPackNo { get; set; }   // dly_pack_no
        public string WalkSeq { get; set; }   // walk_seq
        public string KvaRating { get; set; }   // kva_rating

        // ── Internal / Metadata ──────────────────────────────────────────────
        public string AreaCode { get; set; }
        public string AreaName { get; set; }
        public string BillCycle { get; set; }
        public string ErrorMessage { get; set; }
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Request / Filter model
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Carries all optional filter flags sent by the frontend to the API.
    /// Each "chk*" flag mirrors the legacy checkbox that activates the filter.
    /// </summary>
    public class ListingOfCustomerRequest
    {
        // ── Required ─────────────────────────────────────────────────────────
        public string AreaCode { get; set; }
        public string BillCycle { get; set; }

        // ── Optional filters (set flag + value together) ─────────────────────
        public bool UseTariff { get; set; }
        public string Tariff { get; set; }

        public bool UseTransformer { get; set; }
        public string Transformer { get; set; }   // substn_code

        public bool UsePhase { get; set; }
        public string Phase { get; set; }   // no_of_phase

        public bool UseConnectionType { get; set; }
        public string ConnectionType { get; set; }   // conect_type

        public bool UseReaderCode { get; set; }
        public string ReaderCode { get; set; }

        public bool UseDailyPack { get; set; }
        public string DailyPackNo { get; set; }   // dly_pack_no

        public bool UseDepot { get; set; }
        public string Depot { get; set; }   // crnt_depot

        public bool UseBalance { get; set; }
        public string BalanceOperator { get; set; }   // =, >, <, >=, <=
        public string BalanceAmount { get; set; }

        public bool UseLastPaymentDate { get; set; }
        public string LastPaymentOperator { get; set; } // =, >, <, >=, <=
        public string LastPaymentDate { get; set; }

        public bool UseArrearsPosition { get; set; }
        public string ArrearsOperator { get; set; }  // >=, >, =, <, <=
        public string ArrearsPosition { get; set; }  // numeric value e.g. "1"
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Dropdown option models
    // ════════════════════════════════════════════════════════════════════════

    public class FilterOptionModel
    {
        public string Value { get; set; }
        public string Label { get; set; }
    }

    /// <summary>
    /// Bundles all dropdown lists returned in a single call so the
    /// frontend can populate every filter control at once.
    /// </summary>
    public class ListingOfCustomerFiltersModel
    {
        public string BillCycle { get; set; }
        public List<FilterOptionModel> Tariffs { get; set; } = new List<FilterOptionModel>();
        public List<FilterOptionModel> Transformers { get; set; } = new List<FilterOptionModel>();
        public List<FilterOptionModel> Phases { get; set; } = new List<FilterOptionModel>();
        public List<FilterOptionModel> ConnectionTypes { get; set; } = new List<FilterOptionModel>();
        public List<FilterOptionModel> ReaderCodes { get; set; } = new List<FilterOptionModel>();
        public List<FilterOptionModel> DailyPacks { get; set; } = new List<FilterOptionModel>();
        public List<FilterOptionModel> Depots { get; set; } = new List<FilterOptionModel>();
        public string ErrorMessage { get; set; }
    }
}