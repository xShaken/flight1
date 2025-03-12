using flight.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace flight.Data
{
    public class AppDbContext : IdentityDbContext<Users>
    {
        public DbSet<Flight> Flights { get; set; }
        public DbSet<Airline> Airlines { get; set; }
        public DbSet<Airport> Airports { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Guest> Guests { get; set; }
        public DbSet<Payment> Payments { get; set; }





        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Define Foreign Key Relationships
            builder.Entity<Flight>()
                .HasOne(f => f.Airline)
                .WithMany()
                .HasForeignKey(f => f.AirlineId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Flight>()
                .HasOne(f => f.DepartureAirport)
                .WithMany()
                .HasForeignKey(f => f.DepartureAirportId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Flight>()
                .HasOne(f => f.ArrivalAirport)
                .WithMany()
                .HasForeignKey(f => f.ArrivalAirportId)
                .OnDelete(DeleteBehavior.Restrict);

            // Correctly configure the relationship between Booking and Guest
            builder.Entity<Booking>()
                .HasMany(b => b.Guests) // Booking has many Guests
                .WithOne(g => g.Booking) // Guest has one Booking
                .HasForeignKey(g => g.BookingId) // Foreign key in Guest
                .OnDelete(DeleteBehavior.Cascade); // Cascade delete

            builder.Entity<Payment>()
        .HasOne(p => p.Booking)
        .WithOne(b => b.Payment)
        .HasForeignKey<Payment>(p => p.BookingId)
        .OnDelete(DeleteBehavior.Cascade);



        }
    }
    }

