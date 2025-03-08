using System;
using System.ComponentModel.DataAnnotations;

namespace flight.Models
{
    public class Booking
    {
        public int Id { get; set; }

        [Required]
        public int FlightId { get; set; }

        public Flight? Flight { get; set; }

        [Required]
        public string SeatType { get; set; } // "Economy", "Business", "FirstClass"

        [Required]
        public int NumberOfAdults { get; set; }

        [Required]
        public int NumberOfChildren { get; set; }

        [Required]
        public DateTime BookingDate { get; set; } = DateTime.UtcNow;

        public decimal TotalPrice { get; set; }

        public void CalculateTotalPrice()
        {
            if (Flight == null) return;

            decimal adultPrice = SeatType switch
            {
                "Economy" => Flight.EconomyClassPrice,
                "Business" => Flight.BusinessClassPrice,
                "FirstClass" => Flight.FirstClassPrice,
                _ => 0
            };

            decimal childPrice = adultPrice * 0.75m; // Assuming children get a 25% discount

            TotalPrice = (NumberOfAdults * adultPrice) + (NumberOfChildren * childPrice);
        }
    }
}