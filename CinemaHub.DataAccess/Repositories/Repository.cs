using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using CinemaHub.DataAccess.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace CinemaHub.DataAccess.Repositories
{
	public class Repository<T> : IRepository<T> where T : class
	{
		private readonly AppDbContext _db;
		private readonly DbSet<T> _dbSet;
		public Repository(AppDbContext db)
        {
			_db = db;
			_dbSet = db.Set<T>();
		}
        public T Add(T _object)
		{
			try
			{
				 _dbSet.Add(_object);
			}
			catch (Exception ex)
			{
				
				throw;
			}
			return _object;
		}
		public T Delete(T _object)
		{
			try
			{
				 _dbSet.Remove(_object);
			}
			catch (Exception ex)
			{
				
				throw;
			}
			return _object;
		}

		public async Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>>? filter = null, string? includeProperties = null)
		{
			IQueryable<T> query = _dbSet;
			try
			{				
				if (filter != null)
				{
					query = query.Where(filter);
				}
				if (includeProperties != null)
				{
					foreach (var includeProp in includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
					{
						query = query.Include(includeProp);
					}
				}
			}
			catch (Exception ex)
			{
				
				throw;
			}
			
			return await query.ToListAsync();
		}

		public async Task<T> GetFirstOrDefaultAsync(Expression<Func<T, bool>> filter, string? includeProperties = null)
		{
			IQueryable<T> query = _dbSet;
			try
			{
				query = query.Where(filter);
				if (includeProperties != null)
				{
					foreach (var includeProp in includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
					{
						query = query.Include(includeProp);
					}
				}
			}
			catch (Exception ex)
			{
				
				throw;
			}
			
			return await query.FirstOrDefaultAsync();
		}

		public T Update(T _object)
		{

			try
			{
				_db.Update(_object);
			}
			catch (Exception ex)
			{
				
				throw;
			}
			return _object;
		}
	}
}
