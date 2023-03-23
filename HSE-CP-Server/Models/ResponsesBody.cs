using HSE_CP_Server.Models;
using Microsoft.Extensions.Hosting;

namespace HSE_CP_Server.Models
{
    public class ResponsePrice
    {
        public int IdProcedure { get; set; }
        public double Cost { get; set; }
        public string? PhotoName { get; set; }
        public string ProcedureName { get; set; }

        public ResponsePrice(int IdProcedure, double Cost, string? PhotoName, string ProcedureName)
        {
            this.IdProcedure = IdProcedure;
            this.Cost = Cost;
            this.ProcedureName = ProcedureName;
            this.PhotoName = PhotoName;
        }
    }

    public class ResponseVisit
    {
        public int IdVisit { get; set; }
        public string Date { get; set; }

        public ResponseVisit(int IdVisit, string Date)
        {
            this.IdVisit = IdVisit;
            this.Date = Date;
        }
    }

    public class ResponseVisitId
    {
        public string Date { get; set; }
        public double Cost { get; set; }
        public double SaleSize { get; set; }  
        public List<ResponsePriceInVisit> ResponsePriceInVisits { get; set; }

        public ResponseVisitId(string Date, double Cost, double SaleSize, List<ResponsePriceInVisit> ResponsePriceInVisits)
        {
            this.Date = Date;
            this.Cost = Cost;
            this.SaleSize = SaleSize;
            this.ResponsePriceInVisits = ResponsePriceInVisits;
        }
    }
    public class ResponsePriceInVisit
    {
        public int IdProcedure { get; set; }
        public double Cost { get; set; }
        public string ProcedureName { get; set; }

        public ResponsePriceInVisit(int IdProcedure, double Cost, string ProcedureName)
        {
            this.IdProcedure = IdProcedure;
            this.Cost = Cost;
            this.ProcedureName = ProcedureName;
        }
    }

}
