using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CinemaHub.DataAccess.Data;
using CinemaHub.DataAccess.Repositories;
using CinemaHub.Models;
using CinemaHub.Services;
using static Org.BouncyCastle.Crypto.Engines.SM2Engine;

namespace CinemaHub.Areas.CinemaManager.Controllers
{
    [Area("CinemaManager")]
    [Authorize(Roles = "cinemaManager,admin")]
    public class MovieController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly UploadImageService _uploadImageService;

        public MovieController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment, UploadImageService uploadImageService)
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
            _uploadImageService = uploadImageService;

        }
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            IEnumerable<Movie> movies = await _unitOfWork.Movie.GetAllAsync();
            //return View(cinemas);
            return View(movies);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Movie movie, IFormFile? fileImage, IFormFile? fileVideo)
        {
            if (ModelState.IsValid)
            {
                //string wwwRootPath = _webHostEnvironment.WebRootPath;
                //if (fileImage != null)
                //{
                //	string fileNameImage = Guid.NewGuid().ToString() + Path.GetExtension(fileImage.FileName);
                //	string MovieImagePath = Path.Combine(wwwRootPath, @"images\movie");
                //	using (var fileStream = new FileStream(Path.Combine(MovieImagePath, fileNameImage), FileMode.Create))
                //	{
                //		fileImage.CopyTo(fileStream);

                //	}
                //	movie.ImageUrl = @"\images\movie\" + fileNameImage;

                //}
                if (fileImage is not null)
                {
                    // Check extension of file, make sure it's an image file (.jpg / .png)
                    string extension = Path.GetExtension(fileImage.FileName);
                    if (extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) || extension.Equals(".png", StringComparison.OrdinalIgnoreCase))
                    {
                        movie.ImageUrl = _uploadImageService.UploadImage(fileImage, @"images\movie");
                    }
                    else
                    {
                        ViewData["imageError"] = "Invalid image file format. Only .jpg and .png files are supported.";
                        return View(movie);
                    }
                }

                if (fileVideo is not null)
                {
                    // Make sure the extension of the file is .mp4/.mov/
                    string extension = Path.GetExtension(fileVideo.FileName);
                    if (extension.Equals(".mp4", StringComparison.OrdinalIgnoreCase) || extension.Equals(".mov", StringComparison.OrdinalIgnoreCase))
                    {
                        movie.VideoUrl = _uploadImageService.UploadImage(fileVideo, @"images\movie");
                    }
                    else
                    {
                        ViewData["videoError"] = "Invalid video file format. Only .mp4 and .mov files are supported.";
                        return View(movie);
                    }
                }

                _unitOfWork.Movie.Add(movie);
                _unitOfWork.Save();
                TempData["msg"] = "Movie created succesfully";
            }
            return RedirectToAction("Index");
        }
        [HttpGet]
        public async Task<IActionResult> Update(Guid movie_id)
        {
            var movie = await _unitOfWork.Movie.GetFirstOrDefaultAsync(u => u.MovieID == movie_id);
            if (movie == null)
            {
                return NotFound();
            }
            else
            {
                return View(movie);
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(Movie movie, IFormFile? fileImage, IFormFile? fileVideo)
        {
            // Get the old image of movie
            var _movie = await _unitOfWork.Movie.GetFirstOrDefaultAsync(u => u.MovieID == movie.MovieID);

            if (_movie == null)
            {
                return NotFound();
            }
            _movie.MovieName = movie.MovieName;
            _movie.Description = movie.Description;
            _movie.Price = movie.Price;
            _movie.Director = movie.Director;
            _movie.Actor = movie.Actor;
            _movie.Duration = movie.Duration;
            _movie.Country = movie.Country;
            _movie.Studio = movie.Studio;
            _movie.ReleaseDate = movie.ReleaseDate;
            _movie.EndDate = movie.EndDate;
            _movie.Version = movie.Version;
            _movie.Category = movie.Category;
            if (ModelState.IsValid)
            {
                if (fileImage is not null)
                {
                    // Check extension of file, make sure it's an image file (.jpg / .png)
                    string extension = Path.GetExtension(fileImage.FileName);
                    if (extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) || extension.Equals(".png", StringComparison.OrdinalIgnoreCase))
                    {
                        var old = _movie.ImageUrl;
                        _movie.ImageUrl = _uploadImageService.UploadImage(fileImage, @"images\movie", old);
                    }
                    else
                    {
                        ViewData["imageError"] = "Invalid image file format. Only .jpg and .png files are supported.";
                        return View(movie);
                    }
                }

                if (fileVideo is not null)
                {
                    // Make sure the extension of the file is .mp4/.mov/
                    string extension = Path.GetExtension(fileVideo.FileName);
                    if (extension.Equals(".mp4", StringComparison.OrdinalIgnoreCase) || extension.Equals(".mov", StringComparison.OrdinalIgnoreCase))
                    {
                        var old = _movie.VideoUrl;
                        _movie.VideoUrl = _uploadImageService.UploadImage(fileVideo, @"images\movie", old);
                    }
                    else
                    {
                        ViewData["videoError"] = "Invalid video file format. Only .mp4 and .mov files are supported.";
                        return View(movie);
                    }
                }
                _unitOfWork.Movie.Update(_movie);
                _unitOfWork.Save();
                TempData["msg"] = "Movie updated succesfully";
                return RedirectToAction("Index");
            }

            return View(movie);
        }
        #region API Calls
        [HttpGet]
        public async Task<IActionResult> GetAllMovies()
        {
            var movies = await _unitOfWork.Movie.GetAllAsync();
            return Json(new { data = movies });
        }
        [HttpDelete]
        public async Task<IActionResult> Delete(Guid movie_id)
        {
            var movie = await _unitOfWork.Movie.GetFirstOrDefaultAsync(u => u.MovieID == movie_id);
            if (movie == null)
            {
                return Json(new { success = false, message = "Error while deleting" });
            }
            _unitOfWork.Movie.Delete(movie);
            _unitOfWork.Save();
            return Json(new { success = true, message = "Delete successfully! " });
        }
        #endregion
    }
}
