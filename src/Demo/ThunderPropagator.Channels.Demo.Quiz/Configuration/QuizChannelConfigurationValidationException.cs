using ThunderPropagator.Channels.Demo.Quiz.Configuration;
using ThunderPropagator.Channels.Demo.Quiz.Extensions;
namespace ThunderPropagator.Channels.Demo.Quiz.Configuration
{
    /// <summary>
    /// Thrown by <see cref="QuizChannelExtensions.AddQuizChannel"/> when a rule spanning more than one
    /// <see cref="QuizChannelConfiguration"/> property is violated — #195's own AC: "Invalid
    /// configuration fails at startup with property-specific errors." Distinct from the
    /// <see cref="ArgumentOutOfRangeException"/> each property's own setter throws for a rule that
    /// property alone can check (e.g. <see cref="QuizChannelConfiguration.MaxPlayers"/> or
    /// <see cref="QuizChannelConfiguration.MinPlayers"/> being non-positive): a cross-property rule like
    /// <see cref="QuizChannelConfiguration.MinPlayers"/> exceeding <see cref="QuizChannelConfiguration.MaxPlayers"/>
    /// can only be checked once both values are known, which is only true once the configurator callback
    /// passed to <see cref="QuizChannelExtensions.AddQuizChannel"/> has finished running.
    /// </summary>
    public sealed class QuizChannelConfigurationValidationException(string propertyName, string rule) : Exception($"{propertyName} {rule}")
    {
        public string PropertyName { get; } = propertyName;
    }
}
