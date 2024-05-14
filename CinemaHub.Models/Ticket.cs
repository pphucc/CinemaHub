using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CinemaHub.Models
{
	public class Ticket
	{
		[Key]
        public Guid TicketID { get; set; }
       
		public Guid ShowtimeID { get; set; }
		[ForeignKey(nameof(ShowtimeID))]
		[ValidateNever]
		public Showtime Showtime { get; set; }

		public Guid SeatID { get; set; }
		[ForeignKey(nameof(SeatID))] 
		[ValidateNever]
		public Seat Seat { get; set; }

		public string AppUserID {  get; set; }
		[ForeignKey(nameof(AppUserID))]
		[ValidateNever]
		public AppUser AppUser { get; set; }

		public double Total { get; set; }

		public Guid? VoucherID { get; set; }
		[ForeignKey(nameof(VoucherID))]
		[ValidateNever]
		public Voucher Voucher { get; set; }

		public DateTime? BookedDate { get; set; }

		public string? TicketStatus { get; set; } // Expried, Available

        public Ticket()
        {
			TicketStatus = "Available";
        }
    }
}
