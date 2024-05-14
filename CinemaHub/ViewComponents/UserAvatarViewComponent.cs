using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using CinemaHub.Models;
namespace CinemaHub.ViewComponents
{
	[ViewComponent]
	public class UserAvatarViewComponent : ViewComponent 
	{
		private readonly UserManager<AppUser> _userManager;
        public UserAvatarViewComponent( UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }
        public async Task<IViewComponentResult> InvokeAsync(string? fullName = null)
		{

			var user = await _userManager.GetUserAsync(HttpContext.User);
			if (user != null)
			{
				if (fullName != null)
				{
					return View("FullName",$"{user.FirstName} {user.LastName}");
				}
				return View(user);
			}
			return View();
		}
	}
}
