using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using CinemaHub.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CinemaHub.DataAccess.Data
{
	public class AppDbContext : IdentityDbContext<AppUser>
	{
		public AppDbContext(DbContextOptions options) : base(options)
		{

		}
		public DbSet<Movie> Movies { get; set; }
		public DbSet<Cinema> Cinemas { get; set; }
		public DbSet<Promotion> Promotions { get; set; }
		public DbSet<Showtime> Showtimes { get; set; }
		public DbSet<Ticket> Tickets { get; set; }
		public DbSet<Room> Rooms { get; set; }
		public DbSet<Seat> Seats { get; set; }
		public DbSet<Comment> Comments { get; set; }
		public DbSet<Voucher> Vouchers { get; set; }
		protected override void OnModelCreating(ModelBuilder builder)
		{

			base.OnModelCreating(builder);
			// Remove prefix AspNet of tables IdentityDbContext: AspNetUserRoles, AspNetUser ...
			foreach (var entityType in builder.Model.GetEntityTypes())
			{
				var tableName = entityType.GetTableName();
				if (tableName.StartsWith("AspNet"))
				{
					entityType.SetTableName(tableName.Substring(6));
				}
			}
			builder.Entity<Ticket>()
				.Property(u => u.VoucherID)
				.IsRequired(false);

		}
	}
}
