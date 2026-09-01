using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ThunderPropagator.Channels.Games.RockPaperScissors.Models;

namespace ThunderPropagator.Channels.Games.RockPaperScissors.EntityFrameworkCore.Configurations
{
    internal sealed class RockPaperScissorsGameSessionRecordConfiguration : IEntityTypeConfiguration<RockPaperScissorsGameSessionRecord>
    {
        public void Configure(EntityTypeBuilder<RockPaperScissorsGameSessionRecord> builder)
        {
            builder.ToTable("RockPaperScissorsGameSessionRecords");

            builder.HasKey(session => session.SessionId);

            builder.Property(session => session.SessionId)
                .HasMaxLength(64);

            builder.Property(session => session.FirstPlayerName).IsRequired().HasMaxLength(256);
            builder.Property(session => session.FirstPlayerType).IsRequired();
            builder.Property(session => session.FirstPlayerMove).IsRequired();
            builder.Property(session => session.FirstPlayerConnectionId).HasMaxLength(256);

            builder.Property(session => session.SecondPlayerName).IsRequired().HasMaxLength(256);
            builder.Property(session => session.SecondPlayerType).IsRequired();
            builder.Property(session => session.SecondPlayerMove).IsRequired();
            builder.Property(session => session.SecondPlayerConnectionId).HasMaxLength(256);

            builder.Property(session => session.PlayedAt).IsRequired();
        }
    }
}
