namespace BirdObservationWeb.Models
{
    public class Bird
    {
        public int BirdID { get; set; }
        public string CommonName { get; set; }
        public string? Species { get; set; }
        public decimal WingSpan { get; set; }
        public string EndangeredStatus { get; set; }
        public string? Color { get; set; }
    }
}