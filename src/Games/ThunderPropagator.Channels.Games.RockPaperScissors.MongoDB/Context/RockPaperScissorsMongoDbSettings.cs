namespace ThunderPropagator.Channels.Games.RockPaperScissors.MongoDB.Context
{
    public sealed class RockPaperScissorsMongoDbSettings
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
    }
}
