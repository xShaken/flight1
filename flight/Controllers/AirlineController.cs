using flight.Data;
using flight.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace flight.Controllers
{
    public class AirlineController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AirlineController(AppDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        public IActionResult Index()
        {
            var airlines = _context.Airlines
                .OrderByDescending(a => a.DateAdded) // Show newest airlines first
                .AsNoTracking() // Ensures the latest data is fetched
                .ToList();

            if (!airlines.Any())
            {
                return View("~/Views/Home/Airlines.cshtml", new List<Airline>());
            }

            return View("~/Views/Home/Airlines.cshtml", airlines);
        }







        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Airline airline, IFormFile LogoFile)
        {
            if (ModelState.IsValid)
            {
                if (LogoFile != null && LogoFile.Length > 0)
                {
                    string fileName = Path.GetFileName(LogoFile.FileName);
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images");

                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    string filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await LogoFile.CopyToAsync(stream);
                    }

                    airline.LogoPath = "/images/" + fileName;
                }

                if (airline.DateAdded == default)
                {
                    airline.DateAdded = DateTime.Now; // Only set if it's a new entry
                }

                _context.Airlines.Add(airline);
                await _context.SaveChangesAsync();

                // ✅ Correct RedirectToAction Syntax
                return RedirectToAction("Airlines", "Home");
            }

            // If model state is invalid, return to the same page
            return View(airline);
        }




    }
}
