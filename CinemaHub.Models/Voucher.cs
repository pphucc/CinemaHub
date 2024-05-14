using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CinemaHub.Models
{
	public class Voucher
	{
        public Guid VoucherID { get; set; }

        public string? VoucherName { get; set; } = string.Empty;

        [Required] 
		[Range(1, 100)]
		public double Value { get; set; }

        [Required]
        public int Quantity { get; set; }

        //public virtual Ticket Ticket { get; set; }
    }
}
