using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using UserService.Core.Domain.IdentityEntities;

namespace UserService.Infrastructure.DbContext
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
    {
        public ApplicationDbContext(DbContextOptions options) : base(options)
        {

        }


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<ApplicationUser>().ToTable("Users"); 
            builder.Entity<ApplicationRole>().ToTable("Roles");
            builder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles"); // Liên kết User - Role
            builder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
            builder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
            builder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
            builder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");

            builder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(u => u.FullName).HasMaxLength(255);
                entity.Property(u => u.AvatarUrl).HasMaxLength(500);
                entity.Property(u => u.RefreshToken).HasMaxLength(500);
                entity.Property(u => u.RefreshTokenExpiryTime).HasColumnType("datetime");
            });

            builder.Entity<ApplicationRole>(entity =>
            {
                entity.Property(r => r.Description).HasMaxLength(500);
            });

            // Seed Roles
            var roles = LoadSeedData<ApplicationRole>("SeedData/Roles.json");
            builder.Entity<ApplicationRole>().HasData(roles);
            // Seed Account
            var accounts = LoadSeedData<ApplicationRole>("SeedData/Accounts.json");
            builder.Entity<ApplicationRole>().HasData(accounts);

        }

        private static List<T> LoadSeedData<T>(string filePath)
        {
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "UserService.Infrastructure", filePath);
            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"Không tìm thấy file seed data: {fullPath}");

            var jsonData = File.ReadAllText(fullPath);
            return JsonSerializer.Deserialize<List<T>>(jsonData) ?? new List<T>();
        }
    }
}
