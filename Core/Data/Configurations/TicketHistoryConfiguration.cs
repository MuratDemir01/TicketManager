using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketManager.Entities;

namespace TicketManager.Data.Configurations
{
    public class TicketHistoryConfiguration : IEntityTypeConfiguration<TicketHistory>
    {
        public void Configure(EntityTypeBuilder<TicketHistory> builder)
        {
            builder.ToTable("TicketHistories");

            builder.Property(x => x.Action).HasMaxLength(100).IsRequired();
            builder.Property(x => x.FieldName).HasMaxLength(100).IsRequired();
            builder.Property(x => x.OldValue).HasMaxLength(500);
            builder.Property(x => x.NewValue).HasMaxLength(500);

            builder.HasOne(x => x.Ticket)
                .WithMany(x => x.Histories)
                .HasForeignKey(x => x.TicketId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
