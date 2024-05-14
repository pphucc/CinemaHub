using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaHub.Models
{
	public class Movie
	{
        [Key]
        public Guid MovieID { get; set; }
        public string? MovieName { get; set; } = string.Empty;
        public string? Description { get; set; } = string.Empty;
        public string? Actor { get; set; } = string.Empty;
        public string? Director { get; set; } = string.Empty;
		public double Price {get; set; }
        public string? ImageUrl { get; set; } = string.Empty;
		public string? VideoUrl { get; set; } = string.Empty;
		public int Duration { get; set; }
        [ValidateNever]
        public DateOnly ReleaseDate { get; set; }
        [ValidateNever]
        public DateOnly EndDate { get; set; }
        public string? Country { get; set; } = string.Empty;
		public string? Studio { get; set; } = string.Empty;
		public string? Version { get; set; } = string.Empty;
        public string? Category { get; set; } = string.Empty;
      
    }
}
