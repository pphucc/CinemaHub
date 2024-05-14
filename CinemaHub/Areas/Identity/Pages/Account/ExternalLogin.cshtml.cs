// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using CinemaHub.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CinemaHub.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ExternalLoginModel : PageModel
    {
        private readonly SignInManager<AppUser> _signInManager;
        private readonly UserManager<AppUser> _userManager;
        private readonly IUserStore<AppUser> _userStore;
        private readonly IUserEmailStore<AppUser> _emailStore;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<ExternalLoginModel> _logger;

        public ExternalLoginModel(
            SignInManager<AppUser> signInManager,
            UserManager<AppUser> userManager,
            IUserStore<AppUser> userStore,
            ILogger<ExternalLoginModel> logger,
            IEmailSender emailSender)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _logger = logger;
            _emailSender = emailSender;
        }

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
        public string ProviderDisplayName { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string ErrorMessage { get; set; }

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
            [Required]
            [EmailAddress]
            public string Email { get; set; }
        }

        public IActionResult OnGet()
        {
            return RedirectToPage("./Login");
        }

        public async Task<IActionResult> OnPostAsync(string provider, string returnUrl = null)
        {
            // Request a redirect to the external login provider.
            var redirectUrl = Url.Page("./ExternalLogin", pageHandler: "Callback", values: new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);


			// Kiểm tra yêu cầu dịch vụ provider tồn tại
			var listprovider = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
			var provider_process = listprovider.Find(m => m.Name == provider);
			if (provider_process == null)
			{
				return NotFound("Serivice is not available " + provider);
			}			

			// Chuyển hướng đến dịch vụ ngoài (Googe, Facebook)
			return new ChallengeResult(provider, properties);
			
        }

        public async Task<IActionResult> OnGetCallbackAsync(string returnUrl = null, string remoteError = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");
            if (remoteError != null)
            {
                ErrorMessage = $"Error from external provider: {remoteError}";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                ErrorMessage = "Error loading external login information.";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            // Sign in the user with this external login provider if the user already has a login.
            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
            if (result.Succeeded)
            {
                // Account existed with login Provider (Google) -> Login (Store in UserLogins Table )
                _logger.LogInformation("{Name} logged in with {LoginProvider} provider.", info.Principal.Identity.Name, info.LoginProvider);
                return LocalRedirect(returnUrl);
            }
            if (result.IsLockedOut)
            {
                
                return RedirectToPage("./Lockout");
            }
            else
			{
                // Co tai khoan, chua lien ket => Lien ket tai khoan
			    // Chua co tai khoan => Tao tai khoan, lien ket, dang nhap
			    // If the user does not have an account, then ask the user to create an account.
				ReturnUrl = returnUrl;
                ProviderDisplayName = info.ProviderDisplayName;
                if (info.Principal.HasClaim(c => c.Type == ClaimTypes.Email))
                {
                    Input = new InputModel
                    {
                        Email = info.Principal.FindFirstValue(ClaimTypes.Email)
                    };
                }
                return Page();
            }
        }

        public async Task<IActionResult> OnPostConfirmationAsync(string returnUrl = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");
            // Get the information about the user from the external login provider
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                ErrorMessage = "Error loading external login information during confirmation.";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            if (ModelState.IsValid)
            {
                // User registered with email from Input.Email (in database)
				var registeredUser = await _userManager.FindByNameAsync(Input.Email);
                string externalEmail = null;
                // User which has the same email with email from Google Provider 
                AppUser externalEmailUser = null;

				if (info.Principal.HasClaim(c => c.Type == ClaimTypes.Email))
                {					
					externalEmail = info.Principal.FindFirstValue(ClaimTypes.Email);
                }
                if (externalEmail != null)
                {
					externalEmailUser = await _userManager.FindByEmailAsync(externalEmail);
                }

                // Registered user is the same with ExternalEmailUser
				if (registeredUser != null &&  externalEmailUser != null)
                {
                    if(registeredUser.Id == externalEmailUser.Id)
                    {
                        var resultLink = await _userManager.AddLoginAsync(registeredUser, info);
                        if (resultLink.Succeeded)
                        {
                            await _signInManager.SignInAsync(registeredUser,isPersistent: false);
                            return LocalRedirect(returnUrl);
						}
					} else
                    {
						ModelState.AddModelError(string.Empty, "Can not associate this email, try another email!");
                        return Page();
					}
				}
                // Da co tai khoản khác sử dụng tên email từ info, nhưng không có tài khoản với chính email đó
				if (registeredUser == null && externalEmailUser != null)
				{
					ModelState.AddModelError(string.Empty, "Not support create new account which is different from your Google account, try another one!");
					return Page();
				}
				// Chưa có tài khoản nào liên kết với email từ info, 
				if (externalEmailUser == null && externalEmail == Input.Email)
                {
					var newUser = CreateUser();

					await _userStore.SetUserNameAsync(newUser, Input.Email, CancellationToken.None);
					await _emailStore.SetEmailAsync(newUser, Input.Email, CancellationToken.None);
                    newUser.FirstName = info.Principal.FindFirstValue(ClaimTypes.Name);
                    newUser.PhoneNumber = info.Principal.FindFirstValue(ClaimTypes.HomePhone);
					var resultNewUser = await _userManager.CreateAsync(newUser);
					if (resultNewUser.Succeeded)
					{
						var res = await _userManager.AddLoginAsync(newUser, info);
                        await _userManager.AddToRoleAsync(newUser, "customer");
						if (res.Succeeded)
						{
							_logger.LogInformation("User created an account using {Name} provider.", info.LoginProvider);

							var userId = await _userManager.GetUserIdAsync(newUser);
							var code = await _userManager.GenerateEmailConfirmationTokenAsync(newUser);
							code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
							var callbackUrl = Url.Page(
								"/Account/ConfirmEmail",
								pageHandler: null,
								values: new { area = "Identity", userId = userId, code = code },
								protocol: Request.Scheme);

							await _emailSender.SendEmailAsync(Input.Email, "Confirm your email",
								$"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

							// If account confirmation is required, we need to show the link if we don't have a real email sender
							if (_userManager.Options.SignIn.RequireConfirmedEmail)
							{
								return RedirectToPage("./RegisterConfirmation", new { Email = Input.Email });
							}

							await _signInManager.SignInAsync(newUser, isPersistent: false, info.LoginProvider);
							return LocalRedirect(returnUrl);
						}
					}
				}			
            }

            ProviderDisplayName = info.ProviderDisplayName;
            ReturnUrl = returnUrl;
            return Page();
        }

        private AppUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<AppUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(AppUser)}'. " +
                    $"Ensure that '{nameof(AppUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    $"override the external login page in /Areas/Identity/Pages/Account/ExternalLogin.cshtml");
            }
        }

        private IUserEmailStore<AppUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<AppUser>)_userStore;
        }
    }
}
