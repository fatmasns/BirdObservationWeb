namespace BirdObservationWeb.Models
{
    public class SightingViewModel
    {
        public string BirdName { get; set; }
        public string LocationName { get; set; }
        public string WeatherType { get; set; }
        public string ObserverName { get; set; }
        public DateTime SightingTime { get; set; }
    }
}