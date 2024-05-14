using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using CinemaHub.Areas.Admin.Views.ViewModels;
using CinemaHub.DataAccess.Data;
using CinemaHub.DataAccess.Repositories;
using CinemaHub.Models;
using CinemaHub.Services;
using System.Drawing;

namespace CinemaHub.Areas.Admin.Controllers
{
	[Area("Admin")]
	[Authorize(Roles = "admin")]
	public class DashboardController : Controller
	{
		private readonly UserManager<AppUser> _userManager;
		private readonly IEmailSender _emailSender;
		private readonly IUnitOfWork _unitOfWork;
		private readonly UploadImageService _uploadImageService;
        public DashboardController(UserManager<AppUser> userManager, IEmailSender emailSender, IUnitOfWork unitOfWork, UploadImageService uploadImageService)
        {
           _userManager = userManager;
			_emailSender = emailSender;
			_unitOfWork = unitOfWork;
			_uploadImageService = uploadImageService;
        }
		[HttpGet]
		public IActionResult UserAccount()
		{
			return View();
		}
        [HttpGet]
        public IActionResult ManagerAccount()
        {
            return View();
        }
		[HttpGet]
		public IActionResult CreateManagerAccount()
		{
			return View();
		}
		[HttpPost]
		public async Task<IActionResult> CreateManagerAccount(CinemaManagerVM model, IFormFile? file)
		{
            var cinemaManager = new AppUser
            {
                UserName =  model.CinemaManager.Email,
                NormalizedUserName = model.CinemaManager.Email.ToUpper(),
                Email = model.CinemaManager.Email,
                NormalizedEmail = model.CinemaManager.Email.ToUpper(),
                EmailConfirmed = true,
                LockoutEnabled = false,
                SecurityStamp = Guid.NewGuid().ToString(),
                PhoneNumber = model.CinemaManager.PhoneNumber,
                FirstName = model.CinemaManager.FirstName,
                LastName = model.CinemaManager.LastName
            };
            var password = new PasswordHasher<AppUser>();
            var hashed = password.HashPassword(cinemaManager, model.Password);
            cinemaManager.PasswordHash = hashed;
            if (file != null)
			{
				model.CinemaManager.Avatar = _uploadImageService.UploadImage(file, @"images\avatar");
               
			}
             var result = await _userManager.CreateAsync(cinemaManager,model.Password);
			if (result.Succeeded)
			{
				await _userManager.AddToRoleAsync(cinemaManager, "cinemaManager");
                TempData["msg"] = "Create Manager Account successfully";
            }			
            return RedirectToAction("ManagerAccount");
		}
        [HttpGet]
		public IActionResult Dashboard()
		{
			return View();
		}
		#region API Calls 
		[HttpGet]
		public async Task<IActionResult> GetCustomerUsers()
		{
			var customerUsers = await _userManager.GetUsersInRoleAsync("customer");							  
			return Json(new {data = customerUsers});
		}
        [HttpGet]
        public async Task<IActionResult> GetManagerUsers()
        {
            var managers = await _userManager.GetUsersInRoleAsync("cinemaManager");
            return Json(new { data = managers });
        }
		[HttpGet]
		public async Task<IActionResult> UpdateManagerAccount(Guid user_id)
		{
			var user = await _userManager.FindByIdAsync(user_id.ToString());
			CinemaManagerVM cinemaManagerVM = new CinemaManagerVM
			{
				CinemaManager = user,
			};
			return View(cinemaManagerVM);
		}
		[HttpPost]
		public async Task<IActionResult> UpdateManagerAccount(CinemaManagerVM model, IFormFile? file)
		{
			var _user = await _userManager.FindByIdAsync(model.CinemaManager.Id);
			if (_user != null)
			{
				_user.UserName = model.CinemaManager.Email;
				_user.PhoneNumber = model.CinemaManager.PhoneNumber;
				_user.FirstName = model.CinemaManager.FirstName;
				_user.LastName = model.CinemaManager.LastName;
                var password = new PasswordHasher<AppUser>();
                var hashed = password.HashPassword(_user, model.Password);
                _user.PasswordHash = hashed;
				_user.DOB = model.CinemaManager.DOB;
				_user.Email = model.CinemaManager.Email;
                if (file != null)
                {
                    _user.Avatar = _uploadImageService.UploadImage(file, @"images\avatar\", _user.Avatar);
                }
                var result = await _userManager.UpdateAsync(_user);
                if (result.Succeeded)
                {
                    TempData["msg"] = "Update Manager Account successfully";
                }
            }						
            return RedirectToAction("ManagerAccount");
        }
        [HttpPost]
		public async Task<IActionResult> LockAccount(string user_id)
		{
			var user = await _userManager.FindByIdAsync(user_id);
			if (user != null)
			{
				await _userManager.SetLockoutEnabledAsync(user, true);
				await _userManager.SetLockoutEndDateAsync(user, DateTime.Now + TimeSpan.FromDays(10000));
				await _emailSender.SendEmailAsync(user.Email, "Lock Account", "Your account has been locked");
				return Json(new { success = true, message = "Lock successfully!" });
			}
			return Json(new { success = false, message = "Fail to lock this account" });
		}
		[HttpPost]
		public async Task<IActionResult> UnlockAccount(string user_id)
		{
			var user = await _userManager.FindByIdAsync(user_id);
			if (user != null)
			{
				await _userManager.SetLockoutEndDateAsync(user, DateTime.Now - TimeSpan.FromMinutes(10000));
				await _emailSender.SendEmailAsync(user.Email, "Unlock Account", "Your account has been unlocked");
				return Json(new { success = true, message = "UnLock successfully!" });
			}
			return Json(new { success = false, message = "Fail to unlock this account" });
		}

		[HttpGet]
		public async Task<IActionResult> GetAllTickets()
		{
			var tickets = await _unitOfWork.Ticket.GetAllAsync();
			return Json(new {data = tickets});
		}

		[HttpGet]
		public async Task<IActionResult> GetTrendingMovies(string? filter = null)
		{
			IEnumerable<Ticket> tickets = null ;
			if (filter != null)
			{
				switch (filter)
				{
					case "Day":
                        tickets = await _unitOfWork.Ticket.GetAllAsync(u => u.BookedDate.Value.Date == DateTime.Now.Date );
                        break;
                    case "Month":
                        tickets = await _unitOfWork.Ticket.GetAllAsync(u => u.BookedDate.Value.Month == DateTime.Now.Month && u.BookedDate.Value.Year == DateTime.Now.Year);
                        break;
                    case "Year":
                        tickets = await _unitOfWork.Ticket.GetAllAsync(u => u.BookedDate.Value.Year == DateTime.Now.Year);
						break;
					case "History":
                        tickets = await _unitOfWork.Ticket.GetAllAsync();
						break;
                }
            } else
			{
				tickets = await _unitOfWork.Ticket.GetAllAsync();
			}
			var movies = await _unitOfWork.Movie.GetAllAsync();
			var showtimes = await _unitOfWork.Showtime.GetAllAsync();

			var data = from ticket in tickets
								 join showtime in showtimes on ticket.ShowtimeID equals showtime.ShowtimeID
								 join movie in movies on showtime.MovieID equals movie.MovieID
								 select new { movie = movie.MovieName, revenue = ticket.Total };

			var trendingMovies = data.GroupBy( u => u.movie)
				                            .Select(g => new { Movie = g.Key, TotalRevenue = g.Sum(u => u.revenue) })
											.OrderByDescending(u => u.TotalRevenue)
											.Take(3)
											.ToList();

			return Json(new { data = trendingMovies });
		}
		[HttpGet]
		public async Task<IActionResult> GetTotalCustomers()
		{
			var customer = await _userManager.GetUsersInRoleAsync("customer");
			return Json(new {count =  customer.Count});
		}
        [HttpGet]
        public async Task<IActionResult> GetTotalTickets()
        {
			var tickets = await _unitOfWork.Ticket.GetAllAsync();
			var total = tickets.Count();
            return Json(new {totalTickets = total});
        }
        [HttpGet]
        public async Task<IActionResult> GetTotalRevenue()
        {
            var tickets = await _unitOfWork.Ticket.GetAllAsync();
            var total = tickets.Sum( u => u.Total);
            return Json(new { totalRevenue = total });
        }
        #endregion

    }
}
