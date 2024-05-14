using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CinemaHub.DataAccess.Repositories;
using CinemaHub.Models;
using CinemaHub.Models.ViewModels;

namespace CinemaHub.Areas.CinemaManager.Controllers
{   
    [Area("CinemaManager")]
    [Authorize(Roles = "cinemaManager,admin")]
    public class RoomController : Controller
    {
  
        private readonly IUnitOfWork _unitOfWork;
        public RoomController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            //IEnumerable<Cinema> cinemas = await _unitOfWork.Cinema.GetAllAsync();
            //return View(cinemas);
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var cinemas = await _unitOfWork.Cinema.GetAllAsync();
            RoomVM roomVM = new()
            {
                CinemaList = cinemas.Select(u => new SelectListItem
                {
                    Text = u.CinemaName,
                    Value = u.CinemaID.ToString(),
                }),
                Room = new Room()
            };
           
            return View(roomVM);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Room room)
        {
            if (ModelState.IsValid)
            {
                _unitOfWork.Room.Add(room);
                _unitOfWork.Save();
                TempData["msg"] = "Create Room successfully.";

            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Update(Guid room_id)
        {
            var cinemas = await _unitOfWork.Cinema.GetAllAsync();
            RoomVM roomVM = new()
            {
                CinemaList = cinemas.Select(u => new SelectListItem
                {
                    Text = u.CinemaName,
                    Value = u.CinemaID.ToString(),
                }),
                Room = await _unitOfWork.Room.GetFirstOrDefaultAsync(u => u.RoomID == room_id)
            };
            
            var rooms = roomVM;
            return View(rooms);

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Update(Room room)
        {
            if (ModelState.IsValid)
            {
                _unitOfWork.Room.Update(room);
                _unitOfWork.Save();
                TempData["msg"] = "Update Room successfully.";

            }
            return RedirectToAction("Index");
        }

        #region API Calls
        [HttpGet]
        public async Task<IActionResult> GetAllRooms()
        {
            var rooms = await _unitOfWork.Room.GetAllAsync(includeProperties: "Cinema");
            return Json(new { data = rooms });
        }
        [HttpDelete]
        public async Task<IActionResult> Delete(Guid room_id)
        {
            var room = await _unitOfWork.Room.GetFirstOrDefaultAsync(u => u.RoomID == room_id);
            if (room == null)
            {
                return Json(new { success = false, message = "Error while deleting" });
            }
            _unitOfWork.Room.Delete(room);
            _unitOfWork.Save();
            return Json(new { success = true, message = "Delete room successfully! " });
        }
        #endregion
    }
}
