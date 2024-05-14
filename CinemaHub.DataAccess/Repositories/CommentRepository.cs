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
	public class CommentRepository : Repository<Comment>
	{
		public CommentRepository(AppDbContext db) : base(db)
		{
		}
	}
}
