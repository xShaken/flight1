using flight.Data;
using flight.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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

            // If there's logic to resolve the issue, put it here.

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

            //for users (INDEX)
            [Authorize(Roles = "User")]
            public async Task<IActionResult> User()
            {
                var airports = await _context.Airports.ToListAsync();

                ViewBag.DepartureAirports = airports
                    .Select(a => new SelectListItem { Value = a.Id.ToString(), Text = a.Name })
                    .ToList();

                ViewBag.ArrivalAirports = airports
                    .Select(a => new SelectListItem { Value = a.Id.ToString(), Text = a.Name })
                    .ToList();

                return View("~/Views/Users/Index.cshtml");
            }

        }
    }


