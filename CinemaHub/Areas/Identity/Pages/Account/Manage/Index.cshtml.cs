// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CinemaHub.DataAccess.Repositories;
using CinemaHub.Models;
using CinemaHub.Services;

namespace CinemaHub.Areas.Identity.Pages.Account.Manage
{
	public class IndexModel : PageModel
	{
		private readonly UserManager<AppUser> _userManager;
		private readonly SignInManager<AppUser> _signInManager;
		private readonly UploadImageService _uploadImageService;
		public IndexModel(
			UserManager<AppUser> userManager,
			SignInManager<AppUser> signInManager,
			UploadImageService uploadImageService
		)
		{
			_userManager = userManager;
			_signInManager = signInManager;
			_uploadImageService = uploadImageService;
		}

		/// <summary>
		///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
		///     directly from your code. This API may change or be removed in future releases.
		/// </summary>
		public string Username { get; set; }

		/// <summary>
		///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
		///     directly from your code. This API may change or be removed in future releases.
		/// </summary>
		[TempData]
		public string StatusMessage { get; set; }

		/// <summary>
		///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
		///     directly from your code. This API may change or be removed in future releases.
		/// </summary>
		[BindProperty]
		public InputModel Input { get; set; }

		/// <summary>
		///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
		///     directly from your code. This API may change or be removed in future releases.
		/// </summary>		
		public class InputModel
		{
			/// <summary>
			///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
			///     directly from your code. This API may change or be removed in future releases.
			/// </summary>
			[Phone]
			[Display(Name = "Phone number")]
			public string PhoneNumber { get; set; }

			[Display(Name = "First Name")]
			public string FirstName { get; set; }
			[Display(Name = "Last Name")]
			public string LastName { get; set; }
			[Display(Name = "Date of Birth")]
			public DateOnly DOB { get; set; }
			[Display(Name = "Your Avatar")]
			public IFormFile Avatar { get; set; }
		}

		private async Task LoadAsync(AppUser user)
		{
			var userName = await _userManager.GetUserNameAsync(user);
			var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
			var firstName = user.FirstName;
			var lastName = user.LastName;
			var DOB = user.DOB;
			Username = userName;
			ViewData["avatar"] = user.Avatar;
			Input = new InputModel
			{
				PhoneNumber = phoneNumber,
				FirstName = firstName,
				LastName = lastName,
				DOB = DOB,
			};
		}

		public async Task<IActionResult> OnGetAsync()
		{
			var user = await _userManager.GetUserAsync(User);
			if (user == null)
			{
				return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
			}

			await LoadAsync(user);
			return Page();
		}

		public async Task<IActionResult> OnPostAsync()
		{
			var user = await _userManager.GetUserAsync(User);
			if (user == null)
			{
				return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
			}

			if (!ModelState.IsValid)
			{
				await LoadAsync(user);
				return Page();
			}

			var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
			if (Input.PhoneNumber != phoneNumber)
			{
				var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
				if (!setPhoneResult.Succeeded)
				{
					StatusMessage = "Unexpected error when trying to set phone number.";
					return RedirectToPage();
				}
			}

			if (Input.FirstName != user.FirstName)
			{
				user.FirstName = Input.FirstName;

			}
			if (Input.LastName != user.LastName  && Input.LastName  is not null)
			{
				user.LastName = Input.LastName;
			}
			if (Input.DOB != user.DOB)
			{
				user.DOB = Input.DOB;
			}
			if (Input.Avatar != null)
			{
				user.Avatar = _uploadImageService.UploadImage(Input.Avatar, @"images\avatar",user.Avatar);
			}
			
			await _signInManager.RefreshSignInAsync(user);
			await _userManager.UpdateAsync(user);
			StatusMessage = "Your profile has been updated";
			return RedirectToPage();
		}
	}
}
