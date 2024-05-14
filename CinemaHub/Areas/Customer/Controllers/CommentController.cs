using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using CinemaHub.DataAccess.Repositories;
using CinemaHub.Models;
using System.Security.Claims;

namespace CinemaHub.Areas.Customer.Controllers
{
    [Area("Customer")]
    [AllowAnonymous]
    public class CommentController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<AppUser> _userManager;
        public CommentController(IUnitOfWork unitOfWork, UserManager<AppUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }
        [HttpGet]
        public async Task<IActionResult> GetComments(Guid? movieId)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(claim.Value);
            var user_role = _userManager.GetRolesAsync(user).Result.FirstOrDefault();
            var comments = await _unitOfWork.Comment.GetAllAsync(u => u.MovieID == movieId, includeProperties: "AppUser");
            if (claim != null)
            {

                return Json(new { data = comments, user = claim.Value, role = user_role });
            }
            else
            {

                return Json(new { data = comments });
            }
        }
        public async Task<IActionResult> AddComment(Guid movieId, string text)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(claim.Value);

            Comment comment = new Comment
            {
                AppUserID = claim.Value,
                Content = text,
                MovieID = movieId
            };

            _unitOfWork.Comment.Add(comment);
            _unitOfWork.Save();
            return Json(new { });
        }
        [HttpPost]
        public async Task<IActionResult> UpdateComment(Guid commentId, string newText)
        {
            var comment = await _unitOfWork.Comment.GetFirstOrDefaultAsync(u => u.CommentID == commentId);

            if (comment == null)
            {
                return NotFound("Comment not found.");
            }

            // Update the comment text
            comment.Content = newText;

            _unitOfWork.Comment.Update(comment);
            _unitOfWork.Save();

            return Json(new { });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteComment(Guid commentId)
        {
            var comment = await _unitOfWork.Comment.GetFirstOrDefaultAsync(u => u.CommentID == commentId);

            if (comment == null)
            {
                return NotFound("Comment not found.");
            }

            _unitOfWork.Comment.Delete(comment);
            _unitOfWork.Save();

            return Json(new { });
        }

    }


}
