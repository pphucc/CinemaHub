using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using CinemaHub.DataAccess.Repositories;
using CinemaHub.Models;
using System.Linq;

namespace CinemaHub.Areas.CinemaManager.Controllers
{
    [Area("CinemaManager")]
    [Authorize(Roles = "cinemaManager,admin")]
    public class SeatController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        public SeatController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task<IActionResult> Index()
        {
            IEnumerable<Room> RoomList = await _unitOfWork.Room.GetAllAsync(includeProperties: nameof(Cinema));
            IEnumerable<SelectListItem> RoomListSelect = RoomList.Select(u => new SelectListItem
            {
                Text = u.RoomName + " - " + u.Cinema.CinemaName,
                Value = u.RoomID.ToString(),
            });
            ViewData["RoomList"] = RoomListSelect;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Create(Guid room_id)
        {
            Room r = await _unitOfWork.Room.GetFirstOrDefaultAsync(u => u.RoomID == room_id);
            ViewData["room_id"] = room_id;
            ViewData["room_name"] = r.RoomName;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Seat seat, string addType, string? rowName, int? numberOfSeats)
        {
            if (addType == "single")
            {
                if (ModelState.IsValid)
                {
                    var room = await _unitOfWork.Room.GetFirstOrDefaultAsync(u => u.RoomID == seat.RoomID);
                    if (room != null)
                    {
                        var allSeat = await _unitOfWork.Seat.GetAllAsync(s => s.RoomID == room.RoomID);
                        int remainingSeats = room.NumOfSeats - allSeat.Count();

                        if (remainingSeats > 0)
                        {
                            _unitOfWork.Seat.Add(seat);
                            _unitOfWork.Save();
                        }
                        else
                        {
                            ViewData["room_id"] = room.RoomID;
                            ViewData["room_name"] = room.RoomName;
                            TempData["error"] = "No more seats available in the room.";
                            return View(seat);
                        }
                    }
                    return RedirectToAction("RoomSeats", new { room_id = seat.RoomID });
                }
            }
            else if (addType == "multiple")
            {
                if (ModelState.IsValid)
                {
                    var room = await _unitOfWork.Room.GetFirstOrDefaultAsync(u => u.RoomID == seat.RoomID);
                    if (room != null)
                    {
                        var allSeat = await _unitOfWork.Seat.GetAllAsync(s => s.RoomID == room.RoomID);
                        int remainingSeats = room.NumOfSeats - allSeat.Count();

                        int seatsToAdd = Math.Min(numberOfSeats ?? 0, remainingSeats);
                        if (seatsToAdd <= 0)
                        {
                            ViewData["room_id"] = room.RoomID;
                            ViewData["room_name"] = room.RoomName;
                            TempData["error"] = "No more seats available in the room.";
                            return View(seat);
                        }
                        else
                            for (int i = 0; i < seatsToAdd; i++)
                            {
                                var newSeat = new Seat
                                {
                                    RoomID = room.RoomID,
                                    SeatName = rowName + (i + 1),
                                    SeatStatus = seat.SeatStatus
                                };
                                _unitOfWork.Seat.Add(newSeat);
                            }
                    }
                    _unitOfWork.Save();
                    return RedirectToAction("RoomSeats", new { room_id = seat.RoomID });
                }
            }
            ViewData["room_id"] = seat.Room.RoomID;
            ViewData["room_name"] = seat.Room.RoomName;
            TempData["msg"] = "Create successfully.";

            return View(seat);
        }


        [HttpGet]
        public async Task<IActionResult> Update(Guid seat_id)
        {
            var seat = await _unitOfWork.Seat.GetFirstOrDefaultAsync(u => u.SeatID == seat_id, includeProperties: nameof(Room));
            if (seat == null)
            {
                return NotFound();
            }
            else
            {
                return View(seat);
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(Seat seat)
        {
            if (ModelState.IsValid)
            {
                _unitOfWork.Seat.Update(seat);
                _unitOfWork.Save();
                // ViewData["msg"] = "Seat updated successfully.";
                TempData["msg"] = "Update successfully.";
                return RedirectToAction("RoomSeats", new { room_id = seat.RoomID });
            }
            return View(seat);
        }


        public async Task<IActionResult> RoomSeats(Guid room_id)
        {
            ViewData["room_id"] = room_id;
            IEnumerable<Seat> seats = await _unitOfWork.Seat.GetAllAsync(u => u.RoomID == room_id);
            return View(seats.OrderBy(u => u.SeatName));
        }


        #region API Calls
        [HttpGet]
        public async Task<IActionResult> GetSeatList(Guid? room_id)
        {
            IEnumerable<Seat> seatList;
            if (string.IsNullOrEmpty(room_id.ToString()))
            {
                seatList = await _unitOfWork.Seat.GetAllAsync(includeProperties: nameof(Room));
            }
            else
            {
                seatList = await _unitOfWork.Seat.GetAllAsync(u => u.RoomID == room_id, includeProperties: nameof(Room));
            }
            return Json(new { data = seatList });
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(Guid seat_id)
        {
            var seat = await _unitOfWork.Seat.GetFirstOrDefaultAsync(u => u.SeatID == seat_id);
            if (seat == null)
            {
                return Json(new { success = false, message = "Error while deleting" });
            }
            _unitOfWork.Seat.Delete(seat);
            _unitOfWork.Save();
            return Json(new { success = true, message = "Delete Seat successfully! " });
        }
        #endregion
    }
}
