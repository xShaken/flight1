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

        public IActionResult Dashboard()
        {
            return View();
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
