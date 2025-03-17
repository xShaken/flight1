using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace flight.Models
{
    public class Booking
    {
        public int Id { get; set; }

        [Required]
        [ForeignKey("Flight")]
        public int FlightId { get; set; }
        public Flight Flight { get; set; }

        [ForeignKey("ReturnFlight")]
        public int? ReturnFlightId { get; set; }
        public Flight ReturnFlight { get; set; }

        [ForeignKey("Users")]
        public string? UserId { get; set; } // Make this nullable
        public Users User { get; set; }

        [Required]
        public string SeatClass { get; set; }

        [Required]
        public int NumberOfAdults { get; set; }

        [Required]
        public int NumberOfChildren { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        [Required]
        public DateTime DepartureDate { get; set; }

        public DateTime? ReturnDate { get; set; }

        public DateTime BookingDate { get; set; } = DateTime.UtcNow;

        public string Status { get; set; } = "Pending";

        public List<Guest> Guests { get; set; } = new List<Guest>();
        public Payment Payment { get; set; } = new Payment
        {
            PaymentMethod = "Credit Card", // Set a default value
            Status = "Pending", // Set a default value
            TransactionId = Guid.NewGuid().ToString(), // Generate a default value
            Amount = 0, // Set a default value
            PaymentDate = DateTime.UtcNow // Set a default value
        };
    }
}