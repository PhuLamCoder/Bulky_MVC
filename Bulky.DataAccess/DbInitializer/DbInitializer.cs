using BulkyBook.DataAccess.Data;
using BulkyBook.Models;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

#nullable disable

namespace BulkyBook.DataAccess.DbInitializer
{
    public class DbInitializer : IDbInitializer
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _db;

        public DbInitializer(UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext db)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _db = db;
        }

        public async Task InitializeAsync()
        {
            // Migrations if they are not applied
            try
            {
                if ((await _db.Database.GetPendingMigrationsAsync()).Count() > 0)
                {
                    await _db.Database.MigrateAsync();
                }
            }
            catch (Exception ex)
            {

            }

            // Create roles if they are not created
            if (!_roleManager.RoleExistsAsync(SD.Role_Customer).GetAwaiter().GetResult())
            {
                await _roleManager.CreateAsync(new IdentityRole(SD.Role_Customer));
                await _roleManager.CreateAsync(new IdentityRole(SD.Role_Company));
                await _roleManager.CreateAsync(new IdentityRole(SD.Role_Admin));
                await _roleManager.CreateAsync(new IdentityRole(SD.Role_Employee));

                // If roles are not created, then we will create admin user as well
                await _userManager.CreateAsync(new ApplicationUser
                {
                    UserName = "tranmyphulam123@gmail.com",
                    Email = "tranmyphulam123@gmail.com",
                    Name = "PLamUS",
                    PhoneNumber = "3504925794",
                    StreetAddress = "50 Avenue",
                    State = "No",
                    PostalCode = "APTX4869",
                    City = "Go Cong"
                }, "Phulam123@");

                ApplicationUser user = await _db.ApplicationUsers.FirstOrDefaultAsync(user => user.UserName == "tranmyphulam123@gmail.com");
                await _userManager.AddToRoleAsync(user, SD.Role_Admin);
            }

            return;
        }
    }
}
