using System;
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
        public Flight? Flight { get; set; }

        [Required]
        [ForeignKey("Users")]
        public string UserId { get; set; }
        public Users? User { get; set; }

        [Required]
        public string SeatClass { get; set; } // Business, Economy, First Class

        [Required]
        public int NumberOfAdults { get; set; } // Number of adult passengers

        [Required]
        public int NumberOfChildren { get; set; } // Number of child passengers

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; } // Automatically calculated based on seat selection



        [Required]
        public DateTime DepartureDate { get; set; } // Selected departure date

        public DateTime? ReturnDate { get; set; } // Optional return date for round-trip bookings

        public DateTime BookingDate { get; set; } = DateTime.UtcNow;

        public string Status { get; set; } = "Pending"; // Pending, Confirmed, Cancelled
    }
}