using ThunderPropagator.Application.Channels;

namespace ThunderPropagator.Channels.Demo.Quiz
{
    public
#if !DEBUG
        sealed
#endif
        class QuizChannelConfiguration : AbstractChannelConfiguration
    {
        public QuizFeederConfiguration FeederConfiguration { get; set; } = new();

        public QuizChannelConfiguration()
        {
            IsEnabled = true;
        }
    }
}
