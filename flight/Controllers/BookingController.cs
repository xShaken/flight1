using Microsoft.AspNetCore.Mvc;
using flight.Data;
using flight.Models;
using flight.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;

namespace flight.Controllers
{
    public class BookingController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Users> _userManager;

        public BookingController(AppDbContext context, UserManager<Users> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var airports = await _context.Airports.ToListAsync();
            return View(airports);
        }

        [HttpPost]
        public async Task<IActionResult> ProcessPayment(int bookingId)
        {
            try
            {
                // Validate bookingId
                if (bookingId <= 0)
                {
                    return BadRequest(new { message = "Invalid booking ID." });
                }

                // Fetch the booking from the database
                var booking = await _context.Bookings
                    .Include(b => b.Flight)
                    .Include(b => b.Payment) // Include Payment to check if it exists
                    .FirstOrDefaultAsync(b => b.Id == bookingId);

                if (booking == null)
                {
                    return NotFound(new { message = "Booking not found." });
                }

                // Check if a payment already exists for this booking
                var payment = booking.Payment;

                if (payment == null)
                {
                    // Create a new payment if it doesn't exist
                    payment = new Payment
                    {
                        BookingId = bookingId,
                        Amount = booking.TotalPrice,
                        PaymentDate = DateTime.UtcNow,
                        Status = "Completed", // Mark as completed since we're simulating success
                        PaymentMethod = "Credit Card", // Ensure this is set to a non-null value
                        TransactionId = Guid.NewGuid().ToString() // Generate a unique transaction ID
                    };

                    // Add the new payment to the database
                    _context.Payments.Add(payment);
                }
                else
                {
                    // Update the existing payment
                    payment.Amount = booking.TotalPrice;
                    payment.PaymentDate = DateTime.UtcNow;
                    payment.Status = "Completed";
                    payment.PaymentMethod = "Credit Card";
                    payment.TransactionId = Guid.NewGuid().ToString();

                    // Mark the payment as updated
                    _context.Payments.Update(payment);
                }

                // Update booking status to confirmed
                booking.Status = "Confirmed";

                // Save changes to the database
                await _context.SaveChangesAsync();

                // Return success
                return Json(new { success = true, redirectUrl = $"/Booking/Confirmation/{bookingId}" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ProcessPayment: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while processing your payment. Please try again." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SearchFlights(int departureAirportId, int arrivalAirportId, DateTime departureDate, string tripType, DateTime? returnDate = null, int numberOfAdults = 1, int numberOfChildren = 0)
        {
            var departureFlights = await _context.Flights
                .Include(f => f.Airline)
                .Include(f => f.DepartureAirport)
                .Include(f => f.ArrivalAirport)
                .Where(f => f.DepartureAirportId == departureAirportId
                         && f.ArrivalAirportId == arrivalAirportId
                         && f.DepartureDateTime.Date == departureDate.Date
                         && f.Status == "Scheduled")
                .ToListAsync();

            List<Flight> returnFlights = null;
            if (tripType == "roundtrip" && returnDate.HasValue)
            {
                returnFlights = await _context.Flights
                    .Include(f => f.Airline)
                    .Include(f => f.DepartureAirport)
                    .Include(f => f.ArrivalAirport)
                    .Where(f => f.DepartureAirportId == arrivalAirportId
                               && f.ArrivalAirportId == departureAirportId
                               && f.DepartureDateTime.Date == returnDate.Value.Date
                               && f.Status == "Scheduled")
                    .ToListAsync();
            }

            var model = new FlightSearchResultViewModel
            {
                DepartureFlights = departureFlights,
                ReturnFlights = returnFlights,
                IsRoundTrip = tripType == "roundtrip",
                NumberOfAdults = numberOfAdults,
                NumberOfChildren = numberOfChildren
            };
            return PartialView("_FlightResults", model);
        }

        [HttpGet]
        public async Task<IActionResult> SelectFlights(int departureFlightId, int returnFlightId, string seatClass, int numberOfAdults, int numberOfChildren)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var departureFlight = _context.Flights
                .Include(f => f.DepartureAirport)
                .Include(f => f.ArrivalAirport)
                .FirstOrDefault(f => f.Id == departureFlightId);

            if (departureFlight == null)
            {
                return NotFound("Departure flight not found.");
            }

            Flight returnFlight = null;
            if (returnFlightId != 0)
            {
                returnFlight = _context.Flights
                    .Include(f => f.DepartureAirport)
                    .Include(f => f.ArrivalAirport)
                    .FirstOrDefault(f => f.Id == returnFlightId);

                if (returnFlight == null)
                {
                    return NotFound("Return flight not found.");
                }
            }

            // Calculate the total price
            decimal departurePrice = GetPriceBySeatClass(departureFlight, seatClass);
            decimal returnPrice = returnFlight != null ? GetPriceBySeatClass(returnFlight, seatClass) : 0;

            decimal adultTotal = (departurePrice + returnPrice) * numberOfAdults;
            decimal childTotal = (departurePrice + returnPrice) * 0.75m * numberOfChildren;
            decimal totalPrice = adultTotal + childTotal;

            // Create a new Booking object and save it to the database
            var booking = new Booking
            {
                FlightId = departureFlightId,
                ReturnFlightId = returnFlightId != 0 ? returnFlightId : (int?)null,
                SeatClass = seatClass,
                NumberOfAdults = numberOfAdults,
                NumberOfChildren = numberOfChildren,
                TotalPrice = totalPrice,
                DepartureDate = departureFlight.DepartureDateTime,
                ReturnDate = returnFlight?.DepartureDateTime,
                Status = "Pending",
                UserId = user.Id // Associate the booking with the current user
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            // Create the FlightSelectionViewModel with the BookingId and UserId
            var model = new FlightSelectionViewModel
            {
                DepartureFlight = departureFlight,
                ReturnFlight = returnFlight,
                SeatClass = seatClass,
                NumberOfAdults = numberOfAdults,
                NumberOfChildren = numberOfChildren,
                BookingId = booking.Id,
                UserId = user.Id // Ensure this is set
            };

            return View("GuestInformation", model);
        }

        [HttpPost]
        public IActionResult Confirmation(FlightSelectionViewModel model, List<Guest> guests)
        {
            if (model == null || guests == null || !guests.Any())
            {
                return BadRequest("Invalid booking data.");
            }

            // Fetch the existing booking from the database
            var booking = _context.Bookings
                .Include(b => b.Flight)
                .Include(b => b.ReturnFlight)
                .Include(b => b.Payment)
                .FirstOrDefault(b => b.Id == model.BookingId);

            if (booking == null)
            {
                return NotFound("Booking not found.");
            }

            // Ensure the UserId is set
            booking.UserId = model.UserId;

            // Update the booking with passenger information
            booking.Guests = guests;

            // Save the updated booking to the database
            _context.Bookings.Update(booking);
            _context.SaveChanges();

            // Pass the booking to the BookingSummary view
            return View("BookingSummary", booking);
        }

        [HttpGet]
        public IActionResult Confirmation(int id)
        {
            var booking = _context.Bookings
                .Include(b => b.Flight)
                .Include(b => b.Flight.DepartureAirport)
                .Include(b => b.Flight.ArrivalAirport)
                .Include(b => b.ReturnFlight)
                .Include(b => b.ReturnFlight.DepartureAirport)
                .Include(b => b.ReturnFlight.ArrivalAirport)
                .Include(b => b.Guests)
                .Include(b => b.Payment)
                .FirstOrDefault(b => b.Id == id);

            if (booking == null)
            {
                return NotFound("Booking not found.");
            }

            return View("BookingSummary", booking);
        }

        private decimal GetPriceBySeatClass(Flight flight, string seatClass)
        {
            return seatClass switch
            {
                "Economy" => flight.EconomyClassPrice,
                "Business" => flight.BusinessClassPrice,
                "FirstClass" => flight.FirstClassPrice,
                _ => 0
            };
        }

        public async Task<IActionResult> History()
        {
            // Get the current user
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Fetch bookings for the current user, including Payment
            var bookings = await _context.Bookings
                .Include(b => b.Flight)
                .Include(b => b.Flight.DepartureAirport)
                .Include(b => b.Flight.ArrivalAirport)
                .Include(b => b.ReturnFlight)
                .Include(b => b.ReturnFlight.DepartureAirport)
                .Include(b => b.ReturnFlight.ArrivalAirport)
                .Include(b => b.Guests)
                .Include(b => b.Payment) // Ensure Payment is included
                .Where(b => b.UserId == user.Id)
                .OrderByDescending(b => b.DepartureDate)
                .ToListAsync();

            return View(bookings);
        }
    }
}