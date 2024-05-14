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
	public class Seat
	{
		[Key]
        public Guid SeatID { get; set; }

        [Required]
        public string SeatName { get; set; }

        public string SeatStatus { get; set; }

        public Guid RoomID { get; set; }
        [ForeignKey(nameof(RoomID))]
        [ValidateNever]
        public Room Room { get; set; }
    }
}
