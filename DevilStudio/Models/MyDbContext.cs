using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevilStudio.Models
{
    public class MyDbContext : DbContext
    {
        public DbSet<UserDetail> UserDetails { get; set; }

        public DbSet<ConnectedAccount> ConnectedAccounts { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //optionsBuilder.UseSqlServer("Data Source=192.168.20.17;Initial Catalog=sandbox;User ID=deviluser;Password=devil@user@123;Trust Server Certificate=True");
            optionsBuilder.UseSqlServer("Data Source=SHIVAKUMAR-GS-L;Initial Catalog=sandbox;Integrated Security=True;Encrypt=True;Trust Server Certificate=True");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configuring the one-to-many relationship
            modelBuilder.Entity<ConnectedAccount>()
                .HasOne(ca => ca.UserDetail)
                .WithMany(ud => ud.ConnectedAccounts)
                .HasForeignKey(ca => ca.UserDetailId);
        }
    }
}
