using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CinemaHub.DataAccess.Repositories;
using CinemaHub.Models;

namespace CinemaHub.Areas.CinemaManager.Controllers
{
	[Area("CinemaManager")]
	[Authorize(Roles = "cinemaManager,admin")]
	public class CinemaController : Controller
	{
		private readonly IUnitOfWork _unitOfWork;

		public CinemaController(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
		}
		[HttpGet]
		public IActionResult Index()
		{			
			return View();
		}

		[HttpGet]
		public IActionResult Create()
		{
			return View();
		}
		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult Create(Cinema cinema)
		{
			if (ModelState.IsValid)
			{
				_unitOfWork.Cinema.Add(cinema);
				_unitOfWork.Save();
                TempData["msg"] = "Create Cinema successfully.";

            }
            return RedirectToAction("Index");
		}
		[HttpGet]
		public async Task<IActionResult> Update(Guid cinema_id)
		{
			var cinema = await _unitOfWork.Cinema.GetFirstOrDefaultAsync(u => u.CinemaID == cinema_id);
			if (cinema == null)
			{
				return NotFound();
			}
			else
			{
				return View(cinema);
			}
		}
		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult Update(Cinema cinema)
		{
			if (ModelState.IsValid)
			{
				_unitOfWork.Cinema.Update(cinema);
				_unitOfWork.Save();
                TempData["msg"] = "Update Cinema successfully.";

            }
            return RedirectToAction("Index");
		}
		#region API Calls
		[HttpGet]
		public async Task<IActionResult> GetAllCinemas()
		{
			var cinemas = await _unitOfWork.Cinema.GetAllAsync();
			return Json(new { data = cinemas });
		}
        public async Task<IActionResult> GetCinemaById(Guid cinema_id)
        {
            var cinema = await _unitOfWork.Cinema.GetFirstOrDefaultAsync(u => u.CinemaID == cinema_id);
            return Json(new { data = cinema });
        }
        [HttpDelete]
		public async Task<IActionResult> Delete(Guid cinema_id)
		{
			var cinema = await _unitOfWork.Cinema.GetFirstOrDefaultAsync( u=> u.CinemaID ==cinema_id);
			if (cinema == null)
			{
				return Json(new { success = false, message = "Error while deleting" });
			}
			_unitOfWork.Cinema.Delete(cinema);
			_unitOfWork.Save();
			return Json(new { success = true, message = "Delete cinema successfully! " });
		}
		#endregion
	}
}
