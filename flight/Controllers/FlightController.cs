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

            ViewBag.Airlines = new SelectList(await _context.Airlines.ToListAsync(), "Id", "Name");
            ViewBag.Airports = new SelectList(await _context.Airports.ToListAsync(), "Id", "Name");

            return View(flights);
        }

        // Show the create flight form
        public async Task<IActionResult> Create()
        {
            await PopulateDropdowns();
            return View();
        }

        // Handle flight creation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FlightNumber, AirlineId, AircraftCode, DepartureAirportId, ArrivalAirportId, DepartureDateTime, EstimatedArrivalDateTime, BusinessSeatsAvailable, BusinessClassPrice, EconomySeatsAvailable, EconomyClassPrice, FirstClassSeatsAvailable, FirstClassPrice, Status")] Flight flight)
        {
            if (flight.DepartureDateTime >= flight.EstimatedArrivalDateTime)
            {
                ModelState.AddModelError("EstimatedArrivalDateTime", "Arrival time must be after departure time.");
            }

            if (!ModelState.IsValid)
            {
                _logger.LogError("Validation failed: {Errors}",
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));

                await PopulateDropdowns();
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

            await PopulateDropdowns();
            return View(flight);
        }

        // Show the edit flight modal (GET request - optional, but useful for debugging)
        public async Task<IActionResult> Edit(int id)
        {
            var flight = await _context.Flights.FindAsync(id);
            if (flight == null)
            {
                return NotFound();
            }

            await PopulateDropdowns();
            return View(flight);
        }

        // Handle flight editing
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id, FlightNumber, AirlineId, AircraftCode, DepartureAirportId, ArrivalAirportId, DepartureDateTime, EstimatedArrivalDateTime, BusinessSeatsAvailable, BusinessClassPrice, EconomySeatsAvailable, EconomyClassPrice, FirstClassSeatsAvailable, FirstClassPrice, Status")] Flight flight)
        {
            if (id != flight.Id)
            {
                return BadRequest();
            }

            if (flight.DepartureDateTime >= flight.EstimatedArrivalDateTime)
            {
                ModelState.AddModelError("EstimatedArrivalDateTime", "Arrival time must be after departure time.");
            }

            if (!ModelState.IsValid)
            {
                _logger.LogError("Validation failed during edit: {Errors}",
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));

                await PopulateDropdowns();
                return RedirectToAction("Index");
            }

            try
            {
                var existingFlight = await _context.Flights.FindAsync(flight.Id);
                if (existingFlight == null)
                {
                    return NotFound();
                }

                // Update properties
                existingFlight.FlightNumber = flight.FlightNumber;
                existingFlight.AirlineId = flight.AirlineId;
                existingFlight.AircraftCode = flight.AircraftCode;
                existingFlight.DepartureAirportId = flight.DepartureAirportId;
                existingFlight.ArrivalAirportId = flight.ArrivalAirportId;
                existingFlight.DepartureDateTime = flight.DepartureDateTime;
                existingFlight.EstimatedArrivalDateTime = flight.EstimatedArrivalDateTime;
                existingFlight.BusinessSeatsAvailable = flight.BusinessSeatsAvailable;
                existingFlight.BusinessClassPrice = flight.BusinessClassPrice;
                existingFlight.EconomySeatsAvailable = flight.EconomySeatsAvailable;
                existingFlight.EconomyClassPrice = flight.EconomyClassPrice;
                existingFlight.FirstClassSeatsAvailable = flight.FirstClassSeatsAvailable;
                existingFlight.FirstClassPrice = flight.FirstClassPrice;
                existingFlight.Status = flight.Status;

                _context.Update(existingFlight);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Flight {FlightNumber} updated successfully!", flight.FlightNumber);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Flights.Any(f => f.Id == flight.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToAction("Index");
        }
     
        // Populate Airlines & Airports dropdowns
        private async Task PopulateDropdowns()
        {
            ViewBag.Airlines = new SelectList(await _context.Airlines.ToListAsync(), "Id", "Name");
            ViewBag.Airports = new SelectList(await _context.Airports.ToListAsync(), "Id", "Name");
        }
    }
}
