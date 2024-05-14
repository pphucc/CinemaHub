using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using CinemaHub.DataAccess.Repositories;
using CinemaHub.Models;
using CinemaHub.Utils;
using System.Diagnostics;
using System.Drawing.Printing;

namespace CinemaHub.Areas.Customer.Controllers
{
    [Area("Customer")]
    [AllowAnonymous]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly UserManager<AppUser> _userManager;
        private const int PAGESIZE = 5;
        private readonly IUnitOfWork _unitOfWork;
        public HomeController(ILogger<HomeController> logger, UserManager<AppUser> userManager, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
        }

     
        public async Task<IActionResult> Index(int? pageNumber, int? pagePromotionNumber, string? searchString)
        {

            var movies = await _unitOfWork.Movie.GetAllAsync();

            var promotion = await _unitOfWork.Promotion.GetAllAsync();
            ViewData["promotionData"] = PaginatedList<Promotion>.Create(promotion, pagePromotionNumber ?? 1, promotion.Count());

            return View(PaginatedList<Movie>.Create(movies, pageNumber ?? 1, PAGESIZE));
        }
        [HttpGet]
        public async Task<IActionResult> MovieDetail(Guid movie_id)
        {
            var movie = await _unitOfWork.Movie.GetFirstOrDefaultAsync(x => x.MovieID == movie_id);
            return View(movie);
        }
        public IActionResult Privacy()
        {
            return View();
        }
        public async Task<IActionResult> PromotionDetail(Guid id)
        {
            var promotion = await _unitOfWork.Promotion.GetFirstOrDefaultAsync(x => x.PromotionID == id);
            return View("~/Areas/Customer/Views/Home/PromotionDetail.cshtml", promotion);

        }

        public async Task<IActionResult> PromotionViewAll()
        {
            var promotions = await _unitOfWork.Promotion.GetAllAsync();
            return View(promotions);
        }

        public IActionResult Error(int? statusCode = null)
        {
            switch (statusCode)
            {
                //case 400:
                //    return View("BadRequest");
                //case 401:
                //    return View("Unauthorized");
                //case 403:
                //    return View("Forbidden");
                case 404:
                    return View("NotFound");
                //case 405:
                //    return View("MethodNotAllowed");
                //case 408:
                //    return View("RequestTimeout");
                //case 409:
                //    return View("Conflict");
                //case 410:
                //    return View("Gone");
                //case 413:
                //    return View("PayloadTooLarge");
                //case 415:
                //    return View("UnsupportedMediaType");
                //case 418:
                //    return View("ImATeapot");
                //case 429:
                //    return View("TooManyRequests");
                case 500:
                    return View("ServerError");
                //case 501:
                //    return View("NotImplemented");
                //case 502:
                //    return View("BadGateway");
                //case 503:
                //    return View("ServiceUnavailable");
                //case 504:
                //    return View("GatewayTimeout");
                default:
                    return View("ServerError");
            }
        }


        #region API Calll
        [HttpGet]
        public async Task<IActionResult> GetMoviesByCharacters(string text)
        {
            if (text is not null)
            {
                var movies = await _unitOfWork.Movie.GetAllAsync(u => u.MovieName.ToLower().Contains(text.ToLower()));
                var moviestoRender = movies.Take(4);
                return Json(new { data = moviestoRender });
            }
            return Json(new { success = false });
        }
        #endregion
    }
}
