using System;
using System.Collections.Generic;

namespace MISReports_Api.Models.General
{
    // ── Main report row model ──────────────────────────────────────────────────
    public class ArrearsPositionModel
    {
        public string ReaderCode { get; set; }

        // Formatted display values (matching VB legacy format strings)
        public string Charge { get; set; }   // ###,###,###.#0
        public string CrntBalance { get; set; }   // ###,###,###.#0
        public string Ratio { get; set; }   // ##0.#0
        public string ReaderCount { get; set; }   // ###,###,##0

        // Raw numeric values (for summing / further processing on the frontend)
        public decimal RawCharge { get; set; }
        public decimal RawCrntBalance { get; set; }
        public decimal RawRatio { get; set; }
        public int RawReaderCount { get; set; }

        public string ErrorMessage { get; set; }
    }

    // ── Request model ──────────────────────────────────────────────────────────
    public class ArrearsPositionRequest
    {
        public string BillCycle { get; set; }
        public string AreaCode { get; set; }
    }

    // ── Bill-cycle response model (reused shape expected by the frontend) ──────
    public class ArrearsPositionBillCycleModel
    {
        public string MaxBillCycle { get; set; }
        public List<string> BillCycles { get; set; }
        public string ErrorMessage { get; set; }
    }
}