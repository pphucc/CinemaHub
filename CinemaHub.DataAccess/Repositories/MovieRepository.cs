using Microsoft.Extensions.Logging;
using CinemaHub.DataAccess.Data;
using CinemaHub.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace CinemaHub.DataAccess.Repositories
{
	public class MovieRepository : Repository<Movie>
	{
		public MovieRepository(AppDbContext db) : base(db)
		{
		}
	}
}
