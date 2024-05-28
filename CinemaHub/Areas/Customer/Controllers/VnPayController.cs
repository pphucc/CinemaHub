using CinemaHub.DataAccess.Repositories;
using CinemaHub.Models;
using CinemaHub.Services.IServices;
using CinemaHub.Utils;
using CinemaHub.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CinemaHub.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize(Roles = "customer,admin,cinemaManager")]
    public class VnPayController : Controller
    {
        private static IVnPayService _vnPayService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<AppUser> _userManager;
        public VnPayController(IUnitOfWork unitOfWork, UserManager<AppUser> userManager, IVnPayService vnPayService)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _vnPayService = vnPayService;
        }

        public async Task<IActionResult> AuthorizePayment(string? option, string? amount_purchase)
        {
            try
            {
                var session = HttpContext.Session;
                var amount = session.GetString("totalAmount");

                if (option == "point")
                {
                    session.SetString("option", option);
                    session.SetString("amount_purchase", amount_purchase);
                    amount = amount_purchase;
                }

                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
                var user = await _userManager.FindByIdAsync(claim.Value);

                var request = new VnPaymentRequestModel
                {
                    Amount = double.Parse(amount),
                    CreatedDate = DateTime.Now,
                    FullName = $"{user.FirstName} {user.LastName}",
                    Description = $"{user.FirstName} {user.LastName}",
                    OrderId = new Random().Next(1000, 10000).ToString()
                };


                return Redirect(_vnPayService.CreatePaymentUrl(HttpContext, request));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        public async Task<IActionResult> ExecutePaymentAsync()
        {
            var response = _vnPayService.PaymentExecute(Request.Query);

            if (response.VnPayResponseCode == "00")
            {
                if (HttpContext.Session.GetString("option") == "point")
                {
                    var claimsIdentity = (ClaimsIdentity)User.Identity;
                    var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
                    var user = await _userManager.FindByIdAsync(claim.Value);
                    user.Point += decimal.Parse(HttpContext.Session.GetString("amount_purchase")) / 1000;

                    _unitOfWork.Save();
                    TempData["msg"] = "You payment completed successfully." + decimal.Parse(HttpContext.Session.GetString("amount_purchase")) / 1000 + " points has been added to your account.";

                    HttpContext.Session.Remove("option");
                    HttpContext.Session.Remove("amount_purchase");
                    return RedirectToAction("Index", "Wallet");
                }
                else
                {
                    return RedirectToAction("CompleteBooking", "Booking");
                }
            }
            else
            {
                if (HttpContext.Session.GetString("option") == "point")
                {
                    TempData["error"] = "You payment was cancelled.";

                    HttpContext.Session.Remove("option");
                    HttpContext.Session.Remove("amount_purchase");
                    return RedirectToAction("Index", "Wallet");
                }
                //return RedirectToAction("Error", "Home");
                return RedirectToAction("Cancel", "Ticket");
            }
        }
    }
}
