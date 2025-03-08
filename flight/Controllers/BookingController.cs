using flight.Data;
using flight.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
            ViewBag.Airports = await _context.Airports.ToListAsync();
            return View("~/Views/Users/Index.cshtml");
        }

        [HttpPost]
        public async Task<IActionResult> Search(int from, int to, DateTime departureDate, DateTime? returnDate, string tripType)
        {
            var flights = await _context.Flights
                .Include(f => f.Airline)
                .Include(f => f.DepartureAirport)
                .Include(f => f.ArrivalAirport)
                .Where(f => f.DepartureAirportId == from
                            && f.ArrivalAirportId == to
                            && f.DepartureDateTime.Date == departureDate.Date
                            && f.Status == "Scheduled")
                .ToListAsync();

            if (tripType == "RoundTrip" && returnDate.HasValue)
            {
                var returnFlights = await _context.Flights
                    .Include(f => f.Airline)
                    .Include(f => f.DepartureAirport)
                    .Include(f => f.ArrivalAirport)
                    .Where(f => f.DepartureAirportId == to
                                && f.ArrivalAirportId == from
                                && f.DepartureDateTime.Date == returnDate.Value.Date
                                && f.Status == "Scheduled")
                    .ToListAsync();

                ViewBag.ReturnFlights = returnFlights;
            }

            ViewBag.TripType = tripType;
            ViewBag.DepartureDate = departureDate;
            ViewBag.ReturnDate = returnDate;

            return View("~/Views/Users/Result.cshtml", flights);
        }

        public async Task<IActionResult> Book(int flightId, string seatType, int numberOfAdults, int numberOfChildren, int? returnFlightId)
        {
            var flight = await _context.Flights
                .Include(f => f.DepartureAirport)
                .Include(f => f.ArrivalAirport)
                .FirstOrDefaultAsync(f => f.Id == flightId);

            if (flight == null)
            {
                return NotFound();
            }

            var booking = new Booking
            {
                FlightId = flightId,
                SeatType = seatType,
                NumberOfAdults = numberOfAdults,
                NumberOfChildren = numberOfChildren
            };

            booking.CalculateTotalPrice();

            if (returnFlightId.HasValue)
            {
                var returnFlight = await _context.Flights
                    .Include(f => f.DepartureAirport)
                    .Include(f => f.ArrivalAirport)
                    .FirstOrDefaultAsync(f => f.Id == returnFlightId.Value);

                if (returnFlight != null)
                {
                    var returnBooking = new Booking
                    {
                        FlightId = returnFlightId.Value,
                        SeatType = seatType,
                        NumberOfAdults = numberOfAdults,
                        NumberOfChildren = numberOfChildren
                    };

                    returnBooking.CalculateTotalPrice();
                    booking.TotalPrice += returnBooking.TotalPrice;
                }
            }

            return View("BookingSummary", booking);
        }
    }
}