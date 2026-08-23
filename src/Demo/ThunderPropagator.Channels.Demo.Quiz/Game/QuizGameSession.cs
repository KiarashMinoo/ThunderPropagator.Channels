using ThunderPropagator.Channels.Demo.Quiz.Game.Exceptions;

namespace ThunderPropagator.Channels.Demo.Quiz.Game
{
    /// <summary>
    /// One game's isolated session state: its <see cref="PhaseStateMachine"/> (see #184's own remarks
    /// on why that type owns only the phase itself), its player membership keyed by display name, its
    /// host, and its current public state snapshot. Every instance is independent — two
    /// <see cref="QuizGameSession"/>s never share a lock, a player dictionary, or a
    /// <see cref="PhaseStateMachine"/>, so concurrent activity on one game can never observe or
    /// corrupt another (#187's own AC). All membership mutation and the current-state snapshot are
    /// guarded by the same lock, so a join, reconnect, disconnect, or state update is always seen by
    /// every other operation as either fully applied or not yet applied, never partially.
    /// </summary>
    internal
#if !DEBUG
        sealed
#endif
        class QuizGameSession
    {
#if NET9_0_OR_GREATER
        private readonly Lock _lock = new();
#else
        private readonly object _lock = new();
#endif

        private readonly Dictionary<string, QuizPlayer> _playersByName = new(StringComparer.Ordinal);

        // Kept in sync with _playersByName by every mutation below: an entry exists here for exactly
        // as long as its player is connected under that specific connection — removed on disconnect,
        // and re-pointed (old key removed, new key added) on reconnect under a different connection.
        // This is #192's own resolution path for "which joined player is this connection" — never a
        // client-supplied identity.
        private readonly Dictionary<string, QuizPlayer> _playersByConnectionId = new(StringComparer.Ordinal);

        private QuizChannelFeederMessage? _currentState;

        public QuizGameSession(string gameId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(gameId);
            GameId = gameId;
        }

        /// <summary>This session's identity — the same value every player subscribes under.</summary>
        public string GameId { get; }

        /// <summary>This session's own phase lifecycle. See <see cref="QuizPhaseStateMachine"/>'s own remarks — this composition is exactly what that type's doc comments describe as session state's responsibility.</summary>
        public QuizPhaseStateMachine PhaseStateMachine { get; } = new();

        /// <summary>
        /// The display name of this session's host — the player whose <see cref="Join"/> call created
        /// it. Fixed for the session's lifetime: a host who disconnects is still the host (just with
        /// <see cref="QuizPlayer.IsConnected"/> false) until they reconnect. Reassigning host on
        /// disconnect is deliberately out of scope here — see this type's own remarks on host-authorized
        /// actions being #193's concern, which can decide for itself how to treat a disconnected host.
        /// </summary>
        public string? HostPlayerName { get; private set; }

        /// <summary>
        /// A point-in-time copy of this session's currently connected players. Safe to enumerate
        /// without holding any lock — later membership changes never mutate the returned array.
        /// </summary>
        public IReadOnlyList<QuizPlayer> Players
        {
            get
            {
                lock (_lock)
                {
                    return [.. _playersByName.Values];
                }
            }
        }

        /// <summary>
        /// The most recent public state this session has been told about via <see cref="UpdateCurrentState"/>,
        /// or null if none has been recorded yet (a game still in its very first Lobby moment). This is
        /// the one snapshot <see cref="Join"/> hands back to a newly (re)joining connection — see
        /// <see cref="QuizPlayerJoinResult.CurrentState"/>'s own remarks for why that snapshot must
        /// never be rebroadcast to anyone else.
        /// </summary>
        public QuizChannelFeederMessage? CurrentState
        {
            get
            {
                lock (_lock)
                {
                    return _currentState;
                }
            }
        }

        /// <summary>
        /// Records <paramref name="state"/> as this session's current public state, superseding
        /// whatever was recorded before. Ownership of when to call this — after every phase change, a
        /// scoreboard update, and so on — belongs to whichever future ticket actually drives the game
        /// loop (#189) and emits messages (#190); this type only ever stores whatever it is given.
        /// </summary>
        public void UpdateCurrentState(QuizChannelFeederMessage state)
        {
            ArgumentNullException.ThrowIfNull(state);

            lock (_lock)
            {
                _currentState = state;
            }
        }

        /// <summary>
        /// Adds <paramref name="playerName"/> to this session under <paramref name="connectionId"/>,
        /// or reconnects them if that name already belongs to a disconnected player. The very first
        /// call for a brand-new session names its caller the host (<see cref="HostPlayerName"/>) —
        /// every later call, whether a genuinely new player or a reconnect, leaves host unchanged.
        /// </summary>
        /// <remarks>
        /// <b>Duplicate join vs. reconnect (#187's own AC):</b> a name that already belongs to a
        /// <i>connected</i> player is a duplicate — two live connections can never claim the same
        /// display name in the same game at once, so this throws <see cref="QuizDuplicateJoinException"/>
        /// without mutating anything. A name that belongs to a <i>disconnected</i> player is instead a
        /// reconnect: the existing <see cref="QuizPlayer"/> is reactivated under the new
        /// <paramref name="connectionId"/>, preserving its identity (<see cref="QuizPlayer.PlayerName"/>,
        /// <see cref="QuizPlayer.IsHost"/>) rather than creating a second entry — this is the documented
        /// policy the AC asks for, and is exactly why player membership is keyed by name rather than by
        /// connection.
        /// </remarks>
        public QuizPlayerJoinResult Join(string playerName, string connectionId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(playerName);
            ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

            lock (_lock)
            {
                if (_playersByName.TryGetValue(playerName, out var existingPlayer))
                {
                    if (existingPlayer.IsConnected)
                        throw new QuizDuplicateJoinException(GameId, playerName);

                    _playersByConnectionId.Remove(existingPlayer.ConnectionId);
                    existingPlayer.ConnectionId = connectionId;
                    existingPlayer.IsConnected = true;
                    _playersByConnectionId[connectionId] = existingPlayer;

                    return new QuizPlayerJoinResult(existingPlayer, IsReconnect: true, _currentState);
                }

                var isHost = HostPlayerName is null;
                var player = new QuizPlayer(playerName, connectionId, isHost);
                _playersByName.Add(playerName, player);
                _playersByConnectionId[connectionId] = player;

                if (isHost)
                    HostPlayerName = playerName;

                return new QuizPlayerJoinResult(player, IsReconnect: false, _currentState);
            }
        }

        /// <summary>
        /// Marks the connected player using <paramref name="connectionId"/> as disconnected, keeping
        /// their <see cref="QuizPlayer"/> entry (and thus their identity, host status, and any
        /// score/state a future ticket associates with their name) so a later <see cref="Join"/> under
        /// the same player name can reconnect them. Returns whether a connected player using that
        /// connection was found — disconnecting an unknown or already-disconnected connection is a
        /// harmless no-op, not an error, mirroring an ordinary network disconnect that races a
        /// duplicate notification.
        /// </summary>
        public bool Disconnect(string connectionId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

            lock (_lock)
            {
                if (!_playersByConnectionId.TryGetValue(connectionId, out var player) || !player.IsConnected)
                    return false;

                player.IsConnected = false;
                _playersByConnectionId.Remove(connectionId);
                return true;
            }
        }

        /// <summary>
        /// The currently-connected player using <paramref name="connectionId"/>, or null if that
        /// connection is not a joined player in this session right now — #192's own resolution path
        /// for "who is submitting this answer," server-side, rather than trusting a player identity a
        /// caller might supply directly in a request.
        /// </summary>
        public QuizPlayer? TryGetPlayerByConnectionId(string connectionId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

            lock (_lock)
            {
                return _playersByConnectionId.GetValueOrDefault(connectionId);
            }
        }

        /// <summary>
        /// Whether this session has no player with a live connection — either nobody ever joined, or
        /// everyone who did has since disconnected. <see cref="QuizGameSessionStore.RemoveIfAbandoned"/>
        /// is the only place this drives actual cleanup (#187's own AC); this property itself never
        /// removes anything.
        /// </summary>
        public bool IsAbandoned
        {
            get
            {
                lock (_lock)
                {
                    return _playersByName.Values.All(player => !player.IsConnected);
                }
            }
        }
    }
}
