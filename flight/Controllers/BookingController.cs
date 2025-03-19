using Microsoft.AspNetCore.Mvc;
using flight.Data;
using flight.Models;
using flight.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;

namespace flight.Controllers
{
    public class BookingController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Users> _userManager;

        public BookingController(AppDbContext context, UserManager<Users> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var airports = await _context.Airports.ToListAsync();
            return View(airports);
        }

        [HttpPost]
        public async Task<IActionResult> ProcessPayment(int bookingId)
        {
            try
            {
                // Validate bookingId
                if (bookingId <= 0)
                {
                    return BadRequest(new { message = "Invalid booking ID." });
                }

                // Fetch the booking from the database
                var booking = await _context.Bookings
                    .Include(b => b.Flight)
                    .Include(b => b.ReturnFlight)
                    .Include(b => b.Payment) // Include Payment to check if it exists
                    .FirstOrDefaultAsync(b => b.Id == bookingId);

                if (booking == null)
                {
                    return NotFound(new { message = "Booking not found." });
                }

                // Check if a payment already exists for this booking
                var payment = booking.Payment;

                if (payment == null)
                {
                    // Create a new payment if it doesn't exist
                    payment = new Payment
                    {
                        BookingId = bookingId,
                        Amount = booking.TotalPrice,
                        PaymentDate = DateTime.UtcNow,
                        Status = "Completed", // Mark as completed since we're simulating success
                        PaymentMethod = "Credit Card", // Ensure this is set to a non-null value
                        TransactionId = Guid.NewGuid().ToString() // Generate a unique transaction ID
                    };

                    // Add the new payment to the database
                    _context.Payments.Add(payment);
                }
                else
                {
                    // Update the existing payment
                    payment.Amount = booking.TotalPrice;
                    payment.PaymentDate = DateTime.UtcNow;
                    payment.Status = "Completed";
                    payment.PaymentMethod = "Credit Card";
                    payment.TransactionId = Guid.NewGuid().ToString();

                    // Mark the payment as updated
                    _context.Payments.Update(payment);
                }

                // Update booking status to confirmed
                booking.Status = "Confirmed";

                // Deduct seats only if the booking is confirmed
                if (booking.Status == "Confirmed")
                {
                    int totalGuests = booking.NumberOfAdults + booking.NumberOfChildren;

                    // Deduct seats for the departure flight
                    var departureFlight = booking.Flight;
                    if (departureFlight != null)
                    {
                        switch (booking.SeatClass)
                        {
                            case "Economy":
                                departureFlight.EconomySeatsAvailable -= totalGuests;
                                break;
                            case "Business":
                                departureFlight.BusinessSeatsAvailable -= totalGuests;
                                break;
                            case "FirstClass":
                                departureFlight.FirstClassSeatsAvailable -= totalGuests;
                                break;
                        }
                        _context.Flights.Update(departureFlight);
                    }

                    // Deduct seats for the return flight if it exists
                    var returnFlight = booking.ReturnFlight;
                    if (returnFlight != null)
                    {
                        switch (booking.SeatClass)
                        {
                            case "Economy":
                                returnFlight.EconomySeatsAvailable -= totalGuests;
                                break;
                            case "Business":
                                returnFlight.BusinessSeatsAvailable -= totalGuests;
                                break;
                            case "FirstClass":
                                returnFlight.FirstClassSeatsAvailable -= totalGuests;
                                break;
                        }
                        _context.Flights.Update(returnFlight);
                    }
                }

                // Save changes to the database
                await _context.SaveChangesAsync();

                // Redirect to the BookingSummary view after successful payment
                return Json(new { success = true, redirectUrl = $"/Booking/BookingSummary/{bookingId}" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ProcessPayment: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred while processing your payment. Please try again." });
            }
        }

        [HttpGet]
        public IActionResult BookingSummary(int id)
        {
            var booking = _context.Bookings
                .Include(b => b.Flight)
                .Include(b => b.Flight.DepartureAirport)
                .Include(b => b.Flight.ArrivalAirport)
                .Include(b => b.ReturnFlight)
                .Include(b => b.ReturnFlight.DepartureAirport)
                .Include(b => b.ReturnFlight.ArrivalAirport)
                .Include(b => b.Guests)
                .Include(b => b.Payment)
                .FirstOrDefault(b => b.Id == id);

            if (booking == null)
            {
                return NotFound("Booking not found.");
            }

            return View(booking);
        }

        [HttpPost]
        public async Task<IActionResult> SearchFlights(int departureAirportId, int arrivalAirportId, DateTime departureDate, string tripType, DateTime? returnDate = null, int numberOfAdults = 1, int numberOfChildren = 0)
        {
            var departureFlights = await _context.Flights
                .Include(f => f.Airline)
                .Include(f => f.DepartureAirport)
                .Include(f => f.ArrivalAirport)
                .Where(f => f.DepartureAirportId == departureAirportId
                         && f.ArrivalAirportId == arrivalAirportId
                         && f.DepartureDateTime.Date == departureDate.Date
                         && f.Status == "Scheduled")
                .ToListAsync();

            List<Flight> returnFlights = null;
            if (tripType == "roundtrip" && returnDate.HasValue)
            {
                returnFlights = await _context.Flights
                    .Include(f => f.Airline)
                    .Include(f => f.DepartureAirport)
                    .Include(f => f.ArrivalAirport)
                    .Where(f => f.DepartureAirportId == arrivalAirportId
                               && f.ArrivalAirportId == departureAirportId
                               && f.DepartureDateTime.Date == returnDate.Value.Date
                               && f.Status == "Scheduled")
                    .ToListAsync();
            }

            var model = new FlightSearchResultViewModel
            {
                DepartureFlights = departureFlights,
                ReturnFlights = returnFlights,
                IsRoundTrip = tripType == "roundtrip",
                NumberOfAdults = numberOfAdults,
                NumberOfChildren = numberOfChildren
            };
            return PartialView("_FlightResults", model);
        }

        [HttpGet]
        public async Task<IActionResult> SelectFlights(int departureFlightId, int returnFlightId, string seatClass, int numberOfAdults, int numberOfChildren)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var departureFlight = _context.Flights
                .Include(f => f.DepartureAirport)
                .Include(f => f.ArrivalAirport)
                .FirstOrDefault(f => f.Id == departureFlightId);

            if (departureFlight == null)
            {
                return NotFound("Departure flight not found.");
            }

            Flight returnFlight = null;
            if (returnFlightId != 0)
            {
                returnFlight = _context.Flights
                    .Include(f => f.DepartureAirport)
                    .Include(f => f.ArrivalAirport)
                    .FirstOrDefault(f => f.Id == returnFlightId);

                if (returnFlight == null)
                {
                    return NotFound("Return flight not found.");
                }
            }

            // Calculate the total price
            decimal departurePrice = GetPriceBySeatClass(departureFlight, seatClass);
            decimal returnPrice = returnFlight != null ? GetPriceBySeatClass(returnFlight, seatClass) : 0;

            decimal adultTotal = (departurePrice + returnPrice) * numberOfAdults;
            decimal childTotal = (departurePrice + returnPrice) * 0.75m * numberOfChildren;
            decimal totalPrice = adultTotal + childTotal;

            // Create a new Booking object and save it to the database
            var booking = new Booking
            {
                FlightId = departureFlightId,
                ReturnFlightId = returnFlightId != 0 ? returnFlightId : (int?)null,
                SeatClass = seatClass,
                NumberOfAdults = numberOfAdults,
                NumberOfChildren = numberOfChildren,
                TotalPrice = totalPrice,
                DepartureDate = departureFlight.DepartureDateTime,
                ReturnDate = returnFlight?.DepartureDateTime,
                Status = "Pending",
                UserId = user.Id // Associate the booking with the current user
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            // Create the FlightSelectionViewModel with the BookingId and UserId
            var model = new FlightSelectionViewModel
            {
                DepartureFlight = departureFlight,
                ReturnFlight = returnFlight,
                SeatClass = seatClass,
                NumberOfAdults = numberOfAdults,
                NumberOfChildren = numberOfChildren,
                BookingId = booking.Id,
                UserId = user.Id // Ensure this is set
            };

            return View("GuestInformation", model);


        }


        [HttpPost]
        public IActionResult Confirmation(FlightSelectionViewModel model, List<Guest> guests)
        {
            if (model == null || guests == null || !guests.Any())
            {
                return BadRequest("Invalid booking data.");
            }

            // Fetch the existing booking from the database
            var booking = _context.Bookings
                .Include(b => b.Flight)
                .Include(b => b.ReturnFlight)
                .Include(b => b.Payment)
                .FirstOrDefault(b => b.Id == model.BookingId);

            if (booking == null)
            {
                return NotFound("Booking not found.");
            }

            // Ensure the UserId is set
            booking.UserId = model.UserId;

            // Update the booking with passenger information
            booking.Guests = guests;

            // Save the updated booking to the database
            _context.Bookings.Update(booking);
            _context.SaveChanges();

            // Pass the booking to the BookingSummary view
            return View("BookingSummary", booking);
        }

        [HttpGet]
        public IActionResult Confirmation(int id)
        {
            var booking = _context.Bookings
                .Include(b => b.Flight)
                .Include(b => b.Flight.DepartureAirport)
                .Include(b => b.Flight.ArrivalAirport)
                .Include(b => b.ReturnFlight)
                .Include(b => b.ReturnFlight.DepartureAirport)
                .Include(b => b.ReturnFlight.ArrivalAirport)
                .Include(b => b.Guests)
                .Include(b => b.Payment)
                .FirstOrDefault(b => b.Id == id);

            if (booking == null)
            {
                return NotFound("Booking not found.");
            }

            return View("BookingSummary", booking);
        }

        private decimal GetPriceBySeatClass(Flight flight, string seatClass)
        {
            return seatClass switch
            {
                "Economy" => flight.EconomyClassPrice,
                "Business" => flight.BusinessClassPrice,
                "FirstClass" => flight.FirstClassPrice,
                _ => 0
            };
        }
        [HttpGet]
        public async Task<IActionResult> Ticket(int id, int? guestIndex = 0, bool isReturn = false)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Fetch the booking with all related data
            var booking = await _context.Bookings
                .Include(b => b.Flight)
                    .ThenInclude(f => f.Airline) // Include Airline for departure flight
                .Include(b => b.Flight.DepartureAirport)
                .Include(b => b.Flight.ArrivalAirport)
                .Include(b => b.ReturnFlight)
                    .ThenInclude(f => f.Airline) // Include Airline for return flight
                .Include(b => b.ReturnFlight.DepartureAirport)
                .Include(b => b.ReturnFlight.ArrivalAirport)
                .Include(b => b.Guests)
                .Include(b => b.Payment)
                .FirstOrDefaultAsync(b => b.Id == id && b.UserId == user.Id);

            if (booking == null)
            {
                // Log the issue
                Console.WriteLine($"Booking not found for ID: {id}");
                return RedirectToAction("History", "Booking");
            }

            // Ensure the booking is confirmed and payment is completed
            if (booking.Status != "Confirmed" || booking.Payment == null || booking.Payment.Status != "Completed")
            {
                // Log the issue
                Console.WriteLine($"Booking not confirmed or payment not completed for ID: {id}");
                return RedirectToAction("History", "Booking");
            }

            // Determine the flight to display
            var flight = isReturn ? booking.ReturnFlight : booking.Flight;

            if (flight == null)
            {
                return NotFound("Flight not found.");
            }

            // Get the current guest
            var guest = booking.Guests.ElementAtOrDefault(guestIndex ?? 0);

            if (guest == null)
            {
                return NotFound("Guest not found.");
            }

            // Pass the flight and guest to the view using ViewBag
            ViewBag.Flight = flight;
            ViewBag.Guest = guest;
            ViewBag.GuestIndex = guestIndex;
            ViewBag.IsReturn = isReturn;
            ViewBag.TotalGuests = booking.Guests.Count;
            ViewBag.BookingId = booking.Id; // Ensure BookingId is set

            // Pass the booking to the view
            return View(booking);
        }

        [HttpPost]
        public async Task<IActionResult> CancelTicket(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Flight)
                .Include(b => b.ReturnFlight)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
            {
                return NotFound();
            }

            int totalGuests = booking.NumberOfAdults + booking.NumberOfChildren;

            // Return seats for the departure flight
            if (booking.Flight != null)
            {
                switch (booking.SeatClass)
                {
                    case "Economy":
                        booking.Flight.EconomySeatsAvailable += totalGuests;
                        break;
                    case "Business":
                        booking.Flight.BusinessSeatsAvailable += totalGuests;
                        break;
                    case "FirstClass":
                        booking.Flight.FirstClassSeatsAvailable += totalGuests;
                        break;
                }
                _context.Flights.Update(booking.Flight);
            }

            // Return seats for the return flight if it exists
            if (booking.ReturnFlight != null)
            {
                switch (booking.SeatClass)
                {
                    case "Economy":
                        booking.ReturnFlight.EconomySeatsAvailable += totalGuests;
                        break;
                    case "Business":
                        booking.ReturnFlight.BusinessSeatsAvailable += totalGuests;
                        break;
                    case "FirstClass":
                        booking.ReturnFlight.FirstClassSeatsAvailable += totalGuests;
                        break;
                }
                _context.Flights.Update(booking.ReturnFlight);
            }

            booking.Status = "Cancelled";
            _context.Bookings.Update(booking);
            await _context.SaveChangesAsync();

            return Ok();
        }

        public async Task<IActionResult> History()
        {
            // Get the current user
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Fetch confirmed bookings for the current user
            var bookings = await _context.Bookings
                .Include(b => b.Flight)
                .Include(b => b.Flight.DepartureAirport)
                .Include(b => b.Flight.ArrivalAirport)
                .Include(b => b.ReturnFlight)
                .Include(b => b.ReturnFlight.DepartureAirport)
                .Include(b => b.ReturnFlight.ArrivalAirport)
                .Include(b => b.Guests)
                .Include(b => b.Payment)
                .Where(b => b.UserId == user.Id && b.Status == "Confirmed")
                .OrderByDescending(b => b.DepartureDate)
                .ToListAsync();

            return View(bookings);
        }
      
        [HttpGet]
        public async Task<IActionResult> FlightStatus(int id)
        {
            // Fetch the departure flight
            var flight = await _context.Flights
                .Include(f => f.DepartureAirport)
                .Include(f => f.ArrivalAirport)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (flight == null)
            {
                return NotFound("Flight not found.");
            }

            // Check if this flight is part of a roundtrip booking
            var booking = await _context.Bookings
                .Include(b => b.ReturnFlight)
                .ThenInclude(f => f.DepartureAirport)
                .Include(b => b.ReturnFlight)
                .ThenInclude(f => f.ArrivalAirport)
                .FirstOrDefaultAsync(b => b.FlightId == id || b.ReturnFlightId == id);

            if (booking != null && booking.ReturnFlightId.HasValue && booking.ReturnFlightId != id)
            {
                // Fetch the return flight details
                var returnFlight = await _context.Flights
                    .Include(f => f.DepartureAirport)
                    .Include(f => f.ArrivalAirport)
                    .FirstOrDefaultAsync(f => f.Id == booking.ReturnFlightId);

                ViewBag.ReturnFlight = returnFlight;
            }

            return View(flight);
        }
    }
}