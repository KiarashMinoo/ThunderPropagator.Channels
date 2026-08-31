using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ThunderPropagator.Application.Connections;
using ThunderPropagator.Channels.Games.TicTacToe.Channel;
using ThunderPropagator.Channels.Games.TicTacToe.Configuration;
using ThunderPropagator.Channels.Games.TicTacToe.Game.Enums;
using ThunderPropagator.Channels.Games.TicTacToe.Metadata;
﻿using Xunit;

namespace ThunderPropagator.UnitTests.Games.TicTacToe
{
    public class TicTacToeChannelTests
    {
        private static TicTacToeChannel CreateChannel()
        {
            var serviceProvider = Substitute.For<IServiceProvider>();
            serviceProvider.GetService(typeof(IHostApplicationLifetime)).Returns(Substitute.For<IHostApplicationLifetime>());
            serviceProvider.GetService(typeof(ILoggerFactory)).Returns(NullLoggerFactory.Instance);
            serviceProvider.GetService(typeof(TicTacToeChannelConfiguration)).Returns(new TicTacToeChannelConfiguration());

            var channel = new TicTacToeChannel(serviceProvider);
            channel.Initialize(CancellationToken.None);

            return channel;
        }

        private static IConnectionInfo CreateConnection(string connectionId)
        {
            var connectionInfo = Substitute.For<IConnectionInfo>();
            connectionInfo.ConnectionId.Returns(connectionId);
            return connectionInfo;
        }

        [Fact]
        public void TicTacToeChannel_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Games.TicTacToe.Channel.TicTacToeChannel);
            Assert.True(type.IsPublic);
        }

        // Issue #37: Move's own `throw new KeyNotFoundException(...)` used to sit unconditionally
        // after the session/connection lookup, so it fired even when the caller was the game's own
        // player and the move itself succeeded. Uses PlayerKind.Human (not Computer, which AddGame
        // never adds to _games at all, so Move could never find it) — before a second player joins,
        // TicTacToeGame.StartGame hasn't wired up its turn-order event handlers yet, so Player1's own
        // first move goes through untouched by that, exercising exactly the AddGame -> Move control
        // flow this fix is about.
        [Fact]
        public void Move_ByTheGamesOwnPlayer_DoesNotThrowKeyNotFoundException()
        {
            var channel = CreateChannel();
            var connectionInfo = CreateConnection("connection-1");
            channel.AddGame(connectionInfo, "request-1", "session-1", "Alice", PlayerSign.X, PlayerKind.Human);

            var exception = Record.Exception(() => channel.Move("session-1", connectionInfo, 0, 0));

            Assert.IsNotType<KeyNotFoundException>(exception);
        }

        [Fact]
        public void Move_ForAnUnknownSession_ThrowsKeyNotFoundExceptionWithoutEchoingTheSessionId()
        {
            var channel = CreateChannel();
            var connectionInfo = CreateConnection("connection-1");

            var exception = Assert.Throws<KeyNotFoundException>(() => channel.Move("super-secret-session-id", connectionInfo, 0, 0));

            Assert.DoesNotContain("super-secret-session-id", exception.Message);
        }

        [Fact]
        public void Move_ByAConnectionThatIsNotTheGamesPlayer_ThrowsKeyNotFoundException()
        {
            var channel = CreateChannel();
            var ownerConnection = CreateConnection("owner-connection");
            var intruderConnection = CreateConnection("intruder-connection");
            channel.AddGame(ownerConnection, "request-1", "session-1", "Alice", PlayerSign.X, PlayerKind.Human);

            Assert.Throws<KeyNotFoundException>(() => channel.Move("session-1", intruderConnection, 0, 0));
        }

        [Fact]
        public void TicTacToeChannelConfiguration_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Games.TicTacToe.Configuration.TicTacToeChannelConfiguration);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void TicTacToeChannelMetadata_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Games.TicTacToe.Metadata.TicTacToeChannelMetadata);
            Assert.True(type.IsPublic);
        }
    }
}

