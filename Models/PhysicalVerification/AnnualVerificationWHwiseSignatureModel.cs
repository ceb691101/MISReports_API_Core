using System;

namespace MISReports_Api.Models.PhysicalVerification
{
    public class AnnualVerificationWHwiseSignatureModel
    {
        public string MaterialCode { get; set; }    
        public string MaterialName { get; set; }      
        public string UomCode { get; set; }           
        public string GradeCode { get; set; }        
        public decimal UnitPrice { get; set; }      
        public decimal QtyOnHand { get; set; }       
        public decimal CountedQty { get; set; }     
        public string DocumentNo { get; set; }        
        public DateTime? PhvDate { get; set; }        
        public decimal? SurplusQty { get; set; }      
        public decimal? ShortageQty { get; set; }   
        public decimal StockBook { get; set; }       
        public decimal PhysicalBook { get; set; }    
        public decimal? SurplusAmount { get; set; }   
        public decimal? ShortageAmount { get; set; }  
        public string Reason { get; set; }            
    }
}
