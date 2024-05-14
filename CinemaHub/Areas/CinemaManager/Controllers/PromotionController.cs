using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using CinemaHub.DataAccess.Repositories;
using CinemaHub.Models;
using CinemaHub.Services;
using static Org.BouncyCastle.Crypto.Engines.SM2Engine;

namespace CinemaHub.Areas.CinemaManager.Controllers
{
    [Area("CinemaManager")]
    [Authorize(Roles = "cinemaManager,admin")]
    public class PromotionController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
		private readonly IWebHostEnvironment _webHostEnvironment;
		private readonly UploadImageService _uploadImageService;
		public PromotionController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment, UploadImageService uploadImageService)
		{
			_unitOfWork = unitOfWork;
			_webHostEnvironment = webHostEnvironment;
			_uploadImageService = uploadImageService;

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
        public IActionResult Create(Promotion promotion, IFormFile? fileImage)
		{
            if (ModelState.IsValid)
            {
                if (fileImage != null && fileImage.Length > 0)
                {
                    promotion.ImageUrl = _uploadImageService.UploadImage(fileImage, @"images\promotion");
                }
                else
                {
                    promotion.ImageUrl = null;
                }

                _unitOfWork.Promotion.Add(promotion);
                _unitOfWork.Save();
                TempData["msg"] = "Promotion created succesfully";

            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Update(Guid promotion_id)
        {
            var promotion = await _unitOfWork.Promotion.GetFirstOrDefaultAsync(u => u.PromotionID == promotion_id);
            if (promotion == null)
            {
                return NotFound();
            }
            else
            {
                return View(promotion);
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
		public async Task<IActionResult> Update(Promotion promotion, IFormFile? fileImage)
        {
			var _promotion = await _unitOfWork.Promotion.GetFirstOrDefaultAsync(u => u.PromotionID == promotion.PromotionID);
            _promotion.Topic = promotion.Topic;
            _promotion.Content = promotion.Content;
            _promotion.StartDate = promotion.StartDate;
            _promotion.EndDate = promotion.EndDate;
			if (ModelState.IsValid)
			{
				if (fileImage is not null)
				{
                    var old = _promotion.ImageUrl;
					_promotion.ImageUrl = _uploadImageService.UploadImage(fileImage, @"images\promotion", old);
				}
					
				_unitOfWork.Promotion.Update(_promotion);
				_unitOfWork.Save();
                TempData["msg"] = "Promotion updated succesfully";

            }
            return RedirectToAction("Index");
        }

        #region API Calls
        [HttpGet]
        public async Task<IActionResult> GetAllPromotions()
        {
            var promotions = await _unitOfWork.Promotion.GetAllAsync();
            return Json(new { data = promotions });
        }
        
        [HttpDelete]
        public async Task<IActionResult> Delete(Guid promotion_id)
        {
            var promotion = await _unitOfWork.Promotion.GetFirstOrDefaultAsync(u => u.PromotionID == promotion_id);
            if (promotion == null)
            {
                return Json(new { success = false, message = "Error while deleting" });
            }
            _unitOfWork.Promotion.Delete(promotion);
            _unitOfWork.Save();
            return Json(new { success = true, message = "Delete promotion successfully! " });
        }
        #endregion
    }
}
