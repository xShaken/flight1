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
        public int NumberOfSeats { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }


        public DateTime BookingDate { get; set; } = DateTime.UtcNow;

        public string Status { get; set; } = "Pending"; // Pending, Confirmed, Cancelled
    }
}
