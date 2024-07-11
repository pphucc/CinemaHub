using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using CinemaHub.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace CinemaHub.DataAccess.Data
{
	public class DbInitialize
	{
		private readonly AppDbContext _db;
		private readonly UserManager<AppUser> _userManager;
        public DbInitialize(AppDbContext db, UserManager<AppUser> userManager)
        {
            _db = db;
			_userManager = userManager;
        }
		public void AutoMigrate()
		{
            try
            {
                if (_db.Database.GetPendingMigrations().Count() > 0)
                {
                    _db.Database.Migrate();
                }
            }
            catch (Exception ex)
            {

            }
        }
		public async Task SeedCinemaManagerAsync()
		{
			var user = new AppUser
			{
				UserName = "manager@gmail.com",
				NormalizedUserName = "customer@gmail.com".ToUpper(),
				Email = "manager@gmail.com",
				NormalizedEmail = "manager@gmail.com".ToUpper(),
				EmailConfirmed = true,
				LockoutEnabled = false,
				SecurityStamp = Guid.NewGuid().ToString(),
				PhoneNumber = "0898234369",
				FirstName = "Manager",
				LastName = "Manager"
            };
			var roleStore = new RoleStore<IdentityRole>(_db);
			if (!_db.Roles.Any(r => r.Name == "cinemaManager"))
			{
				await roleStore.CreateAsync(new IdentityRole { Name = "cinemaManager", NormalizedName = "CINEMAMANAGER" });
			}
			if (!_db.Roles.Any(r => r.Name == "customer"))
			{
				await roleStore.CreateAsync(new IdentityRole { Name = "customer", NormalizedName = "CUSTOMER" });
			}
			if (!_db.Users.Any(u => u.UserName == user.UserName))
			{
				var password = new PasswordHasher<AppUser>();
				var hashed = password.HashPassword(user, "Manager0123456789!");
				user.PasswordHash = hashed;
				var userStore = new UserStore<AppUser>(_db);
				await _userManager.CreateAsync(user, "Manager0123456789!");
				await _userManager.AddToRoleAsync(user, "cinemaManager");
			}
		}

        public async Task SeedAdminAccountsAsync()
        {
			var user = new AppUser
			{
				UserName = "admin@gmail.com",
				NormalizedUserName = "admin@gmail.com".ToUpper(),
				Email = "admin@gmail.com",
				NormalizedEmail = "admin@gmail.com".ToUpper(),
				EmailConfirmed = true,
				LockoutEnabled = false,
				SecurityStamp = Guid.NewGuid().ToString(),
				PhoneNumber = "0981995925",
				FirstName = "Admin",
				LastName = "Admin"
			};
			var roleStore = new RoleStore<IdentityRole>(_db);
			if (!_db.Roles.Any(r => r.Name == "admin"))
			{
				await roleStore.CreateAsync(new IdentityRole { Name = "admin", NormalizedName = "ADMIN" });
			}
			if (!_db.Roles.Any(r => r.Name == "customer"))
			{
				await roleStore.CreateAsync(new IdentityRole { Name = "customer", NormalizedName = "CUSTOMER" });
			}
			if (!_db.Users.Any(u => u.UserName == user.UserName))
			{
				var password = new PasswordHasher<AppUser>();
				var hashed = password.HashPassword(user, "Admin0123456789!");
				user.PasswordHash = hashed;
				var userStore = new UserStore<AppUser>(_db);
				await _userManager.CreateAsync(user, "Admin0123456789!");
				await _userManager.AddToRoleAsync(user, "admin");
			}
		}
    }
}
