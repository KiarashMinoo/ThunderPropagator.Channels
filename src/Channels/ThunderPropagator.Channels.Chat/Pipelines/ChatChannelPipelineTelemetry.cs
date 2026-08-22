using System.Diagnostics.Metrics;

namespace ThunderPropagator.Channels.Chat.Pipelines
{
    // Issue #139: every Chat receiver pipeline lazily creates its own request counter on first
    // Invoke, since the counter's name is derived from the channel/pipeline metadata only known once
    // Invoke actually runs — it can't be created in the constructor. `_counter ??=
    // Telemetry.CreateCounter<long>(...)` alone is a non-atomic check-then-assign, so concurrent
    // first use could create and briefly reference more than one Counter<long> instrument before one
    // wins the race. Every pipeline counter field goes through this one helper instead of duplicating
    // the locking by hand, so all of them share the exact same safe pattern (the AC's own
    // requirement) and that pattern is unit-testable in isolation — ChannelInfo's constructor is
    // internal to a closed-source assembly (see ChatChannelAuthenticationTests' own comment), so a
    // pipeline's Invoke can't be driven directly in a test the way this standalone helper can.
    internal static class ChatChannelPipelineTelemetry
    {
        /// <summary>
        /// Returns <paramref name="counter"/>, creating it via <paramref name="createCounter"/> first
        /// if it's still unset. Double-checked locking — the outer null check is only a fast path
        /// once a counter already exists — guarantees <paramref name="createCounter"/> runs at most
        /// once per <paramref name="counterLock"/> regardless of how many threads race the first call.
        /// </summary>
        internal static Counter<long>? EnsureCounter(ref Counter<long>? counter, object counterLock, Func<Counter<long>?> createCounter)
        {
            if (counter is null)
            {
                lock (counterLock)
                {
                    counter ??= createCounter();
                }
            }

            return counter;
        }
    }
}
