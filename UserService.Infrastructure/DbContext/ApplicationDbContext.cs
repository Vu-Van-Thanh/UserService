﻿using System;
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
            

        }

        private static List<T> LoadSeedData<T>(string filePath)
        {
            // string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            //string projectRoot = Directory.GetParent(baseDirectory).Parent.Parent.Parent.Parent.FullName;
            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            var fullPath = Path.Combine(basePath, filePath);


            // string fullPath = Path.Combine(projectRoot, "UserService.Infrastructure", filePath);
            if (!File.Exists(fullPath))
            {
                Console.WriteLine($"⚠️ File seed data không tồn tại: {fullPath}");
                return new List<T>();
            }

            /*if (!File.Exists(fullPath))
                throw new FileNotFoundException($"Không tìm thấy file seed data: {fullPath}");*/

            var jsonData = File.ReadAllText(fullPath);
            var items = JsonSerializer.Deserialize<List<T>>(jsonData) ?? new List<T>();

            if (typeof(T) == typeof(ApplicationRole))
            {
                foreach (var item in items.Cast<ApplicationRole>())
                {
                    item.Id = Guid.Parse(item.Id.ToString());
                }
            }

            return items;
        }

    }
}
