using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketManager.Entities;
using TicketManager.Enums;

namespace TicketManager.Data.Configurations
{
    public class TicketNoteConfiguration : IEntityTypeConfiguration<TicketNote>
    {
        public void Configure(EntityTypeBuilder<TicketNote> builder)
        {
            builder.ToTable("TicketNotes");

            builder.Property(x => x.NoteText).HasMaxLength(2000).IsRequired();
            // Eski sat�rlar migration s�ras�nda bo� kalabilir. Nullable olmas� bu sebepli. �leride Nullable kald�r�labilir.
            builder.Property(x => x.CreatedByUserId).HasMaxLength(100).IsRequired().HasDefaultValue("");
            builder.Property(x => x.Status).HasDefaultValue(Status.Active);

            builder.HasOne(x => x.Ticket)
                .WithMany(x => x.Notes)
                .HasForeignKey(x => x.TicketId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
