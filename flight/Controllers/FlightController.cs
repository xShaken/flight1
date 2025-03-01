using flight.Data;
using flight.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace flight.Controllers
{
    public class FlightController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<FlightController> _logger;

        public FlightController(AppDbContext context, ILogger<FlightController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Show the list of flights
        public async Task<IActionResult> Index()
        {
            var flights = await _context.Flights
                .Include(f => f.Airline)
                .Include(f => f.DepartureAirport)
                .Include(f => f.ArrivalAirport)
                .ToListAsync();

            return View(flights);
        }

        // Show the create flight form
        public async Task<IActionResult> Create()
        {
            ViewBag.Airlines = new SelectList(_context.Airlines, "Id", "Name");
            ViewBag.Airports = new SelectList(_context.Airports, "Id", "Name");
           
            await PopulateDropdowns();
            return View();
        }

        // Handle flight creation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FlightNumber, AirlineId, DepartureAirportId, ArrivalAirportId, DepartureTime, ArrivalTime, Status")] Flight flight)
        {
            if (flight.DepartureTime >= flight.ArrivalTime)
            {
                ModelState.AddModelError("ArrivalTime", "Arrival time must be after departure time.");
            }

            if (!ModelState.IsValid)
            {
                _logger.LogError("Validation failed: {Errors}",
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));

                await PopulateDropdowns(); // ✅ Fix: Ensure dropdowns are populated before returning View.
                return View(flight);
            }

            flight.DateAdded = DateTime.UtcNow;

            try
            {
                await _context.Flights.AddAsync(flight);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Flight {FlightNumber} saved successfully!", flight.FlightNumber);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error saving flight: {Exception}", ex.ToString());
                ModelState.AddModelError("", "An error occurred while saving the flight.");
            }

            await PopulateDropdowns(); // ✅ Fix: Ensure dropdowns are populated before returning View.
            return View(flight);
        }


        // Populate Airlines & Airports dropdowns
        private async Task PopulateDropdowns()
        {
            ViewBag.Airlines = new SelectList(await _context.Airlines.ToListAsync(), "Id", "Name");
            ViewBag.Airports = new SelectList(await _context.Airports.ToListAsync(), "Id", "Name");
        }

    }
}
