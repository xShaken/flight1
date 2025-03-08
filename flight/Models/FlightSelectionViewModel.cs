namespace flight.Models
{
    public class FlightSelectionViewModel
    {
        public Flight DepartureFlight { get; set; } // Selected departure flight
        public Flight ReturnFlight { get; set; } // Selected return flight (for round-trip)
        public string SeatClass { get; set; } // Selected seat class (Economy, Business, First Class)
        public int NumberOfAdults { get; set; } // Number of adult passengers
        public int NumberOfChildren { get; set; } // Number of child passengers
    }
}