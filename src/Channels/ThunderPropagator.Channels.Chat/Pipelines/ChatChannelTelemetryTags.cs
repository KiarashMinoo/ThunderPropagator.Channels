namespace ThunderPropagator.Channels.Chat.Pipelines
{
    // Issue #40: every activity/counter tag naming the runtime channel now goes through one shared
    // set of constants, so the OTel-convention lowercase-dotted key ("channel.name", not "ChannelName")
    // can't drift between pipelines.
    internal static class ChatChannelTelemetryTags
    {
        internal const string ChannelType = "channel.type";
        internal const string ChannelKey = "channel.key";
        internal const string ChannelName = "channel.name";
    }
}
