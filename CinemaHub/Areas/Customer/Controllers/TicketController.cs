using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using CinemaHub.DataAccess.Data;
using CinemaHub.DataAccess.Repositories;
using CinemaHub.Models;
using CinemaHub.Models.ViewModels;
using System;
using System.Linq;
using System.Security.Claims;
using System.Text;
using CinemaHub.Services.IServices;

namespace CinemaHub.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize(Roles = "customer,admin,cinemaManager")]
    public class TicketController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<AppUser> _userManager;
        public TicketVM TicketVM;
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly IUnlockASeatService _unlockASeatService;
        private readonly ITicketService _ticketService;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly IEmailSender _emailSender;
        public TicketController(IUnitOfWork unitOfWork,
                                IDbContextFactory<AppDbContext> dbContextFactory,
                                IUnlockASeatService unlockASeatService,
                                ITicketService ticketService,
                                IBackgroundJobClient backgroundJobClient,
                                UserManager<AppUser> userManager,
                                IEmailSender emailSender)
        {
            _unlockASeatService = unlockASeatService;
            _ticketService = ticketService;
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _dbContextFactory = dbContextFactory;
            _backgroundJobClient = backgroundJobClient;
            _emailSender = emailSender;
        }
        public async Task<IActionResult> Index(Guid? movie_id = null)
        {
            // Get all movies and order by showtimes for each of them
            IEnumerable<Movie> movieList = await _unitOfWork.Movie.GetAllAsync();
            var showtimes = await _unitOfWork.Showtime.GetAllAsync();
            var movieShowtimeCounts = showtimes.GroupBy(s => s.MovieID)
                                               .ToDictionary(g => g.Key, g => g.Count());
            movieList = movieList.OrderByDescending(m => movieShowtimeCounts.ContainsKey(m.MovieID) ? movieShowtimeCounts[m.MovieID] : 0);
            if (movie_id != null)
            {
                var movieToMove = movieList.FirstOrDefault(m => m.MovieID == movie_id);
                if (movieToMove != null)
                {
                    movieList = movieList.Where(m => m.MovieID != movie_id); // Remove the movie from the list
                    movieList = new[] { movieToMove }.Concat(movieList); // Add the movie to the beginning of the list
                }
            }
            ViewData["MovieList"] = movieList;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Refund(Guid ticketId)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(claim.Value);

            Ticket ticket = await _unitOfWork.Ticket.GetFirstOrDefaultAsync(u => u.TicketID == ticketId, includeProperties: "Showtime,Seat,Voucher");

            return View(ticket);
        }

        [HttpPost]
        public async Task<IActionResult> Refund(Guid ticketId, string fullName, string email, string reason)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(claim.Value);

            Ticket ticket = await _unitOfWork.Ticket.GetFirstOrDefaultAsync(u => u.TicketID == ticketId, includeProperties: "Voucher");
            var pointsRefund = 0.0;
            if (ticket.Voucher != null)
            {
                pointsRefund = ticket.Total * (1 - ticket.Voucher.Value / 100) * 0.8 / 1000; // Convert to points
            }
            else
            {
                pointsRefund = ticket.Total * 0.8 / 1000; // Convert to points
            }
            user.Point += (decimal)pointsRefund; // Increase the user's points

            string message = $@"
                        <p>Dear {fullName},</p>
                        <p>We hope this message finds you well.</p>
                        <p>We want to inform you that your refund request for ticket ID <strong>{ticketId}</strong> has been successfully processed.</p>
                        <p>As per our refund policy, an amount of <strong>{pointsRefund} points</strong> has been credited to your account. These points can be redeemed for future purchases on CinemaHub.</p>
                        <p>If you have any questions or concerns regarding your refund or any other matter, please don't hesitate to contact us. Our customer support team is available to assist you.</p>
                        <p>Thank you for choosing CinemaHub. We appreciate your business and look forward to serving you again in the future.</p>
                        <p>Best regards,<br>
                        The CinemaHub Team</p>
                            ";
            await _emailSender.SendEmailAsync(email, "CinemaHub: Your Refund Request", message);

            _backgroundJobClient.Enqueue(() =>
                        _unlockASeatService.UnlockASeat(ticket.SeatID, ticket.ShowtimeID, "success"));

            ticket.TicketStatus = "Refunded";
            _unitOfWork.Ticket.Update(ticket);
            _unitOfWork.Save();

            TempData["msg"] = "Refund ticket successfully.";
            return RedirectToAction("BookedTickets");
        }
        [HttpGet]
        public async Task<IActionResult> BookedTickets()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(claim.Value);

            var currentTime = DateTime.Now; // Get the current time

            var allTickets = await _unitOfWork.Ticket.GetAllAsync(u => u.AppUserID == claim.Value, includeProperties: "Showtime,Seat,Voucher");

            foreach (var ticket in allTickets)
            {
                var showtime = ticket.Showtime;
                var movieID = showtime.MovieID;
                var roomID = showtime.RoomID;
                var movie = await _unitOfWork.Movie.GetFirstOrDefaultAsync(u => u.MovieID == movieID);
                var room = await _unitOfWork.Room.GetFirstOrDefaultAsync(u => u.RoomID == roomID, includeProperties: "Cinema");

                showtime.Movie = movie;
                showtime.Room = room;
                ticket.Showtime = showtime;
            }

            return View(allTickets.OrderByDescending(u => u.BookedDate).ToList());
        }

        [HttpGet]
        public async Task<IActionResult> ChooseSeat(Guid showtime_id)
        {
            try
            {

                var showtime = await _unitOfWork.Showtime.GetFirstOrDefaultAsync(u => u.ShowtimeID == showtime_id, includeProperties: "Room,Movie");
                if (showtime is not null)
                {
                    var room = showtime.Room;
                    var seats = await _unitOfWork.Seat.GetAllAsync(u => u.RoomID == room.RoomID);
                    ViewData["ShowtimeID"] = showtime_id.ToString();
                    ViewData["Seats"] = seats;
                    return View();
                }
                else
                {
                    return RedirectToAction("Error", "Home");

                }
            }
            catch (Exception ex)
            {
                return RedirectToAction("Error", "Home");
            }
        }

        public async Task<IActionResult> Cancel(string? seatIDs, Guid? showtime_id)
        {
            var session = HttpContext.Session;
            seatIDs = session.GetString("seatIDs") != null ? session.GetString("seatIDs") : seatIDs;
            showtime_id = session.GetString("showtime_id") != null ? Guid.Parse(session.GetString("showtime_id")) : showtime_id;

            string[] seatIDListString = seatIDs.Split(',');
            List<Guid> seatIDList = new List<Guid>();
            foreach (var s in seatIDListString)
            {
                Guid sID = Guid.Parse(s);
                seatIDList.Add(sID);
            }

            bool isReload = false;
            foreach (var seatId in seatIDList)
            {
                var seat = await _unitOfWork.Seat.GetFirstOrDefaultAsync(u => u.SeatID == seatId);
                if (seat.SeatStatus.ToLower().Contains(showtime_id + "_status=pending"))
                {
                    isReload = true;
                    _backgroundJobClient.Enqueue(() => _unlockASeatService.UnlockASeat(seatId, (Guid)showtime_id, "pending"));
                }
            }

            TempData["success"] = "Booking Confirmation cancelled.";
            return RedirectToAction("ChooseSeat", new { showtime_id = showtime_id });

        }

        [HttpPost]
        public async Task<IActionResult> LockSeats(string seatIDs, Guid showtime_id)
        {
            string[] seatIDListString = seatIDs.Split(',');
            List<Guid> seatIDList = new List<Guid>();
            foreach (var s in seatIDListString)
            {
                Guid sID = Guid.Parse(s);
                seatIDList.Add(sID);
            }

            bool isReload = false;
            foreach (var seatId in seatIDList)
            {
                var seat = await _unitOfWork.Seat.GetFirstOrDefaultAsync(u => u.SeatID == seatId);
                LockASeat(seat, showtime_id, "pending");
            }
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> UnlockSeats(string seatIDs, Guid showtime_id)
        {
            string[] seatIDListString = seatIDs.Split(',');
            List<Guid> seatIDList = new List<Guid>();
            foreach (var s in seatIDListString)
            {
                Guid sID = Guid.Parse(s);
                seatIDList.Add(sID);
            }

            bool isReload = false;
            foreach (var seatId in seatIDList)
            {
                var seat = await _unitOfWork.Seat.GetFirstOrDefaultAsync(u => u.SeatID == seatId);
                if (seat.SeatStatus.ToLower().Contains(showtime_id + "_status=pending"))
                {
                    _backgroundJobClient.Enqueue(() => _unlockASeatService.UnlockASeat(seatId, showtime_id, "pending"));
                }
            }
            return Ok();
        }

        public async Task<IActionResult> BookingConfirmation(string seatIDs, Guid showtime_id)
        {
            try
            {

                string[] seatIDListString = seatIDs.Split(',');
                List<Guid> seatIDList = new List<Guid>();
                foreach (var s in seatIDListString)
                {
                    Guid sID = Guid.Parse(s);
                    seatIDList.Add(sID);
                }

                var showtime = await _unitOfWork.Showtime.GetFirstOrDefaultAsync(x => x.ShowtimeID == showtime_id, includeProperties: "Room");
                var room = showtime.Room;
                var movie = await _unitOfWork.Movie.GetFirstOrDefaultAsync(u => u.MovieID == showtime.MovieID);
                var cinema = await _unitOfWork.Cinema.GetFirstOrDefaultAsync(u => u.CinemaID == room.CinemaID);

                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
                var user = await _userManager.FindByIdAsync(claim.Value);


                List<Seat> seatList = new List<Seat>();
                List<string> seatsName = new List<string>();
                var total = 0.0;
                foreach (var seatID in seatIDList)
                {
                    var seat = await _unitOfWork.Seat.GetFirstOrDefaultAsync(u => u.SeatID == seatID);
                    seatList.Add(seat);
                    seatsName.Add(seat.SeatName);
                    total += movie.Price;
                }

                foreach (var seat in seatList)
                {
                    LockASeat(seat, showtime_id, "pending");
                }

                var Time = $"Date: {showtime.Date.ToString()} Time: {showtime.Time}h{showtime.Minute}";
                ViewData["Time"] = Time;
                ViewData["Movie"] = movie.MovieName;
                ViewData["Duration"] = movie.Duration.ToString();
                ViewData["Seats"] = seatsName;
                ViewData["Room"] = room.RoomName;
                ViewData["Cinema"] = cinema.CinemaName;
                ViewData["Price"] = movie.Price.ToString();
                ViewData["Total"] = total.ToString();
                ViewData["user_points"] = user.Point;

                ViewData["seatIDs"] = seatIDs;
                ViewData["showtime_id"] = showtime_id;

                return View();
            }
            catch (Exception ex)
            {
                TempData["error"] = "An unexpected error occurs. Please try again.";
                return RedirectToAction("Index", "Home");
            }
        }


        [HttpGet]
        public async Task<IActionResult> ViewQRCode(Guid ticketId)
        {
            QRCodeGenerator QrGenerator = new QRCodeGenerator();
            QRCodeData QrCodeInfo = QrGenerator.CreateQrCode(ticketId.ToString(), QRCodeGenerator.ECCLevel.Q);

            BitmapByteQRCode qrCode = new BitmapByteQRCode(QrCodeInfo);
            string QrUri = string.Format("data:image/png;base64,{0}", Convert.ToBase64String(qrCode.GetGraphic(60)));
            //ViewBag.QrCodeUri = QrUri;
            Ticket ticket = await _unitOfWork.Ticket.GetFirstOrDefaultAsync(u => u.TicketID == ticketId, includeProperties: "Showtime,Seat");
            if (ticket != null)
            {
                ViewData["SeatName"] = ticket.Seat.SeatName;
                Showtime showtime = ticket.Showtime;
                Movie movie = await _unitOfWork.Movie.GetFirstOrDefaultAsync(u => u.MovieID == showtime.MovieID);
                Room room = await _unitOfWork.Room.GetFirstOrDefaultAsync(u => u.RoomID == showtime.RoomID);
                ViewData["RoomName"] = room.RoomName;
                ViewData["TicketStatus"] = ticket.TicketStatus;
                ViewData["Showtime"] = showtime.Date.ToShortDateString()
                    + " " + showtime.Time + ":" + showtime.Minute
                    + " | " + movie.MovieName;
            }
            return PartialView("_QRCodePartial", QrUri);
        }

        [HttpGet]
        public IActionResult DownloadQRCode(Guid ticketId)
        {
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(ticketId.ToString(), QRCodeGenerator.ECCLevel.Q);

            BitmapByteQRCode qrCode = new BitmapByteQRCode(qrCodeData);
            byte[] qrCodeBytes = qrCode.GetGraphic(60);

            return File(qrCodeBytes, "image/png", "QRCode.png");
        }


        private void LockASeat(Seat seat, Guid showtime_id, string? status)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            IUnitOfWork uow = new UnitOfWork(dbContext);
            if (!seat.SeatStatus.ToLower().Contains("locked"))
            {
                seat.SeatStatus = "LOCKED_";
            }

            if (status == "pending")
                if (!seat.SeatStatus.ToLower().Contains((showtime_id.ToString() + "_status=pending").ToLower()))
                {
                    seat.SeatStatus += showtime_id.ToString() + "_status=pending";
                }

            if (status == "success")
            {
                if (seat.SeatStatus.ToLower().Contains((showtime_id.ToString() + "_status=pending").ToLower()))
                {
                    seat.SeatStatus = seat.SeatStatus.Replace(showtime_id.ToString() + "_status=pending", showtime_id.ToString() + "_status=success");
                }
                else
                {
                    seat.SeatStatus += showtime_id.ToString() + "_status=success";
                }
            }

            uow.Seat.Update(seat);
            uow.Save();
            Console.WriteLine($"Lock : {showtime_id.ToString().ToUpper()}");
        }

        #region API Call
        [HttpGet]
        public async Task<IActionResult> GetSeatStatuses(string showtime_id)
        {
            try
            {
                var showtime = await _unitOfWork.Showtime.GetFirstOrDefaultAsync(u => u.ShowtimeID == Guid.Parse(showtime_id), includeProperties: "Room");
                IEnumerable<Seat> seats = null;
                if (showtime is not null)
                {
                    var room = showtime.Room;
                    seats = await _unitOfWork.Seat.GetAllAsync(u => u.RoomID == room.RoomID);
                }

                foreach (var seat in seats)
                {
                    if (!seat.SeatStatus.ToLower().Contains(showtime_id.ToLower()) && seat.SeatStatus.ToLower() != "locked")
                    {
                        seat.SeatStatus = "AVAILABLE";
                    }
                }

                var seatStatuses = seats.Select(seat => new { seatID = seat.SeatID, seatStatus = seat.SeatStatus, seatName = seat.SeatName });

                return Json(seatStatuses);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        public async Task<IActionResult> GetVoucherValue(string voucherCode)
        {
            Voucher voucher = await _unitOfWork.Voucher.GetFirstOrDefaultAsync(u => u.VoucherName == voucherCode);
            if (voucher == null)
            {
                return Json(new { data = "" });
            }
            else if (voucher.Quantity <= 0)
            {
                return Json(new { data = "" });
            }
            return Json(new { data = voucher.Value });
        }

        [HttpGet]
        public async Task<IActionResult> GetShowtimeDatesForMovie(Guid movie_id)
        {
            var showtimes = await _unitOfWork.Showtime.GetAllAsync(u => u.MovieID == movie_id);
            HashSet<string> showtimeDatesInFuture = new HashSet<string>();
            foreach (var showtime in showtimes)
            {
                int year = showtime.Date.Year;
                int month = showtime.Date.Month;
                int day = showtime.Date.Day;
                DateTime showtimeDate = new DateTime(year, month, day);
                if (showtimeDate >= DateTime.Now.Date)
                {
                    showtimeDatesInFuture.Add(showtimeDate.ToString("dd/MM/yyyy"));
                }

            }
            return Json(new { dates = showtimeDatesInFuture });

        }
        [HttpGet]
        public async Task<IActionResult> GetShowtimeForAMovieWithinADay(Guid movie_id, string? date, string? address)
        {
            string[] dateComponents = date.Split('/');
            if (dateComponents.Length != 3)
            {
                throw new FormatException("Invalid date format");
            }

            int year = int.Parse(dateComponents[2]);
            int month = int.Parse(dateComponents[1]);
            int day = int.Parse(dateComponents[0]);
            DateOnly showtimeDate = new DateOnly(year, month, day);
            var showtimesForMovie = await _unitOfWork.Showtime.GetAllAsync(u => u.MovieID == movie_id, includeProperties: "Room,Movie");

            // Get showtime for a specific day for of a movie
            var showtimesWithinDay = showtimesForMovie.Where(u => u.Date == showtimeDate);
            // Get Available room for it
            var roomsForShowtime = showtimesWithinDay.Select(u => u.Room).DistinctBy(u => u.RoomID);
            // Retrive all cinema address available for it
            var cinemas = await _unitOfWork.Cinema.GetAllAsync();

            var cinemasAddressForThisShowtime = from room in roomsForShowtime
                                                join cinema in cinemas on room.CinemaID equals cinema.CinemaID
                                                select cinema.Address;
            cinemasAddressForThisShowtime = cinemasAddressForThisShowtime.Distinct();
            if (address != null)
            {
                DateOnly currentDate = DateOnly.FromDateTime(DateTime.Now);
                DateTime currentDateTime = DateTime.Now;
                var showtimesForAllCondition = from room in roomsForShowtime
                                               join cinema in cinemas on room.CinemaID equals cinema.CinemaID
                                               join showtime in showtimesWithinDay on room.RoomID equals showtime.RoomID
                                               where cinema.Address == address &&
                                               (showtime.Date > currentDate || // Showtime Date is bigger than current date
                                               (showtime.Date == currentDate && showtime.Time > currentDateTime.Hour) || // Current date == Showtime Date
                                               (showtime.Date == currentDate && showtime.Time == currentDateTime.Hour && showtime.Minute > currentDateTime.Minute))
                                               select new { name = cinema.CinemaName, time = showtime.Time, minute = showtime.Minute, showtimeID = showtime.ShowtimeID };
                return Json(new { showtimes = showtimesWithinDay, addresses = cinemasAddressForThisShowtime, showtimesForAllCondition = showtimesForAllCondition });
            }
            return Json(new { showtimes = showtimesWithinDay, addresses = cinemasAddressForThisShowtime });
        }

        [HttpGet]
        public async Task<IActionResult> GetRecommendFilms(Guid movie_id)
        {
            DateOnly currentDate = DateOnly.FromDateTime(DateTime.Now);
            DateTime currentDateTime = DateTime.Now;
            var movie = await _unitOfWork.Movie.GetFirstOrDefaultAsync(u => u.MovieID == movie_id);
            var category = movie.Category ?? "No Category Available";
            var categories = category.ToLower().Split(',');
            var recommendedMovies = await _unitOfWork.Movie.GetAllAsync(u => categories.Any(c => u.Category.ToLower().Contains(c.Trim())) || u.Category.ToLower() == category.ToLower());
            recommendedMovies = recommendedMovies.Where(m => m.MovieID != movie_id);
            List<Showtime> showtimesForThisMovie = new List<Showtime>();
            foreach (var m in recommendedMovies)
            {
                var showtime = await _unitOfWork.Showtime.GetAllAsync(u => u.MovieID == m.MovieID);
                showtimesForThisMovie.AddRange(showtime);
            };
            var availableRecommendMovies = from showtime in showtimesForThisMovie
                                           join m in recommendedMovies on showtime.MovieID equals m.MovieID
                                           where (showtime.Date > currentDate || // Showtime Date is bigger than current date
                                               (showtime.Date == currentDate && showtime.Time > currentDateTime.Hour) || // Current date == Showtime Date
                                               (showtime.Date == currentDate && showtime.Time == currentDateTime.Hour && showtime.Minute > currentDateTime.Minute))
                                           select m;
            // Maximum is 4
            availableRecommendMovies = availableRecommendMovies.Take(4);
            return Json(new { data = availableRecommendMovies });
        }
        #endregion


        #region OLD CODE
        //public double CalculateDifferentTime(Guid showtime_id)
        //{
        //    Showtime showtime = _unitOfWork.Showtime.GetFirstOrDefaultAsync(u => u.ShowtimeID == showtime_id).Result;

        //    DateTime currentTime = DateTime.Now;
        //    DateOnly showTimeDate = showtime.Date;
        //    DateTime endShow = new DateTime(showTimeDate.Year, showTimeDate.Month, showTimeDate.Day, showtime.Time, showtime.Minute, 0);

        //    TimeSpan difference = endShow - currentTime;
        //    // Get the difference in seconds
        //    double timeUntilShowtimeEnd = difference.TotalSeconds;

        //    return timeUntilShowtimeEnd;
        //}
        //public async Task HandleLockAndUnlock(List<Guid> seats, Guid showtime_id, string? status)
        //{
        //    try
        //    {
        //        using var dbContext = _dbContextFactory.CreateDbContext();
        //        IUnitOfWork uow = new UnitOfWork(dbContext);
        //        // Handle payment settings -- On developing

        //        double timeSpan = 0;
        //        if (status == "pending")
        //        {
        //            timeSpan = 3 * 60; // 3 min
        //        }

        //        if (status == "success")
        //        {
        //            timeSpan = CalculateDifferentTime(showtime_id);
        //        }


        //        foreach (var seatID in seats)
        //        {
        //            var seat = await uow.Seat.GetFirstOrDefaultAsync(u => u.SeatID == seatID);
        //            LockASeat(seat, showtime_id, status);
        //            _backgroundJobClient.Schedule(() =>
        //                _unlockASeatService.UnlockASeat(seatID, showtime_id, status), TimeSpan.FromSeconds(timeSpan));
        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.Message);
        //        throw;
        //    }

        //}


        //public async Task<string> GenerateEmailMessage(List<Ticket> tickets, Showtime showtime)
        //{
        //    Room room = await _unitOfWork.Room.GetFirstOrDefaultAsync(u => u.RoomID == showtime.RoomID);
        //    Cinema cinema = await _unitOfWork.Cinema.GetFirstOrDefaultAsync(u => u.CinemaID == room.CinemaID);

        //    StringBuilder message = new StringBuilder();
        //    message.Append("<p>Dear Customer,</p>");
        //    message.Append("<p>Thank you for booking tickets with us. Below are the details of your booking:</p>");
        //    message.Append($"<strong>{cinema.CinemaName}</strong>");

        //    message.Append("<table border='1' cellpadding='5' cellspacing='0'>");
        //    message.Append("<tr><th>Movie</th><th>Room</th><th>Seat</th><th>Showtime</th><th>Total</th></tr>");

        //    foreach (var ticket in tickets)
        //    {
        //        var date = ticket.Showtime.Date.ToShortDateString() + " " + ticket.Showtime.Time + ":" + ticket.Showtime.Minute;
        //        message.AppendFormat("<tr><td>{0}</td><td>{1}</td><td>{2}</td><td>{3}</td><td>{4:C}</td></tr>",
        //            ticket.Showtime.Movie.MovieName, room.RoomName, ticket.Seat.SeatName, date, ticket.Total);
        //    }

        //    message.Append("</table>");
        //    message.Append("<p>Thank you for choosing our service. Enjoy the show!</p>");
        //    message.Append("<p>Best regards,<br/>Your Cinema Team</p>");

        //    return message.ToString();
        //}

        //public async Task<IActionResult> BookingProcess(string seatIDs, Guid showtime_id, string? payment_method, string totalAmount, string? status = null, string? voucherCode = null)
        //{
        //    try
        //    {

        //        var voucher = await _unitOfWork.Voucher.GetFirstOrDefaultAsync(u => u.VoucherName == voucherCode);
        //        double voucher_value = 0;
        //        Guid? voucher_id = null;
        //        if (voucher != null)
        //        {
        //            voucher_value = voucher.Value;
        //            voucher_id = voucher.VoucherID;
        //        }

        //        string[] seatIDListString = seatIDs.Split(',');
        //        List<Guid> seatIDList = new List<Guid>();

        //        foreach (var s in seatIDListString)
        //        {
        //            Guid sID = Guid.Parse(s);
        //            seatIDList.Add(sID);
        //        }

        //        if (payment_method == null)
        //        {
        //            TempData["error"] = "Payment Fails.";
        //            return RedirectToAction("Cancel", new
        //            {
        //                seatIDs = seatIDs,
        //                showtime_id = showtime_id,
        //            });
        //        }
        //        else if (payment_method == "point")
        //        {
        //            var showtime = await _unitOfWork.Showtime.GetFirstOrDefaultAsync(u => u.ShowtimeID == showtime_id, includeProperties: "Room");
        //            var movie = await _unitOfWork.Movie.GetFirstOrDefaultAsync(u => u.MovieID == showtime.MovieID);
        //            var claimsIdentity = (ClaimsIdentity)User.Identity;
        //            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
        //            var user = await _userManager.FindByIdAsync(claim.Value);


        //            List<Seat> seatList = new List<Seat>();
        //            List<Ticket> ticketList = new List<Ticket>();
        //            foreach (var seatID in seatIDList)
        //            {
        //                var seat = await _unitOfWork.Seat.GetFirstOrDefaultAsync(u => u.SeatID == seatID);

        //                Ticket ticket = new Ticket();
        //                ticket.SeatID = seat.SeatID;
        //                ticket.Seat = seat;
        //                ticket.ShowtimeID = showtime_id;
        //                ticket.Total = movie.Price;
        //                ticket.AppUserID = claim.Value;
        //                if (voucher != null)
        //                {
        //                    ticket.VoucherID = voucher_id;
        //                }
        //                ticket.BookedDate = DateTime.Now;
        //                ticketList.Add(ticket);
        //                _unitOfWork.Ticket.Add(ticket);
        //                user.Point = user.Point - (decimal)movie.Price * (decimal)(1 - voucher_value / 100) * 1000;


        //            }
        //            if (voucher != null)
        //            {
        //                voucher.Quantity--;
        //                _unitOfWork.Voucher.Update(voucher);
        //            }
        //            string message = await GenerateEmailMessage(ticketList, showtime);
        //            await _emailSender.SendEmailAsync(user.Email, "CinemaHub: Your Tickets", message);
        //            _unitOfWork.Save();

        //            HandleLockAndUnlock(seatIDList, showtime_id, "success");

        //            foreach (var seatId in seatIDList)
        //            {
        //                var ticket = await _unitOfWork.Ticket.GetFirstOrDefaultAsync(
        //                    u => u.SeatID == seatId && u.ShowtimeID == showtime_id);
        //                var timeSpan = CalculateDifferentTime(ticket.ShowtimeID);
        //                _backgroundJobClient.Schedule(() =>
        //                     _ticketService.ExpriedTicket(ticket.TicketID), TimeSpan.FromSeconds(timeSpan));
        //            }
        //            TempData["msg"] = "Payment execute successfully.";

        //            return View();
        //        }
        //        else if (payment_method == "paypal")
        //        {

        //            switch (status)
        //            {
        //                case null:
        //                    {
        //                        return RedirectToAction("AuthorizePayment", "Payment", new
        //                        {
        //                            seatIDs = seatIDs,
        //                            showtime_id = showtime_id,
        //                            status = status,
        //                            voucherCode = voucherCode
        //                        });
        //                    }
        //                case "success":
        //                    {

        //                        var showtime = await _unitOfWork.Showtime.GetFirstOrDefaultAsync(u => u.ShowtimeID == showtime_id, includeProperties: "Room");
        //                        var movie = await _unitOfWork.Movie.GetFirstOrDefaultAsync(u => u.MovieID == showtime.MovieID);
        //                        var claimsIdentity = (ClaimsIdentity)User.Identity;
        //                        var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
        //                        var user = await _userManager.FindByIdAsync(claim.Value);


        //                        List<Seat> seatList = new List<Seat>();
        //                        List<Ticket> ticketList = new List<Ticket>();
        //                        foreach (var seatID in seatIDList)
        //                        {
        //                            var seat = await _unitOfWork.Seat.GetFirstOrDefaultAsync(u => u.SeatID == seatID);

        //                            Ticket ticket = new Ticket();
        //                            ticket.SeatID = seat.SeatID;
        //                            ticket.Seat = seat;
        //                            ticket.ShowtimeID = showtime_id;
        //                            ticket.Total = movie.Price * (1 - voucher_value / 100);
        //                            ticket.AppUserID = claim.Value;
        //                            ticket.BookedDate = DateTime.Now;
        //                            if (voucher != null)
        //                            {
        //                                ticket.VoucherID = voucher_id;
        //                                voucher.Quantity--;
        //                                _unitOfWork.Voucher.Update(voucher);
        //                            }
        //                            user.Point += (decimal)(0.05 * movie.Price) * 1000;


        //                            ticketList.Add(ticket);
        //                            _unitOfWork.Ticket.Add(ticket);
        //                        }
        //                        if (voucher != null)
        //                        {
        //                            voucher.Quantity--;
        //                            _unitOfWork.Voucher.Update(voucher);
        //                        }

        //                        string message = await GenerateEmailMessage(ticketList, showtime);
        //                        await _emailSender.SendEmailAsync(user.Email, "CinemaHub: Your Tickets", message);

        //                        _unitOfWork.Save();
        //                        HandleLockAndUnlock(seatIDList, showtime_id, "success");

        //                        foreach (var seatId in seatIDList)
        //                        {
        //                            var ticket = await _unitOfWork.Ticket.GetFirstOrDefaultAsync(
        //                                u => u.SeatID == seatId && u.ShowtimeID == showtime_id);
        //                            var timeSpan = CalculateDifferentTime(ticket.ShowtimeID);
        //                            _backgroundJobClient.Schedule(() =>
        //                                 _ticketService.ExpriedTicket(ticket.TicketID), TimeSpan.FromSeconds(timeSpan));
        //                        }

        //                        TempData["msg"] = "Payment execute successfully.";
        //                        return View();
        //                    }
        //                case "cancel":
        //                    {
        //                        TempData["error"] = "Payment Cancelled.";
        //                        return RedirectToAction("Cancel", new
        //                        {
        //                            seatIDs = seatIDs,
        //                            showtime_id = showtime_id,
        //                        });
        //                    }
        //                case "fail":
        //                    {
        //                        TempData["error"] = "Payment Fails.";
        //                        return RedirectToAction("Cancel", new
        //                        {
        //                            seatIDs = seatIDs,
        //                            showtime_id = showtime_id,
        //                        });
        //                    }
        //                default:
        //                    {
        //                        TempData["error"] = "Payment Fails.";
        //                        return RedirectToAction("Cancel", new
        //                        {
        //                            seatIDs = seatIDs,
        //                            showtime_id = showtime_id,
        //                        });
        //                    }
        //            }
        //        }
        //        else 
        //        {
        //            TempData["error"] = "An unexpected error occurs. Please try again.";
        //            return RedirectToAction("ChooseSeat", new { showtime_id = showtime_id });
        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        TempData["error"] = "An unexpected error occurs. Please try again.";
        //        return RedirectToAction("Error", "Home");
        //    }
        //}

        #endregion
    }
}
