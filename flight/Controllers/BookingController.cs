using flight.Data;
using flight.Models;
using flight.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace flight.Controllers
{
    public class BookingController : Controller
    {
        private readonly AppDbContext _context;

        public BookingController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var airports = await _context.Airports.ToListAsync();
            return View(airports);
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

            var model = new FlightSelectionViewModel
            {
                DepartureFlight = departureFlight,
                ReturnFlight = returnFlight,
                SeatClass = seatClass,
                NumberOfAdults = numberOfAdults,
                NumberOfChildren = numberOfChildren
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

            // Fetch complete flight objects since they might not be fully populated from the form
            var departureFlight = _context.Flights
                .Include(f => f.DepartureAirport)
                .Include(f => f.ArrivalAirport)
                .FirstOrDefault(f => f.Id == model.DepartureFlight.Id);

            Flight returnFlight = null;
            if (model.ReturnFlight != null && model.ReturnFlight.Id > 0)
            {
                returnFlight = _context.Flights
                    .Include(f => f.DepartureAirport)
                    .Include(f => f.ArrivalAirport)
                    .FirstOrDefault(f => f.Id == model.ReturnFlight.Id);
            }

            // Update model with complete flight objects
            model.DepartureFlight = departureFlight;
            model.ReturnFlight = returnFlight;

            // If UserId is not set, set a default or use session-based ID
            if (string.IsNullOrEmpty(model.UserId))
            {
                model.UserId = "guest-" + Guid.NewGuid().ToString(); // For guest bookings
            }

            // Validate model again after updating missing fields
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors);
                foreach (var error in errors)
                {
                    Console.WriteLine($"ModelState Error: {error.ErrorMessage}");
                }
                return View("GuestInformation", model);
            }

            // Calculate the total price directly here instead of using a separate method
            decimal departurePrice = GetPriceBySeatClass(model.DepartureFlight, model.SeatClass);
            decimal returnPrice = model.ReturnFlight != null ? GetPriceBySeatClass(model.ReturnFlight, model.SeatClass) : 0;

            decimal totalPrice = (model.NumberOfAdults * (departurePrice + returnPrice)) +
                                 (model.NumberOfChildren * (departurePrice + returnPrice) * 0.75m);

            // Create a new Booking object
            var booking = new Booking
            {
                FlightId = model.DepartureFlight.Id,
                ReturnFlightId = model.ReturnFlight?.Id,
                SeatClass = model.SeatClass,
                NumberOfAdults = model.NumberOfAdults,
                NumberOfChildren = model.NumberOfChildren,
                TotalPrice = totalPrice,
                DepartureDate = model.DepartureFlight.DepartureDateTime,
                ReturnDate = model.ReturnFlight?.DepartureDateTime,
                Status = "Pending",
                Guests = guests,
                UserId = model.UserId
            };

            // Save the booking to the database
            _context.Bookings.Add(booking);
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