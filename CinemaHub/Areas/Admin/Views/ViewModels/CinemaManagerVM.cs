using CinemaHub.Models;
using System.ComponentModel.DataAnnotations;

namespace CinemaHub.Areas.Admin.Views.ViewModels
{
    public class CinemaManagerVM
    {
        public AppUser CinemaManager { get; set; }

        public string Password { get; set; }

        [Compare("Password")]
        public string ConfirmPassword { get; set; }

    }
}
