namespace ThunderPropagator.Channels.Chat.MongoDB
{
    public sealed class ChatMongoDbSettings
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
    }
}
