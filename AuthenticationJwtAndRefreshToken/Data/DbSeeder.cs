using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MorCohen.Models.AuthenticateModels;

namespace MorCohen.Data
{
    /// <summary>
    /// Responsible for filling the DB with the relevant data. It should be used when the application is first loaded.
    /// </summary>
    public class DbSeeder
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        public DbSeeder(RoleManager<IdentityRole> roleManager, ApplicationDbContext context)
        {
            _roleManager = roleManager;
            _context = context;
        }

        public async Task Seed()
        {
            await _context.Database.EnsureCreatedAsync();
            string[] roles = new string[] { nameof(IdentityTypes.Role.Admin), nameof(IdentityTypes.Role.User) };
            foreach (var role in roles)
            {
                if (!await _roleManager.RoleExistsAsync(role).ConfigureAwait(false))
                {
                    await _roleManager.CreateAsync(new IdentityRole(role)).ConfigureAwait(false);
                }
            }
        }


    }
}
