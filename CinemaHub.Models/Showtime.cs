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
	public class Showtime
	{
        [Key]
        public Guid ShowtimeID { get; set; }

        public DateOnly Date { get; set; }

        public int Time { get; set; }

        public int Minute { get; set; }

        public Guid MovieID { get; set; }

		[ForeignKey("MovieID")]
		[ValidateNever]
		public Movie Movie { get; set; }

		public Guid RoomID { get; set; }
		[ForeignKey(nameof(RoomID))]
		[ValidateNever]
		public Room Room { get; set; }
	}
}
