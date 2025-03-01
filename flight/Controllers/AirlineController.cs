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
                .OrderByDescending(a => a.DateAdded)
                .AsNoTracking()
                .ToList();

            return View("Airlines", airlines);
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

                airline.DateAdded = DateTime.Now;

                _context.Airlines.Add(airline);
                await _context.SaveChangesAsync();

                return RedirectToAction("Index");
            }

            return View("Airlines", _context.Airlines.ToList());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Airline airline, IFormFile LogoFile)
        {
            if (ModelState.IsValid)
            {
                var existingAirline = await _context.Airlines.FindAsync(airline.Id);
                if (existingAirline == null)
                {
                    return NotFound();
                }

                // If a new logo is uploaded, update it, otherwise retain the existing logo
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

                    existingAirline.LogoPath = "/images/" + fileName;
                }

                existingAirline.Name = airline.Name;
                existingAirline.Status = airline.Status;

                _context.Airlines.Update(existingAirline);
                await _context.SaveChangesAsync();

                return RedirectToAction("Index");
            }

            return View("Airlines", _context.Airlines.ToList());
        }
    }
}
