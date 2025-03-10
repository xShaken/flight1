using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace flight.Models
{
    public class Guest
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "First name is required.")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last name is required.")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Title is required.")]

        public string Title { get; set; } // Mr, Mrs, Ms, Dr

        [Required(ErrorMessage = "Date of birth is required.")]
        public DateTime DateOfBirth { get; set; }

        [Required(ErrorMessage = "Nationality is required.")]
        public string Nationality { get; set; }

       

        [Required(ErrorMessage = "IsChild is required.")]
        public bool IsChild { get; set; } // Indicates if the guest is a child

        [ForeignKey("Booking")]
        public int BookingId { get; set; } // Foreign key to Booking
        public Booking Booking { get; set; }
    }
}