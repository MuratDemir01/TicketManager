using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketManager.Entities;

namespace TicketManager.Data.Configurations
{
    public class TicketNoteConfiguration : IEntityTypeConfiguration<TicketNote>
    {
        public void Configure(EntityTypeBuilder<TicketNote> builder)
        {
            builder.ToTable("TicketNotes");

            builder.Property(x => x.NoteText).HasMaxLength(2000).IsRequired();
            // Eski satırlar migration sırasında boş kalabilir. Nullable olması bu sebepli. İleride Nullable kaldırılabilir.
            builder.Property(x => x.CreatedByUserId).HasMaxLength(100).IsRequired().HasDefaultValue("");

            builder.HasOne(x => x.Ticket)
                .WithMany(x => x.Notes)
                .HasForeignKey(x => x.TicketId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
