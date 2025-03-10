using flight.Models;
using System.ComponentModel.DataAnnotations;

namespace flight.ViewModels
{
    public class FlightSelectionViewModel
    {
        [Required]
        public Flight DepartureFlight { get; set; }

        // Make ReturnFlight nullable since it's optional for one-way trips
        public Flight ReturnFlight { get; set; }

        [Required]
        public string SeatClass { get; set; }

        [Required]
        public int NumberOfAdults { get; set; }

        public int NumberOfChildren { get; set; }

        // Make DepartureAirport nullable or remove [Required]
        public Airport DepartureAirport { get; set; }

        // For guest bookings, this might be auto-generated
        public string UserId { get; set; }

        // Remove this if not needed or make it nullable
        public Booking Booking { get; set; }
    }
}