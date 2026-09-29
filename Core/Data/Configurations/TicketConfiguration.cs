using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketManager.Entities;

namespace TicketManager.Data.Configurations
{
    public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
    {
        public void Configure(EntityTypeBuilder<Ticket> builder)
        {
            builder.ToTable("Tickets");

            // Bunlar db tablosu tasarımı kuralları.
            builder.Property(x => x.TicketNumber).HasMaxLength(50).IsRequired();
            builder.HasIndex(x => x.TicketNumber).IsUnique();

            builder.Property(x => x.Title).HasMaxLength(150).IsRequired();
            builder.Property(x => x.Description).IsRequired();
            builder.Property(x => x.CustomerName).HasMaxLength(150).IsRequired();
            builder.Property(x => x.CustomerEmail).HasMaxLength(256).IsRequired();
            builder.Property(x => x.AssignedUserId).HasMaxLength(100);
            builder.Property(x => x.CreatedByUserId).HasMaxLength(100);
        }
    }
}
