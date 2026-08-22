using ThunderPropagator.Application.Channels;

namespace ThunderPropagator.Channels.Demo.Quiz
{
    public
#if !DEBUG
        sealed
#endif
        class QuizChannelConfiguration : AbstractChannelConfiguration
    {
        public QuizChannelConfiguration()
        {
            IsEnabled = true;
        }
    }
}
