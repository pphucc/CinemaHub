using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaHub.Models
{
	public class Promotion
	{
		public Guid PromotionID { get; set; }

		public string? Topic { get; set; } = string.Empty;

		public string? Content { get; set; } = string.Empty;

		public DateTime StartDate { get; set; }

		public DateTime EndDate { get; set; }

		[ValidateNever]
		public string? ImageUrl { get; set; }
	}
}
