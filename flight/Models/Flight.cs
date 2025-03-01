using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace flight.Models
{
    public class Flight
    {
        public int Id { get; set; }

        [Required]
        public string FlightNumber { get; set; } // Unique identifier for the flight

        [Required]
        [ForeignKey("Airline")]
        public int AirlineId { get; set; } // Foreign key reference to Airline

        public Airline? Airline { get; set; } // Nullable to prevent model binding issues

        [Required]
        [ForeignKey("DepartureAirport")]
        public int DepartureAirportId { get; set; } // Foreign key reference to departure Airport

        public Airport? DepartureAirport { get; set; } // Nullable to prevent model binding issues

        [Required]
        [ForeignKey("ArrivalAirport")]
        public int ArrivalAirportId { get; set; } // Foreign key reference to arrival Airport

        public Airport? ArrivalAirport { get; set; } // Nullable to prevent model binding issues

        [Required]
        public DateTime DepartureTime { get; set; }

        [Required]
        public DateTime ArrivalTime { get; set; }

        [Required]
        public string Status { get; set; } = "Scheduled"; // Flight status (Scheduled, Delayed, Cancelled)

        public DateTime DateAdded { get; set; } = DateTime.UtcNow;
    }
}
