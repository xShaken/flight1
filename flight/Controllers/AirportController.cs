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

            return View("Airports", airports);

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
                return RedirectToAction(nameof(Index));
            }
            return View("~/Views/Airport/Airport.cshtml", airport);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Airport airport)
        {
            if (id != airport.Id)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(airport);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Airports.Any(a => a.Id == airport.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            return View("~/Views/Airport/Airport.cshtml", airport);
        }
    }
}
