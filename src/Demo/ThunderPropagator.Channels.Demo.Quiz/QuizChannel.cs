using Microsoft.Extensions.DependencyInjection;
using ThunderPropagator.Application.Channels;
using ThunderPropagator.Application.Channels.Subscribers;
using ThunderPropagator.Application.Connections;
using ThunderPropagator.Channels.Demo.Quiz.Game;
using ThunderPropagator.Channels.Demo.Quiz.Game.Enums;

namespace ThunderPropagator.Channels.Demo.Quiz
{
    /// <summary>
    /// Issue #191 gives this channel its first real behavior since #183's scaffold: <see cref="Join"/>
    /// is the orchestration point a receive pipeline (<c>QuizJoinGameReceiverPipeline</c>) calls into —
    /// every join-time business rule (missing game, full game, non-lobby joins, duplicate names) is
    /// enforced here, strictly before the one call that actually mutates anything
    /// (<see cref="QuizGameSession.Join"/>), so a rejected join is guaranteed to leave session state
    /// completely untouched (#191's own AC).
    /// </summary>
    public
#if !DEBUG
        sealed
#endif
        class QuizChannel : AbstractChannel<QuizChannelMetadata, QuizChannelConfiguration>
    {
        private readonly QuizGameSessionStore _sessionStore;
        private readonly QuizGameLoopRegistry _gameLoopRegistry;

        public QuizChannel(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _sessionStore = serviceProvider.GetRequiredService<QuizGameSessionStore>();
            _gameLoopRegistry = serviceProvider.GetRequiredService<QuizGameLoopRegistry>();
        }

        /// <summary>
        /// Joins <paramref name="connectionInfo"/> to <paramref name="gameId"/> under
        /// <paramref name="playerName"/> (normalized — see <see cref="NormalizePlayerName"/>) and
        /// subscribes it to that game's broadcasts. Checked, in order, before anything is mutated:
        /// the game must already exist (<see cref="QuizGameNotFoundException"/> otherwise — sessions
        /// are only ever created by the game loop itself, never implicitly by a join); if the
        /// configuration disallows it, the game must still be in its Lobby phase
        /// (<see cref="QuizNonLobbyJoinNotAllowedException"/>); and, unless this call is a reconnect or
        /// duplicate of an existing player (who never need a fresh seat), the game must not already be
        /// at <see cref="QuizChannelConfiguration.MaxPlayers"/> connected players
        /// (<see cref="QuizGameFullException"/>). <see cref="QuizGameSession.Join"/> itself then decides
        /// duplicate-vs-reconnect (throwing <see cref="Game.Exceptions.QuizDuplicateJoinException"/> for
        /// a name already connected) exactly as #187 documents.
        /// </summary>
        /// <remarks>
        /// A newly (re)joining connection receives the game's current state as a single unicast
        /// snapshot through this channel's own default snapshot-replay-on-subscribe behavior (the same
        /// mechanism <c>PortfolioDemoChannel</c>/<c>StockListBasicDemoChannel</c> already rely on) —
        /// nothing here re-emits it manually, since doing so would risk delivering it twice.
        /// </remarks>
        internal QuizJoinResult Join(IConnectionInfo connectionInfo, string requestId, string gameId, string playerName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(gameId);

            var normalizedPlayerName = NormalizePlayerName(playerName);
            var session = _sessionStore.TryGetSession(gameId) ?? throw new QuizGameNotFoundException(gameId);

            if (!ChannelConfiguration.AllowMidGameJoin && session.PhaseStateMachine.CurrentPhase != QuizPhase.Lobby)
                throw new QuizNonLobbyJoinNotAllowedException(gameId);

            var isExistingPlayer = session.Players.Any(player => player.PlayerName == normalizedPlayerName);
            if (!isExistingPlayer && session.Players.Count(player => player.IsConnected) >= ChannelConfiguration.MaxPlayers)
                throw new QuizGameFullException(gameId, ChannelConfiguration.MaxPlayers);

            var joinResult = session.Join(normalizedPlayerName, connectionInfo.ConnectionId);
            var subscription = Subscribe(connectionInfo, requestId, gameId);

            return new QuizJoinResult(subscription, joinResult.IsReconnect, joinResult.Player.IsHost, normalizedPlayerName);
        }

        /// <summary>
        /// Submits <paramref name="connectionInfo"/>'s answer of <paramref name="optionIndex"/> for
        /// <paramref name="questionIndex"/> in <paramref name="gameId"/>. Resolves which player is
        /// answering from the session's own established membership
        /// (<see cref="QuizGameSession.TryGetPlayerByConnectionId"/>) — never from a value a caller
        /// might supply directly — so a connection that never joined, or a stale connection a
        /// different player has since taken over via reconnect, can never answer on someone else's
        /// behalf (#192's own AC: "Resolve player and game from server-side session state rather than
        /// trusting caller identity", "Only joined players can answer"). Everything else — phase,
        /// question-index staleness, option-index validity, duplicate submissions, and the actual
        /// scoring — is <see cref="QuizGameLoop.SubmitAnswer"/>'s own concern (#190/#192); this method
        /// only ever resolves identity and looks up which loop to delegate to.
        /// </summary>
        internal QuizAnswerOutcome SubmitAnswer(IConnectionInfo connectionInfo, string gameId, int questionIndex, int optionIndex)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(gameId);

            var session = _sessionStore.TryGetSession(gameId) ?? throw new QuizGameNotFoundException(gameId);
            var player = session.TryGetPlayerByConnectionId(connectionInfo.ConnectionId) ?? throw new QuizNotAJoinedPlayerException(gameId);
            var gameLoop = _gameLoopRegistry.TryGet(gameId) ?? throw new QuizGameNotFoundException(gameId);

            return gameLoop.SubmitAnswer(player.PlayerName, questionIndex, optionIndex);
        }

        private Subscription Subscribe(IConnectionInfo connectionInfo, string requestId, string gameId)
        {
            var subscribeRequest = new QuizJoinSubscribeRequest
            {
                SubscribingKeys = new Dictionary<string, IReadOnlyDictionary<string, string>>
                {
                    {
                        nameof(QuizChannelFeederMessage.GameId), new Dictionary<string, string>
                        {
                            { nameof(QuizChannelFeederMessage.GameId), gameId }
                        }
                    }
                },
                SubscribingFields = new HashSet<string>
                {
                    nameof(QuizChannelFeederMessage.Phase),
                    nameof(QuizChannelFeederMessage.QuestionText),
                    nameof(QuizChannelFeederMessage.Options),
                    nameof(QuizChannelFeederMessage.TimeRemaining),
                    nameof(QuizChannelFeederMessage.QuestionIndex),
                    nameof(QuizChannelFeederMessage.TotalQuestions),
                    nameof(QuizChannelFeederMessage.Scoreboard),
                    nameof(QuizChannelFeederMessage.CorrectAnswer),
                    nameof(QuizChannelFeederMessage.Winner)
                },
                SubscriptionMode = SubscriptionMode.Full
            };

            return Subscribe(connectionInfo, requestId, subscribeRequest).Single();
        }

        /// <summary>
        /// Trims and collapses <paramref name="playerName"/>'s internal whitespace to single spaces
        /// (#191's own AC: "Validate ... normalized display name") and rejects it if the result exceeds
        /// <see cref="QuizChannelFeederMessage.TextMaxLength"/> — the same bound
        /// <see cref="QuizChannelFeederMessage.Scoreboard"/> already enforces on every entry's
        /// PlayerName, so an over-long name is rejected here at join time rather than surfacing later
        /// as a validation failure the first time this player appears on a broadcast scoreboard.
        /// </summary>
        private static string NormalizePlayerName(string playerName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(playerName);

            var normalized = string.Join(' ', playerName.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

            if (normalized.Length > QuizChannelFeederMessage.TextMaxLength)
                throw new QuizInvalidPlayerNameException(playerName, $"must not exceed {QuizChannelFeederMessage.TextMaxLength} characters after normalization (was {normalized.Length}).");

            return normalized;
        }
    }
}
