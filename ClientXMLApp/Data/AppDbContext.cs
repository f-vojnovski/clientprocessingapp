using ClientXMLApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ClientXMLApp.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Client> Clients { get; set; }
        public DbSet<Address> Addresses { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Client>()
                .HasKey(c => c.ID);

            modelBuilder.Entity<Address>()
                .HasKey(a => new { a.Type, a.ClientID });

            modelBuilder.Entity<Address>()
                .HasOne(a => a.Client)
                .WithMany(c => c.Addresses)
                .HasForeignKey(a => a.ClientID);
        }
    }
}
