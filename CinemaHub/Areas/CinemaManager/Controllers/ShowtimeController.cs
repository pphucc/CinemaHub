using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SQLitePCL;
using CinemaHub.DataAccess.Repositories;
using CinemaHub.Models;
using CinemaHub.Models.ViewModels;


namespace CinemaHub.Areas.CinemaManager.Controllers
{
    [Area("CinemaManager")]
    [Authorize(Roles = "cinemaManager,admin")]

    public class ShowtimeController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        public ShowtimeController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            return View();
        }


        [HttpGet]
        public async Task<IActionResult> Create()
        {

            var cinemas = await _unitOfWork.Cinema.GetAllAsync();
            var movies = await _unitOfWork.Movie.GetAllAsync();
            var rooms = await _unitOfWork.Room.GetAllAsync();

            ShowtimeVM showtimeVM = new()
            {
                CinemaList = cinemas.Select(u => new SelectListItem
                {
                    Text = u.CinemaName,
                    Value = u.CinemaID.ToString(),

                }),
                // TODO: Fix Movie List
                MovieList = movies.Select(u => new SelectListItem
                {
                    Text = u.MovieName,
                    Value = u.MovieID.ToString(),
                }),

                Showtime = new Showtime()
            };

            return View(showtimeVM);

        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ShowtimeVM showtimeVM)
        {
            if (ModelState.IsValid)
            {
                var showtime = showtimeVM.Showtime;
                var roomID = showtimeVM.Showtime.RoomID;
                var existingShowtimes = await _unitOfWork.Showtime.GetAllAsync(u => u.RoomID == roomID && u.Date == showtimeVM.Showtime.Date);

                bool hasTimeConflict = false;

                foreach (var existingShowtime in existingShowtimes)
                {
                    var startTimeOfExistingShowtime = existingShowtime.Time * 60 + existingShowtime.Minute;
                    var movie = await _unitOfWork.Movie.GetFirstOrDefaultAsync(u => u.MovieID == existingShowtime.MovieID);
                    var movie_duration = movie.Duration;
                    var endTimeOfExistingShowtime = startTimeOfExistingShowtime + movie_duration;

                    var startTimeOfNewShowtime = showtime.Time * 60 + showtime.Minute;
                    var endTimeOfNewShowtime = startTimeOfNewShowtime + movie_duration;

                    if (startTimeOfNewShowtime < endTimeOfExistingShowtime && endTimeOfNewShowtime > startTimeOfExistingShowtime)
                    {
                        hasTimeConflict = true;
                        break;
                    }
                }

                if (!hasTimeConflict)
                {
                    _unitOfWork.Showtime.Add(showtime);
                    _unitOfWork.Save();
                    TempData["msg"] = "Create showtime successfully.";

                    return RedirectToAction("Index");
                }
                else
                {

                    var cinemas = await _unitOfWork.Cinema.GetAllAsync();
                    var movies = await _unitOfWork.Movie.GetAllAsync();

                    showtimeVM.CinemaList = cinemas.Select(u => new SelectListItem
                    {
                        Text = u.CinemaName,
                        Value = u.CinemaID.ToString(),
                    });

                    showtimeVM.MovieList = movies.Select(u => new SelectListItem
                    {
                        Text = u.MovieName,
                        Value = u.MovieID.ToString(),
                    });

                    TempData["error"] = "Start/End time conflict with others.";
                    return View(showtimeVM);
                }
            }
            TempData["msg"] = "Create showtime successfully.";
            return View(showtimeVM);
        }




        [HttpGet]
        public async Task<IActionResult> Update(Guid showtime_id)
        {
            var cinemas = await _unitOfWork.Cinema.GetAllAsync();
            var movies = await _unitOfWork.Movie.GetAllAsync();

            ShowtimeVM showtimeVM = new ShowtimeVM
            {

                CinemaList = cinemas.Select(u => new SelectListItem
                {
                    Text = u.CinemaName,
                    Value = u.CinemaID.ToString(),
                }),
                MovieList = movies.Select(u => new SelectListItem
                {
                    Text = u.MovieName,
                    Value = u.MovieID.ToString(),
                }),
                Showtime = await _unitOfWork.Showtime.GetFirstOrDefaultAsync(u => u.ShowtimeID == showtime_id),
            };

            var showtimes = showtimeVM;
            return View(showtimes);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(ShowtimeVM showtimeVM)
        {
            if (ModelState.IsValid)
            {
                var updatedShowtime = showtimeVM.Showtime;

                var oldShowtime = new Showtime
                {
                    ShowtimeID = updatedShowtime.ShowtimeID,
                    RoomID = updatedShowtime.RoomID,
                    Date = updatedShowtime.Date,
                    Time = updatedShowtime.Time,
                };

                var roomID = updatedShowtime.RoomID;
                var date = updatedShowtime.Date;
                var existingShowtimes = await _unitOfWork.Showtime.GetAllAsync(u => u.RoomID == roomID && u.Date == date);
                bool hasTimeConflict = false;

                foreach (var existingShowtime in existingShowtimes)
                {
                    if (existingShowtime.ShowtimeID != updatedShowtime.ShowtimeID)
                    {
                        var startTimeOfExistingShowtime = existingShowtime.Time * 60 + existingShowtime.Minute;
                        var movie = await _unitOfWork.Movie.GetFirstOrDefaultAsync(u => u.MovieID == existingShowtime.MovieID);
                        var movie_duration = movie.Duration;
                        var endTimeOfExistingShowtime = startTimeOfExistingShowtime + movie_duration;

                        var startTimeOfNewShowtime = updatedShowtime.Time * 60 + updatedShowtime.Minute;
                        var endTimeOfNewShowtime = startTimeOfNewShowtime + movie_duration;

                        if (startTimeOfNewShowtime < endTimeOfExistingShowtime && endTimeOfNewShowtime > startTimeOfExistingShowtime)
                        {
                            hasTimeConflict = true;
                            break;
                        }
                    }
                }

                if (!hasTimeConflict)
                {
                    var _showtime = await _unitOfWork.Showtime.GetFirstOrDefaultAsync(u => u.ShowtimeID == updatedShowtime.ShowtimeID);
                    _showtime.RoomID = updatedShowtime.RoomID;
                    _showtime.Date = updatedShowtime.Date;
                    _showtime.Time = updatedShowtime.Time;
                    _showtime.Minute = updatedShowtime.Minute;
                    _showtime.MovieID = updatedShowtime.MovieID;

                    _unitOfWork.Showtime.Update(_showtime);
                    _unitOfWork.Save();
                    TempData["msg"] = "Update showtime successfully.";

                    return RedirectToAction("Index");
                }
                else
                {

                    ViewData["ErrorMessage"] = "Start time is not valid.";
                    TempData["error"] = "Start time is not valid.";


                    var cinemas = await _unitOfWork.Cinema.GetAllAsync();
                    var movies = await _unitOfWork.Movie.GetAllAsync();

                    showtimeVM.CinemaList = cinemas.Select(u => new SelectListItem
                    {
                        Text = u.CinemaName,
                        Value = u.CinemaID.ToString(),
                    });

                    showtimeVM.MovieList = movies.Select(u => new SelectListItem
                    {
                        Text = u.MovieName,
                        Value = u.MovieID.ToString(),
                    });

                    return View(showtimeVM);
                }
            }

            TempData["msg"] = "Update showtime successfully.";
            return RedirectToAction("Index");
        }


        [HttpPost]
        #region API Calls
        [HttpGet]
        public async Task<IActionResult> GetAllShowtime()
        {
            var showtimes = await _unitOfWork.Showtime.GetAllAsync(includeProperties: "Movie,Room");

            // Retrieve cinema names for each showtime
            var showtimesWithCinemaName = new List<object>();
            foreach (var showtime in showtimes)
            {
                var cinemaID = showtime.Room.CinemaID;
                var movieID = showtime.Room.CinemaID;

                var cinema = await _unitOfWork.Cinema.GetFirstOrDefaultAsync(u => u.CinemaID == cinemaID);
                var movie = await _unitOfWork.Movie.GetFirstOrDefaultAsync(u => u.MovieID == movieID);

                var showtimeData = new
                {
                    showtimeID = showtime.ShowtimeID,
                    date = showtime.Date,
                    time = showtime.Time,
                    minute = showtime.Minute,
                    movie = showtime.Movie,
                    movie_duration = showtime.Movie.Duration,
                    cinema_name = cinema.CinemaName, // Include cinema name here
                    room = showtime.Room,

                };
                showtimesWithCinemaName.Add(showtimeData);
            }

            return Json(new { data = showtimesWithCinemaName });
        }
        [HttpGet]
        public async Task<IActionResult> GetRoomList(string cinema_id)
        {
            Guid cinemaID = Guid.Parse(cinema_id);

            var roomList = await _unitOfWork.Room.GetAllAsync(u => u.CinemaID == cinemaID);

            return Json(new { data = roomList });
        }
        [HttpDelete]
        public async Task<IActionResult> Delete(Guid showtime_id)
        {
            var showtime = await _unitOfWork.Showtime.GetFirstOrDefaultAsync(u => u.ShowtimeID == showtime_id);
            if (showtime == null)
            {
                return Json(new { success = false, message = "Error while deleting" });
            }
            _unitOfWork.Showtime.Delete(showtime);
            _unitOfWork.Save();
            return Json(new { success = true, message = "Delete showtime successfully! " });
        }


    }
    #endregion
}



