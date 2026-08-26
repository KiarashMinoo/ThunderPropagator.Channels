using Microsoft.Extensions.DependencyInjection;
using ThunderPropagator.Application.Channels;
using ThunderPropagator.Application.Channels.Exceptions;
using ThunderPropagator.Application.Channels.Subscribers;
using ThunderPropagator.Application.Connections;
using ThunderPropagator.Channels.Demo.Quiz.Game;
using ThunderPropagator.Channels.Demo.Quiz.Game.Enums;
using ThunderPropagator.Channels.Demo.Quiz.Configuration;
using ThunderPropagator.Channels.Demo.Quiz.Extensions;
using ThunderPropagator.Channels.Demo.Quiz.Feeders;
using ThunderPropagator.Channels.Demo.Quiz.Messages;
using ThunderPropagator.Channels.Demo.Quiz.Metadata;

namespace ThunderPropagator.Channels.Demo.Quiz.Channel
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
        class QuizChannel : AbstractChannel<QuizChannelMetadata, QuizChannelConfiguration>, IProvider<QuizProviderMessage>
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

        /// <summary>
        /// Starts <paramref name="gameId"/> early on behalf of <paramref name="connectionInfo"/>,
        /// skipping the rest of its autonomous Lobby wait (#189). Checked, in order (#193's own scope:
        /// "Validate current phase, minimum players, and question availability"): the caller must be a
        /// joined player (<see cref="QuizNotAJoinedPlayerException"/> otherwise) and specifically the
        /// session's host (<see cref="QuizNotTheHostException"/> otherwise — #193's own AC, "Only the
        /// host can start a game"); the game must have at least
        /// <see cref="QuizChannelConfiguration.MinPlayers"/> connected players
        /// (<see cref="QuizNotEnoughPlayersException"/> otherwise). "Question availability" has no
        /// separate runtime check here — <see cref="Game.QuizQuestionBank"/>'s own constructor already
        /// guarantees at least <see cref="Game.QuizQuestionBank.MinimumQuestionCount"/> questions (#188),
        /// so a bank that reached this point can never be empty. Phase itself is not checked
        /// explicitly beforehand: <see cref="QuizGameLoop.TryStartNow"/> only ever succeeds from Lobby
        /// and returns null otherwise, which this method reports as
        /// <see cref="QuizStartOutcome.AlreadyStarted"/> rather than an error — the same outcome a
        /// second, duplicate start request (or a concurrent one that lost the race) produces, since
        /// <see cref="QuizGameLoop.TryStartNow"/>'s own lock makes exactly one caller ever succeed
        /// (#193's own AC: "Concurrent requests create one running loop").
        /// </summary>
        /// <remarks>
        /// A successful start broadcasts the new Question-phase state via
        /// <see cref="ThunderPropagator.Application.Channels.IChannel.EmitMessage"/> — unlike
        /// <see cref="Join"/>/<see cref="SubmitAnswer"/>, this transition is not driven by
        /// <see cref="QuizFeeder"/>'s own tick loop, so nothing else would ever reach every current
        /// subscriber (#193's own AC: "All subscribers receive the phase transition") without this
        /// explicit emission.
        /// </remarks>
        internal QuizStartOutcome StartGame(IConnectionInfo connectionInfo, string gameId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(gameId);

            var session = _sessionStore.TryGetSession(gameId) ?? throw new QuizGameNotFoundException(gameId);
            var player = session.TryGetPlayerByConnectionId(connectionInfo.ConnectionId) ?? throw new QuizNotAJoinedPlayerException(gameId);

            if (!string.Equals(player.PlayerName, session.HostPlayerName, StringComparison.Ordinal))
                throw new QuizNotTheHostException(gameId);

            var connectedPlayerCount = session.Players.Count(candidate => candidate.IsConnected);
            if (connectedPlayerCount < ChannelConfiguration.MinPlayers)
                throw new QuizNotEnoughPlayersException(gameId, ChannelConfiguration.MinPlayers, connectedPlayerCount);

            var gameLoop = _gameLoopRegistry.TryGet(gameId) ?? throw new QuizGameNotFoundException(gameId);
            var message = gameLoop.TryStartNow();

            if (message is null)
                return QuizStartOutcome.AlreadyStarted;

            EmitMessage(message);
            return QuizStartOutcome.Started;
        }

        /// <summary>
        /// Broadcasts <paramref name="message"/> as this game's current state — #194's own
        /// <see cref="IProvider{TMessage}"/> implementation, letting a host application push its own
        /// externally-produced quiz state/questions programmatically, entirely independent of this
        /// package's built-in simulation (<see cref="Game.QuizGameSessionStore"/>/<see cref="Game.QuizGameLoop"/>/
        /// <see cref="QuizFeeder"/>): unlike <see cref="Join"/>/<see cref="SubmitAnswer"/>/<see cref="StartGame"/>,
        /// this method never touches session or membership state at all, and <paramref name="message"/>'s
        /// <see cref="QuizProviderMessage.GameId"/> need not correspond to any session
        /// <see cref="Game.QuizGameSessionStore"/> knows about. Provider-driven and simulated (#189)
        /// publishing coexist safely only for <em>different</em> GameIds — the built-in simulation always
        /// drives its own fixed demo GameId, so a provider-driven host should never reuse that literal
        /// value; this package has no configuration to disable the simulation outright, so a deployment
        /// that wants provider-only behavior for a shared GameId cannot currently do so through
        /// <see cref="QuizChannelExtensions.AddQuizChannel"/> alone.
        /// </summary>
        /// <remarks>
        /// Checked, in order: <paramref name="cancellationToken"/> must not already be cancelled
        /// (#194's own AC: "propagate cancellation/errors" — checked before anything else, so a
        /// cancelled call never touches the channel at all); <see cref="AbstractChannelConfiguration.IsEnabled"/>
        /// must be <see langword="true"/> (<see cref="ChannelIsNotEnabledException"/> otherwise, the
        /// same framework exception <c>NotificationsChannel</c> already uses for the same reason). Most
        /// of #194's own "payload limits"/"timing"/"options" validation then comes for free from
        /// constructing <see cref="QuizChannelFeederMessage"/> itself below — every property assigned
        /// here already validates through that type's own setters (#186), exactly as strictly as the
        /// built-in simulation's own messages are. The one rule the wire message does not itself enforce
        /// — because it legitimately allows these same fields to be empty at other phases — is checked
        /// explicitly first: <see cref="QuizPhase.Question"/>/<see cref="QuizPhase.Revealing"/> require
        /// actual question content (<see cref="QuizProviderValidationException"/> otherwise).
        /// </remarks>
        public Task PublishAsync(QuizProviderMessage message, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(message);
            cancellationToken.ThrowIfCancellationRequested();

            if (!ChannelConfiguration.IsEnabled)
                throw new ChannelIsNotEnabledException();

            if (message.Phase is QuizPhase.Question or QuizPhase.Revealing)
            {
                if (string.IsNullOrWhiteSpace(message.QuestionText))
                    throw new QuizProviderValidationException(nameof(message.QuestionText), $"must not be empty while Phase is {message.Phase}.");

                if (message.Options.Count < 2)
                    throw new QuizProviderValidationException(nameof(message.Options), $"must contain at least 2 options while Phase is {message.Phase} (had {message.Options.Count}).");
            }

            var feederMessage = new QuizChannelFeederMessage
            {
                GameId = message.GameId,
                Phase = message.Phase,
                QuestionText = message.QuestionText,
                Options = message.Options,
                TimeRemaining = message.TimeRemaining,
                QuestionIndex = message.QuestionIndex,
                TotalQuestions = message.TotalQuestions,
                Scoreboard = message.Scoreboard,
                CorrectAnswer = message.CorrectAnswer,
                Winner = message.Winner
            };

            EmitMessage(feederMessage);

            return Task.CompletedTask;
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
