using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CinemaHub.Models
{
	public class Comment
	{
		[Key]
        public Guid CommentID { get; set; }

        public string Content { get; set; }

		public Guid MovieID { get; set; }

		[ForeignKey("MovieID")]
		[ValidateNever]
		public Movie Movie { get; set; }

        public string AppUserID { get; set; }
        [ForeignKey(nameof(AppUserID))]
        [ValidateNever]
        public AppUser AppUser { get; set; }


    }
}
