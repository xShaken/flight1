using flight.Data;
using flight.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace flight.Controllers
{
    public class AirportController : Controller
    {
        private readonly AppDbContext _context;

        public AirportController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var airports = _context.Airports
                .OrderByDescending(a => a.DateAdded)
                .AsNoTracking()
                .ToList();

            return View("~/Views/Home/Airports.cshtml", airports);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Airport airport)
        {
            if (ModelState.IsValid)
            {
                airport.DateAdded = DateTime.Now;
                _context.Airports.Add(airport);
                await _context.SaveChangesAsync();

                return RedirectToAction("Airports", "Home");
            }

            return View("~/Views/Home/Airports.cshtml", airport);
        }
    }
}
