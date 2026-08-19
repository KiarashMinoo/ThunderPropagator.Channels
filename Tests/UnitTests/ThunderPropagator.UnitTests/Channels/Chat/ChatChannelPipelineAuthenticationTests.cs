using System.Runtime.CompilerServices;
using ThunderPropagator.Application.Pipelines.Receivers;
using ThunderPropagator.Channels.Chat;
using ThunderPropagator.Channels.Chat.Pipelines;

namespace ThunderPropagator.UnitTests.Channels.Chat
{
    /// <summary>
    /// Issue #109: every Chat receiver pipeline except Login and Register must require an
    /// authenticated session. Ad hoc per-handler checks made that easy to miss (see #106), so the
    /// enforcement mechanism is structural — AuthenticatedChatChannelReceiverPipeline runs the guard
    /// before pipeline-specific logic runs at all. This test sweeps every ChatChannel receiver
    /// pipeline in the assembly via reflection: it fails the build if a new pipeline is added that
    /// neither derives from AuthenticatedChatChannelReceiverPipeline nor is explicitly allow-listed as
    /// anonymous, so the guard can't quietly be skipped for a new handler.
    /// </summary>
    public sealed class ChatChannelPipelineAuthenticationTests
    {
        private static readonly HashSet<string> AnonymousRequestKeys = new(StringComparer.Ordinal)
        {
            "Users/Login",
            "Users/Register",
        };

        private static Type[] GetAllChatReceivePipelineTypes()
            => typeof(ChatChannel).Assembly.GetTypes()
                .Where(type => !type.IsAbstract && DerivesFromChatChannelReceivePipeline(type))
                .ToArray();

        private static bool DerivesFromChatChannelReceivePipeline(Type type)
        {
            for (var current = type.BaseType; current is not null; current = current.BaseType)
            {
                if (current.IsGenericType
                    && current.GetGenericTypeDefinition() == typeof(AbstractReceivePipeline<>)
                    && current.GetGenericArguments()[0] == typeof(ChatChannel))
                    return true;
            }

            return false;
        }

        private static string GetRequestKey(Type type)
        {
            var instance = RuntimeHelpers.GetUninitializedObject(type);
            var property = type.GetProperty(nameof(IReceivePipeline.RequestKey))
                ?? throw new MissingMemberException(type.FullName, nameof(IReceivePipeline.RequestKey));
            return (string)property.GetValue(instance)!;
        }

        [Fact]
        public void AtLeastOnePipeline_IsDiscovered()
        {
            Assert.NotEmpty(GetAllChatReceivePipelineTypes());
        }

        [Fact]
        public void EveryPipeline_IsEitherAuthenticatedOrExplicitlyAnonymous()
        {
            foreach (var type in GetAllChatReceivePipelineTypes())
            {
                var requestKey = GetRequestKey(type);
                var isAnonymous = AnonymousRequestKeys.Contains(requestKey);
                var isAuthenticated = typeof(AuthenticatedChatChannelReceiverPipeline).IsAssignableFrom(type);

                Assert.True(isAnonymous || isAuthenticated,
                    $"{type.Name} (RequestKey '{requestKey}') derives from AbstractReceivePipeline<ChatChannel> but is neither " +
                    $"in the anonymous allow-list nor derived from {nameof(AuthenticatedChatChannelReceiverPipeline)}. " +
                    "Every pipeline except Login and Register must require an authenticated session.");

                Assert.False(isAnonymous && isAuthenticated,
                    $"{type.Name} is both allow-listed as anonymous and derives from {nameof(AuthenticatedChatChannelReceiverPipeline)} — remove it from one.");
            }
        }

        [Fact]
        public void OnlyLoginAndRegister_AreAnonymous()
        {
            var anonymousKeys = GetAllChatReceivePipelineTypes()
                .Where(type => !typeof(AuthenticatedChatChannelReceiverPipeline).IsAssignableFrom(type))
                .Select(GetRequestKey)
                .OrderBy(key => key, StringComparer.Ordinal)
                .ToArray();

            Assert.Equal(AnonymousRequestKeys.OrderBy(key => key, StringComparer.Ordinal), anonymousKeys);
        }

        [Fact]
        public void AllPipelineRequestKeys_AreUnique()
        {
            var keys = GetAllChatReceivePipelineTypes().Select(GetRequestKey).ToArray();
            Assert.Equal(keys.Length, keys.Distinct(StringComparer.Ordinal).Count());
        }
    }
}
