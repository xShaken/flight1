using System;
using System.ComponentModel.DataAnnotations;

namespace flight.Models
{
    public class Airport
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string Location { get; set; }

        public string Code { get; set; } // Optional Airport Code (e.g., LAX, JFK)

        public DateTime DateAdded { get; set; } = DateTime.Now;
    }
}
