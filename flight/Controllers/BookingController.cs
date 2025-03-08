using flight.Data;
using flight.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
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
        public async Task<IActionResult> SearchFlights(int departureAirportId, int arrivalAirportId, DateTime departureDate)
        {
            var flights = await _context.Flights
                .Include(f => f.Airline)
                .Include(f => f.DepartureAirport)
                .Include(f => f.ArrivalAirport)
                .Where(f => f.DepartureAirportId == departureAirportId
                         && f.ArrivalAirportId == arrivalAirportId
                         && f.DepartureDateTime.Date == departureDate.Date
                         && f.Status == "Scheduled")
                .ToListAsync();

            return PartialView("_FlightResults", flights);
        }
    }
}