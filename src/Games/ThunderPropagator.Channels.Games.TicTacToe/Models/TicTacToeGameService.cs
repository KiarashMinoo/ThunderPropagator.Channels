namespace ThunderPropagator.Channels.Games.TicTacToe.Models
{
    // Issue: replaces TicTacToeChannel's old node-local _games dictionary — see TicTacToeGameRecord's
    // own doc comment. Deliberately thin: rehydrating a TicTacToeGame from a record, applying a move,
    // and re-snapshotting it back into the record are all TicTacToeChannel's own job (it already owns
    // EmitMessage/the board-changed wiring those steps need), not this service's — this only does
    // persistence CRUD.
    internal
#if !DEBUG
        sealed
#endif
        class TicTacToeGameService(ITicTacToeContext context)
    {
        public Task<TicTacToeGameRecord?> GetGameAsync(string sessionId, CancellationToken cancellationToken = default)
            => context.GetAsync<TicTacToeGameRecord, string>(sessionId, cancellationToken);

        public Task<TicTacToeGameRecord> CreateGameAsync(TicTacToeGameRecord record, CancellationToken cancellationToken = default)
            => context.CreateAsync(record, cancellationToken);

        public Task<TicTacToeGameRecord> SaveGameAsync(TicTacToeGameRecord record, CancellationToken cancellationToken = default)
            => context.UpdateAsync(record, cancellationToken);

        public Task<bool> DeleteGameAsync(string sessionId, CancellationToken cancellationToken = default)
            => context.DeleteAsync<TicTacToeGameRecord, string>(sessionId, cancellationToken);

        /// <summary>
        /// Games still waiting for a second player — the persisted equivalent of the old
        /// _games.Select(...) GetGames() used to list, now correctly excluding already-started games.
        /// The original never filtered because it never had to: it never stored a vs-Computer game at
        /// all (the bug this fix corrects), so every game GetGames() ever saw actually was still open.
        /// </summary>
        public async Task<IReadOnlyCollection<TicTacToeGameRecord>> GetOpenGamesAsync(CancellationToken cancellationToken = default)
        {
            var games = await context.GetAllAsync<TicTacToeGameRecord>(cancellationToken);
            return games.Where(game => game.Player2Kind is null).ToList();
        }
    }
}
