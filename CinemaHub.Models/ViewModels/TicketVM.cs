using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CinemaHub.Models.ViewModels
{
	public class TicketVM
	{
		public List<Ticket> Ticket { get; set; } = new List<Ticket>();

		public int Quantity { get; set; }

		public double Total {  get; set; }
	}
}
