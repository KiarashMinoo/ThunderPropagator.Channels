using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ThunderPropagator.Channels.Games.RockPaperScissors.Models;

namespace ThunderPropagator.Channels.Games.RockPaperScissors.EntityFrameworkCore.Configurations
{
    internal sealed class RockPaperScissorsMatchReservationConfiguration : IEntityTypeConfiguration<RockPaperScissorsMatchReservation>
    {
        public void Configure(EntityTypeBuilder<RockPaperScissorsMatchReservation> builder)
        {
            builder.ToTable("RockPaperScissorsMatchReservations");

            // ConnectionId is the primary key itself, not a synthetic Guid Id — a reservation's whole
            // identity is "this connection has played," so the PK's own uniqueness is
            // TryReserveConnectionAsync's atomicity guarantee (see EntityFrameworkCoreRockPaperScissorsContext's
            // own comment): a second insert for the same ConnectionId throws DbUpdateException instead
            // of silently succeeding.
            builder.HasKey(reservation => reservation.ConnectionId);

            builder.Property(reservation => reservation.ConnectionId)
                .HasMaxLength(256);

            builder.Property(reservation => reservation.ReservedAt)
                .IsRequired();
        }
    }
}
