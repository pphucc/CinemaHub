using Microsoft.Extensions.Logging;
using CinemaHub.DataAccess.Data;
using CinemaHub.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CinemaHub.DataAccess.Repositories
{
	public class UnitOfWork : IUnitOfWork
	{
		private readonly AppDbContext _db;
		public UnitOfWork(AppDbContext db)
        {
			_db = db;
			
			Cinema = new CinemaRepository(_db);
			Movie = new MovieRepository(_db);
			Seat = new SeatRepository(_db);
			Ticket = new TicketRepository(_db);
			Room = new RoomRepository(_db);
			Comment = new CommentRepository(_db);
			Showtime = new ShowtimeRepository(_db);
			Promotion = new PromotionRepository(_db);
			Voucher = new VoucherRepository(_db);
		}
        public IRepository<Cinema> Cinema { get; private set; }
		public IRepository<Movie> Movie { get; private set; }
		public IRepository<Seat> Seat { get; private set; }
		public IRepository<Ticket> Ticket { get; private set; }
		public IRepository<Room> Room { get; private set; }
		public IRepository<Comment> Comment { get; private set; }
		public IRepository<Showtime> Showtime { get; private set; }
		public IRepository<Promotion> Promotion { get; private set; }
		public IRepository<Voucher> Voucher { get; private set; }
		public void Save() { 

			 _db.SaveChanges();
		}
	}
}
