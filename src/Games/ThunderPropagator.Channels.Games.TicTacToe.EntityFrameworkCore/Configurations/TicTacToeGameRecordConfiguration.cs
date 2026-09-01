using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ThunderPropagator.Channels.Games.TicTacToe.Models;

namespace ThunderPropagator.Channels.Games.TicTacToe.EntityFrameworkCore.Configurations
{
    internal sealed class TicTacToeGameRecordConfiguration : IEntityTypeConfiguration<TicTacToeGameRecord>
    {
        public void Configure(EntityTypeBuilder<TicTacToeGameRecord> builder)
        {
            builder.ToTable("TicTacToeGames");

            builder.HasKey(game => game.SessionId);

            builder.Property(game => game.SessionId).HasMaxLength(128);
            builder.Property(game => game.Board).IsRequired().HasMaxLength(9);

            builder.Property(game => game.Player1Name).IsRequired().HasMaxLength(256);
            builder.Property(game => game.Player1Sign).IsRequired();
            builder.Property(game => game.Player1ConnectionId).IsRequired().HasMaxLength(256);

            builder.Property(game => game.Player2Name).HasMaxLength(256);
            builder.Property(game => game.Player2Kind);
            builder.Property(game => game.Player2ConnectionId).HasMaxLength(256);
            builder.Property(game => game.Player2DifficultyLevel);
            builder.Property(game => game.CurrentTurnSign);
        }
    }
}
