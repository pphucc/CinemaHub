using CinemaHub.DataAccess.Data;
using CinemaHub.DataAccess.Repositories;
using CinemaHub.Models;
using CinemaHub.Services.IServices;
using CinemaHub.ViewModels;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mono.TextTemplating;
using System.Security.Claims;
using System.Text;

namespace CinemaHub.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize(Roles = "customer,admin,cinemaManager")]
    public class BookingController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<AppUser> _userManager;
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly IUnlockASeatService _unlockASeatService;
        private readonly ITicketService _ticketService;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly IEmailSender _emailSender;

        public BookingController(IUnitOfWork unitOfWork, UserManager<AppUser> userManager, IVnPayService vnPayService,
                                IDbContextFactory<AppDbContext> dbContextFactory,
                                IUnlockASeatService unlockASeatService,
                                ITicketService ticketService,
                                IEmailSender emailSender,
                                IBackgroundJobClient backgroundJobClient)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _dbContextFactory = dbContextFactory;
            _backgroundJobClient = backgroundJobClient;
            _emailSender = emailSender;
            _ticketService = ticketService;
            _unlockASeatService = unlockASeatService;
        }
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult ProceedBooking(string seatIDs, Guid showtime_id, string? payment_method, string totalAmount, string? status = null, string? voucherCode = null)
        {
            HttpContext.Session.SetString("seatIDs", seatIDs);
            HttpContext.Session.SetString("showtime_id", showtime_id.ToString());
            HttpContext.Session.SetString("voucherCode", voucherCode + "");
            HttpContext.Session.SetString("totalAmount", totalAmount);


            switch (payment_method)
            {
                case "paypal":

                    break;
                case "point":

                    return RedirectToAction("CompleteBooking", new { isPayByPoint = true });

                case "vnpay":

                    return RedirectToAction("AuthorizePayment", "VnPay");
                default:

                    break;
            }

            return Ok();
        }

        public async Task<IActionResult> CompleteBookingAsync(bool? isPayByPoint)
        {
            try
            {
                // Add Ticket to the Db, Set the auto unlock seats && expire tickets
                var tickets = await SaveTicketToDbAsync(isPayByPoint);

                await StartAutoHandleSeatStatusAsync();

                await StartAutoHandleTicketStatusAsync();

                await SendEmailToUser(tickets);

                // Clear the booking information

                // return result to the user
                TempData["msg"] = "Payment execute successfully.";
                return View();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private async Task<string> GenerateEmailMessage(List<Ticket> tickets, Showtime showtime)
        {
            Room room = await _unitOfWork.Room.GetFirstOrDefaultAsync(u => u.RoomID == showtime.RoomID);
            Cinema cinema = await _unitOfWork.Cinema.GetFirstOrDefaultAsync(u => u.CinemaID == room.CinemaID);

            StringBuilder message = new StringBuilder();
            message.Append("<p>Dear Customer,</p>");
            message.Append("<p>Thank you for booking tickets with us. Below are the details of your booking:</p>");
            message.Append($"<strong>{cinema.CinemaName}</strong>");

            message.Append("<table border='1' cellpadding='5' cellspacing='0'>");
            message.Append("<tr><th>Movie</th><th>Room</th><th>Seat</th><th>Showtime</th><th>Total</th></tr>");

            foreach (var ticket in tickets)
            {
                var date = ticket.Showtime.Date.ToShortDateString() + " " + ticket.Showtime.Time + ":" + ticket.Showtime.Minute;
                message.AppendFormat("<tr><td>{0}</td><td>{1}</td><td>{2}</td><td>{3}</td><td>{4:C}</td></tr>",
                    ticket.Showtime.Movie.MovieName, room.RoomName, ticket.Seat.SeatName, date, ticket.Total);
            }

            message.Append("</table>");
            message.Append("<p>Thank you for choosing our service. Enjoy the show!</p>");
            message.Append("<p>Best regards,<br/>Your Cinema Team</p>");

            return message.ToString();
        }
        public async Task SendEmailToUser(List<Ticket> ticketList)
        {
            var showtime_id = Guid.Parse(HttpContext.Session.GetString("showtime_id"));
            var showtime = await _unitOfWork.Showtime.GetFirstOrDefaultAsync(u => u.ShowtimeID == showtime_id);
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(claim.Value);

            string message = await GenerateEmailMessage(ticketList, showtime);
            await _emailSender.SendEmailAsync(user.Email, "CinemaHub: Your Tickets", message);
        }
        public async Task<List<Ticket>> SaveTicketToDbAsync(bool? isPayByPoint)
        {
            try
            {
                var session = HttpContext.Session;
                var voucherCode = session.GetString("voucherCode");
                var seatIDs = session.GetString("seatIDs");
                var showtime_id = Guid.Parse(session.GetString("showtime_id"));
                var totalAmount = double.Parse(session.GetString("totalAmount"));

                var showtime = await _unitOfWork.Showtime.GetFirstOrDefaultAsync(u => u.ShowtimeID == showtime_id, includeProperties: "Room");
                var movie = await _unitOfWork.Movie.GetFirstOrDefaultAsync(u => u.MovieID == showtime.MovieID);
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
                var user = await _userManager.FindByIdAsync(claim.Value);

                var voucher = await _unitOfWork.Voucher.GetFirstOrDefaultAsync(u => u.VoucherName == voucherCode);
                double voucher_value = 0;
                Guid? voucher_id = null;
                if (voucher != null)
                {
                    voucher_value = voucher.Value;
                    voucher_id = voucher.VoucherID;
                }


                List<Guid> seatIDList = GetSeatIdList(seatIDs);

                List<Ticket> ticketList = new List<Ticket>();

                foreach (var seatID in seatIDList)
                {
                    var seat = await _unitOfWork.Seat.GetFirstOrDefaultAsync(u => u.SeatID == seatID);

                    Ticket ticket = new Ticket();
                    ticket.SeatID = seat.SeatID;
                    ticket.Seat = seat;
                    ticket.ShowtimeID = showtime_id;
                    ticket.Total = Math.Round(movie.Price * (1 - voucher_value / 100), 0);
                    ticket.AppUserID = claim.Value;
                    ticket.BookedDate = DateTime.Now;
                    if (voucher != null)
                    {
                        ticket.VoucherID = voucher_id;
                    }
                    ticketList.Add(ticket);
                    _unitOfWork.Ticket.Add(ticket);
                }
                if (voucher != null)
                {
                    voucher.Quantity--;
                    _unitOfWork.Voucher.Update(voucher);
                }

                if (isPayByPoint != null && (bool)isPayByPoint)
                {
                    user.Point -= (decimal)(totalAmount * (1 - voucher_value / 100) / 1000);
                }
                else
                {
                    user.Point += (decimal)(totalAmount * (1 - voucher_value / 100) / 1000 * 0.1);
                }

                _unitOfWork.Save();
                return ticketList;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

        }
        public async Task StartAutoHandleSeatStatusAsync()
        {
            var seatIdList = HttpContext.Session.GetString("seatIDs");
            var showtime_id = Guid.Parse(HttpContext.Session.GetString("showtime_id"));

            List<Guid> seats = GetSeatIdList(seatIdList);

            try
            {
                using var dbContext = _dbContextFactory.CreateDbContext();
                IUnitOfWork uow = new UnitOfWork(dbContext);

                double timeSpan = 0;

                timeSpan = CalculateDifferentTime(showtime_id);

                foreach (var seatID in seats)
                {
                    var seat = await uow.Seat.GetFirstOrDefaultAsync(u => u.SeatID == seatID);
                    _unlockASeatService.LockASeat(seat, showtime_id, "success");
                    _backgroundJobClient.Schedule(() =>
                        _unlockASeatService.UnlockASeat(seatID, showtime_id, "success"), TimeSpan.FromSeconds(timeSpan));
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }
        public async Task StartAutoHandleTicketStatusAsync()
        {
            var seatIdList = HttpContext.Session.GetString("seatIDs");
            var showtime_id = Guid.Parse(HttpContext.Session.GetString("showtime_id"));

            List<Guid> seats = GetSeatIdList(seatIdList);

            foreach (var seatId in seats)
            {
                var ticket = await _unitOfWork.Ticket.GetFirstOrDefaultAsync(
                    u => u.SeatID == seatId && u.ShowtimeID == showtime_id);
                var timeSpan = CalculateDifferentTime(showtime_id);
                _backgroundJobClient.Schedule(() =>
                     _ticketService.ExpriedTicket(ticket.TicketID), TimeSpan.FromSeconds(timeSpan));
            }
        }
        public List<Guid> GetSeatIdList(string seatIDs)
        {
            string[] seatIDListString = seatIDs.Split(',');
            List<Guid> seatIDList = new List<Guid>();

            foreach (var s in seatIDListString)
            {
                Guid sID = Guid.Parse(s);
                seatIDList.Add(sID);
            }

            return seatIDList;
        }
        private double CalculateDifferentTime(Guid showtime_id)
        {
            Showtime showtime = _unitOfWork.Showtime.GetFirstOrDefaultAsync(u => u.ShowtimeID == showtime_id).Result;

            DateTime currentTime = DateTime.Now;
            DateOnly showTimeDate = showtime.Date;
            DateTime endShow = new DateTime(showTimeDate.Year, showTimeDate.Month, showTimeDate.Day, showtime.Time, showtime.Minute, 0);

            TimeSpan difference = endShow - currentTime;
            // Get the difference in seconds
            double timeUntilShowtimeEnd = difference.TotalSeconds;

            return timeUntilShowtimeEnd;
        }

    }
}
