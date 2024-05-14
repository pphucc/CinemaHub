using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using CinemaHub.DataAccess.Repositories;
using CinemaHub.Models;

namespace CinemaHub.Areas.CinemaManager.Controllers
{
    [Area("CinemaManager")]
    [Authorize(Roles = "cinemaManager,admin")]
    public class VoucherController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        public VoucherController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public IActionResult Index()
        {
            return View();
        }


        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Voucher voucher)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    _unitOfWork.Voucher.Add(voucher);
                    _unitOfWork.Save();
                    TempData["msg"] = "Create voucher successfully.";
                }
            }
            catch
            {
                throw;
            }
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Update(Guid voucher_id)
        {
            var voucher = await _unitOfWork.Voucher.GetFirstOrDefaultAsync(u => u.VoucherID == voucher_id);
            return View(voucher);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Update(Voucher voucher)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    _unitOfWork.Voucher.Update(voucher);
                    _unitOfWork.Save(); 
                    TempData["msg"] = "Update voucher successfully.";

                }

            }
            catch
            {
                throw;
            }
            return RedirectToAction("Index");
        }

        public ActionResult Delete(int id)
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                TempData["msg"] = "Delete successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["error"] = "Error while deleting voucher " + ex.Message;
                return View();
            }
        }

        #region API Calls
        [HttpGet]
        public async Task<IActionResult> GetAllVouchers()
        {
            var vouchers = await _unitOfWork.Voucher.GetAllAsync();
            return Json(new { data = vouchers });
        }
        [HttpDelete]
        public async Task<IActionResult> Delete(Guid voucher_id)
        {
            var voucher = await _unitOfWork.Voucher.GetFirstOrDefaultAsync(u => u.VoucherID == voucher_id);
            _unitOfWork.Voucher.Delete(voucher);
            _unitOfWork.Save();
            return Json(new { success = true, message = "Delete voucher successfully! " });
        }
        #endregion
    }
}
