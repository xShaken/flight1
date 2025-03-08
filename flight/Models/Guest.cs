using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace flight.Models
{
    public class Guest
    {
        public int Id { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        public bool IsChild { get; set; } // Indicates if the guest is a child

        [ForeignKey("Booking")]
        public int BookingId { get; set; }
        public Booking? Booking { get; set; }
    }
}