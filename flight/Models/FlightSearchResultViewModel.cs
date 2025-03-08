namespace flight.Models
{
    public class FlightSearchResultViewModel
    {
        public List<Flight> DepartureFlights { get; set; }
        public List<Flight> ReturnFlights { get; set; }
        public bool IsRoundTrip { get; set; }
        public int NumberOfAdults { get; set; }
        public int NumberOfChildren { get; set; }
        public string SeatClass { get; set; } // Selected seat class (Economy, Business, First Class)
    }
}