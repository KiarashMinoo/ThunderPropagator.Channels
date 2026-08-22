using ThunderPropagator.Application.Channels;

namespace ThunderPropagator.Channels.Demo.Quiz
{
    public
#if !DEBUG
        sealed
#endif
        class QuizChannel : AbstractChannel<QuizChannelMetadata, QuizChannelConfiguration>
    {
        public QuizChannel(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }
    }
}
