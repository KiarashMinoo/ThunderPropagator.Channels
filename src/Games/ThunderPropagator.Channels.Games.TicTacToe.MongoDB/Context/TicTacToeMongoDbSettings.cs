namespace ThunderPropagator.Channels.Games.TicTacToe.MongoDB.Context
{
    public sealed class TicTacToeMongoDbSettings
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
    }
}
