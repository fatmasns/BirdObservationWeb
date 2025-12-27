namespace BirdObservationWeb.Models
{
    public class SightingView
    {
        public int BirdId { get; set; }
        public string? BirdName { get; set; }
        public string? LocationName { get; set; }
        public string? WeatherType { get; set; }
        public string? ObserverName { get; set; }
        public DateTime SightingTime { get; set; }
        public string ExperienceLevel { get; set; } = "Çaylak";
        public string Notes { get; set; } = "";
    }
}