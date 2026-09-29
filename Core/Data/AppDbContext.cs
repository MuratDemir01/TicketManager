using Microsoft.EntityFrameworkCore;
using TicketManager.Entities;

namespace TicketManager.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<TicketNote> TicketNotes { get; set; }
        public DbSet<TicketHistory> TicketHistories { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // Bu assembly'deki IEntityTypeConfiguration<> sınıflarını bulup tek tek ApplyConfiguration yazmama gerek kalmadan yükler. 
            // TicketConfiguration vs. ekleyince burayı elle güncellememe gerek kalmaz.
            builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}
