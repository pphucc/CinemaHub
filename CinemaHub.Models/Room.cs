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
	public class Room
	{
        public Guid RoomID { get; set; }
        [Required]
        public string RoomName { get; set; }
        public bool Status { get; set; }
        public Guid CinemaID { get; set; }
        [ForeignKey("CinemaID")]
        [ValidateNever]
        public Cinema Cinema { get; set; }

        public string? Note { get; set; } = string.Empty;

        [Required]
        public int NumOfSeats { get; set; }
      
    }
}
