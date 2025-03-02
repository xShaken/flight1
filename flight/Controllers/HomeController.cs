using flight.Data;
using flight.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace flight.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;  // ✅ Use AppDbContext
        private readonly ILogger<HomeController> _logger;

       
        public HomeController(AppDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Landingpage()
        {
            return View();
        }
        [Authorize]
        public IActionResult Privacy()
        {
            return View();
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Admin()
        {
            return View();
        }

        public async Task<IActionResult> Dashboard()
        {
            var flights = await _context.Flights
                .Include(f => f.Airline)
                .Include(f => f.DepartureAirport)
                .Include(f => f.ArrivalAirport)
                .ToListAsync();

            var airlinesCount = await _context.Airlines.CountAsync(); // Get total airlines

            ViewBag.AirlinesCount = airlinesCount; // Pass to the view

            return View(flights);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, int? delayMinutes, string status)
        {
            var flight = await _context.Flights.FindAsync(id);
            if (flight == null)
            {
                return NotFound();
            }

            if (status == "Delayed" && delayMinutes.HasValue)
            {
                // Update both departure and arrival times if delayed
                flight.DepartureDateTime = flight.DepartureDateTime.AddMinutes(delayMinutes.Value);
                flight.EstimatedArrivalDateTime = flight.EstimatedArrivalDateTime.AddMinutes(delayMinutes.Value);
            }
            else if (status == "Departed")
            {
                // Update DepartureDateTime to the current time when marked as Departed
                flight.DepartureDateTime = DateTime.Now;
            }

            flight.Status = status;
            _context.Update(flight);
            await _context.SaveChangesAsync();

            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public async Task<IActionResult> ResolveIssue(int id)
        {
            var flight = await _context.Flights.FindAsync(id);
            if (flight == null)
            {
                return NotFound();
            }

            flight.Status = "Scheduled";
            _context.Update(flight);
            await _context.SaveChangesAsync();

            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsArrived(int id)
        {
            var flight = await _context.Flights.FindAsync(id);
            if (flight == null)
            {
                return NotFound();
            }

            // Update EstimatedArrivalDateTime to the current time when marked as Arrived
            flight.EstimatedArrivalDateTime = DateTime.Now;
            flight.Status = "Arrived";
            _context.Update(flight);
            await _context.SaveChangesAsync();

            return RedirectToAction("Dashboard");
        }



        public IActionResult Flights()
        {
            return View();
        }

        public IActionResult Airlines()
        {
            var airlines = _context.Airlines.ToList(); // ✅ Fetch airlines correctly
            return View(airlines);
        }

        public IActionResult Airports()
        {
            var airports = _context.Airports.ToList();
            return View(airports);
        }



        [Authorize(Roles = "User")]
        public IActionResult User()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
