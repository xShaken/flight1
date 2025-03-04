using flight.Data;
using flight.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace flight.Controllers
{
    [Authorize] // Only authenticated users can access booking
    public class BookingsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Users> _userManager;

        public BookingsController(AppDbContext context, UserManager<Users> userManager)
        {
            _context = context;
            _userManager = userManager;
        }


        public async Task<IActionResult> Index()
        {
            ViewBag.DepartureAirports = await _context.Airports
                .Select(a => new { Value = a.Id, Text = a.Name })
                .ToListAsync();

            ViewBag.ArrivalAirports = ViewBag.DepartureAirports; // Assuming same list for both

            return View("~/Views/Users/Index.cshtml");
        }


        // Display Booking Form
        public IActionResult Create()
        {
            return View();
        }

        // Handle Booking Submission
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BookFlight(Booking booking)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);

                if (user != null)
                {
                    booking.UserId = user.Id;
                    booking.TotalPrice = booking.NumberOfSeats * 1500; // Example price calculation
                    booking.BookingDate = DateTime.Now;

                    _context.Bookings.Add(booking);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Flight Booked Successfully!";
                    return RedirectToAction("UserBookings");
                }
            }
            TempData["Error"] = "Failed to book flight!";
            return View("Create", booking);
        }

        // Show List of User Bookings
        public async Task<IActionResult> UserBookings()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user != null)
            {
                var bookings = await _context.Bookings
                    .Where(b => b.UserId == user.Id)
                    .ToListAsync();

                return View(bookings);
            }

            return RedirectToAction("Login", "Account");
        }
    }
}
