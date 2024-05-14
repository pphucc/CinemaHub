using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QRCoder;
using CinemaHub.DataAccess.Repositories;
using CinemaHub.Models;

namespace CinemaHub.Areas.CinemaManager.Controllers
{
    [Area("CinemaManager")]
    [Authorize(Roles = "cinemaManager,admin")]
    public class QRCodeController : Controller
    {

        private readonly IUnitOfWork _unitOfWork;

        public QRCodeController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public IActionResult Scan()
        {
            return View();
        }   

        [HttpPost]
        public async Task<IActionResult> CheckTicket(Guid ticket_id)
        {
            if (await IsValidTicket(ticket_id))
            {
                return Json(new { success = true, message = "Ticket is valid." });
            }
            else
            {
                return Json(new { success = false, message = "Ticket is not valid." });
            }
        }
        [HttpPost]
        public async Task<IActionResult> CheckInTicket(Guid ticket_id)
        {
            try
            {

                Ticket ticket = await _unitOfWork.Ticket.GetFirstOrDefaultAsync(u => u.TicketID == ticket_id);
                ticket.TicketStatus = "Expired";
                _unitOfWork.Ticket.Update(ticket);
                _unitOfWork.Save();
                return Json(new { success = true, message = "Check in successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Check in fail. An error occur: " + ex.Message});

            }

        }

        private async Task<bool> IsValidTicket(Guid ticket_id)
        {
            Ticket ticket = await _unitOfWork.Ticket.GetFirstOrDefaultAsync(u => u.TicketID == ticket_id);
            if(ticket == null)
            {
                return false;
            }
            return ticket.TicketStatus == "Available";
        }


    }
}
