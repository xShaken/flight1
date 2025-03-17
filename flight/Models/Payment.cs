using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace flight.Models
{
    public class Payment
    {
        public int Id { get; set; }

        [Required]
        [ForeignKey("Booking")]
        public int BookingId { get; set; }
        public Booking Booking { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

        [Required]
        public string Status { get; set; } // "Pending", "Completed", "Failed", "Refunded"

        public string PaymentMethod { get; set; } // "Credit Card", "PayPal", etc.

        public string TransactionId { get; set; } // Unique identifier for the transaction
    }
}