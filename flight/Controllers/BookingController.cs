using flight.Data;
using flight.Models;
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

        [HttpGet]
        public IActionResult ConfirmBooking(FlightSelectionViewModel model, List<Guest> guests)
        {
            decimal totalPrice = CalculateTotalPrice(model, guests);
            var booking = new Booking
            {
                FlightId = model.DepartureFlight.Id,
                ReturnFlightId = model.ReturnFlight.Id,
                SeatClass = model.SeatClass,
                NumberOfAdults = model.NumberOfAdults,
                NumberOfChildren = model.NumberOfChildren,
                TotalPrice = totalPrice,
                DepartureDate = model.DepartureFlight.DepartureDateTime,
                ReturnDate = model.ReturnFlight.DepartureDateTime,
                Status = "Pending",
                Guests = guests
            };

            _context.Bookings.Add(booking);
            _context.SaveChanges();

            return View("BookingSummary", booking);
        }

        private decimal CalculateTotalPrice(FlightSelectionViewModel model, List<Guest> guests)
        {
            decimal departurePrice = GetPriceBySeatClass(model.DepartureFlight, model.SeatClass);
            decimal returnPrice = GetPriceBySeatClass(model.ReturnFlight, model.SeatClass);

            decimal totalPrice = (model.NumberOfAdults * (departurePrice + returnPrice)) +
                                (model.NumberOfChildren * (departurePrice + returnPrice) * 0.75m);

            return totalPrice;
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