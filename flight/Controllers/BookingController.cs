using Microsoft.AspNetCore.Mvc;
using flight.Data;
using flight.Models;
using flight.Services;
using flight.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace flight.Controllers
{
    public class BookingController : Controller
    {
        private readonly AppDbContext _context;
        private readonly PayMongoService _payMongoService;
        private readonly UserManager<Users> _userManager;

        public BookingController(AppDbContext context, PayMongoService payMongoService, UserManager<Users> userManager)
        {
            _context = context;
            _payMongoService = payMongoService;
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
                // Log the incoming bookingId
                Console.WriteLine($"Processing payment for bookingId: {bookingId}");

                // Validate bookingId
                if (bookingId <= 0)
                {
                    Console.WriteLine("Invalid booking ID.");
                    return BadRequest(new { message = "Invalid booking ID." });
                }

                // Fetch the booking from the database
                var booking = await _context.Bookings
                    .Include(b => b.Flight)
                    .FirstOrDefaultAsync(b => b.Id == bookingId);

                if (booking == null)
                {
                    Console.WriteLine("Booking not found.");
                    return NotFound(new { message = "Booking not found." });
                }

                // Log the booking details
                Console.WriteLine($"Booking found: Id={booking.Id}, TotalPrice={booking.TotalPrice}");

                // Create a payment intent
                var totalAmount = booking.TotalPrice;
                var paymentIntentId = await _payMongoService.CreatePaymentIntent(totalAmount);

                if (string.IsNullOrEmpty(paymentIntentId))
                {
                    Console.WriteLine("Failed to create payment intent.");
                    return BadRequest(new { message = "Failed to create payment intent." });
                }

                // Log the payment intent ID
                Console.WriteLine($"Payment intent created: {paymentIntentId}");

                // Save the payment to the database
                var payment = new Payment
                {
                    BookingId = bookingId,
                    PaymentIntentId = paymentIntentId,
                    Amount = totalAmount,
                    Status = "Pending"
                };

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                // Log the successful payment
                Console.WriteLine("Payment saved to the database.");

                // Return the payment intent ID to the client
                return Json(new { paymentIntentId });
            }
            catch (Exception ex)
            {
                // Log the exception
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
                IsRoundTrip = tripType == "roundtrip", // Ensure this is set correctly
                NumberOfAdults = numberOfAdults,
                NumberOfChildren = numberOfChildren
            };
            return PartialView("_FlightResults", model);
        }

        [HttpGet]
        public IActionResult SelectFlights(int departureFlightId, int returnFlightId, string seatClass, int numberOfAdults, int numberOfChildren)
        {
            var departureFlight = _context.Flights
                .Include(f => f.DepartureAirport)
                .Include(f => f.ArrivalAirport)
                .FirstOrDefault(f => f.Id == departureFlightId);

            if (departureFlight == null)
            {
                return NotFound("Departure flight not found.");
            }

            Flight returnFlight = null;
            if (returnFlightId != 0) // Check if return flight is selected
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
                ReturnFlightId = returnFlightId != 0 ? returnFlightId : (int?)null, // Set to null if no return flight
                SeatClass = seatClass,
                NumberOfAdults = numberOfAdults,
                NumberOfChildren = numberOfChildren,
                TotalPrice = totalPrice, // Set the calculated total price
                DepartureDate = departureFlight.DepartureDateTime,
                ReturnDate = returnFlight?.DepartureDateTime,
                Status = "Pending",
                UserId = null // Allow null for guest bookings
            };

            _context.Bookings.Add(booking);
            _context.SaveChanges();

            // Create the FlightSelectionViewModel with the BookingId
            var model = new FlightSelectionViewModel
            {
                DepartureFlight = departureFlight,
                ReturnFlight = returnFlight,
                SeatClass = seatClass,
                NumberOfAdults = numberOfAdults,
                NumberOfChildren = numberOfChildren,
                BookingId = booking.Id // Set the BookingId
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
                .FirstOrDefault(b => b.Id == model.BookingId);

            if (booking == null)
            {
                return NotFound("Booking not found.");
            }

            // Update the booking with passenger information
            booking.Guests = guests;

            // Calculate the total price
            decimal departurePrice = GetPriceBySeatClass(booking.Flight, model.SeatClass);
            decimal returnPrice = booking.ReturnFlight != null ? GetPriceBySeatClass(booking.ReturnFlight, model.SeatClass) : 0;

            booking.TotalPrice = (model.NumberOfAdults * (departurePrice + returnPrice)) +
                                 (model.NumberOfChildren * (departurePrice + returnPrice) * 0.75m);

            // Save the updated booking to the database
            _context.Bookings.Update(booking);
            _context.SaveChanges();

            // Pass the booking to the BookingSummary view
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
    }
}