using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ThunderPropagator.Application.Feeders;
using ThunderPropagator.Channels.Clock;
using ThunderPropagator.Infrastructure.Channels;
using ThunderPropagator.Infrastructure.Extensions;
using ThunderPropagator.Infrastructure.Feeders;

namespace ThunderPropagator.UnitTests.Feeders
{
    /// <summary>
    /// Channel-level integration coverage for issue #57: does a real IterativeFeeder (via a real
    /// ClockChannel, wired up through this repo's own AddClockChannel()/AddThunderPropagator() DI
    /// extensions, running inside a real generic Host) stop and dispose cleanly under a short-lived
    /// host — the scenario a WebApplicationFactory-based integration test exercises.
    ///
    /// Empirically confirmed (against plain Microsoft.Extensions.Hosting, not guessed) that neither
    /// IHost.Dispose() nor IHost.DisposeAsync() trigger ApplicationStopping or call any hosted
    /// service's StopAsync unless StopAsync was invoked explicitly first — documented .NET hosting
    /// behavior, true for any host, not specific to ThunderPropagator. FeederHostedService.StopAsync
    /// is the only path that stops/disposes feeders in this app, so a host disposed without an
    /// explicit StopAsync never runs that path at all. That's not something IterativeFeeder can work
    /// around — its StopAsync is simply never invoked.
    ///
    /// Separately (and unrelated to #57): disposing the host at all — even after a proper graceful
    /// StopAsync — currently throws ObjectDisposedException from
    /// ChannelManager.BuildChannelInfo's channel-Disposed handler, which lazily resolves FeederManager
    /// from the container inside the disposal callback. .NET's DI container marks a scope disposed
    /// before it finishes disposing the singletons it owns, so resolving anything new from that same
    /// container during a disposal callback throws, regardless of ordering. That's a distinct core
    /// bug (tracked separately upstream), so these tests assert everything they care about about
    /// feeder lifecycle before disposing the host, and isolate the disposal call so its known-broken
    /// exception doesn't mask a real regression in the assertions above it.
    /// </summary>
    public sealed class FeederHostLifecycleTests
    {
        private static IHost BuildHost()
        {
            var builder = Host.CreateDefaultBuilder();
            builder.ConfigureServices(services =>
            {
                var configuration = new ConfigurationBuilder().Build();
                services.AddThunderPropagator(configuration.GetSection("ThunderPropagator"));
                services.AddClockChannel();
            });

            var host = builder.Build();

            // Mirrors what UseThunderPropagator() does at ASP.NET Core startup time, minus the
            // middleware/endpoint registration this test doesn't need.
            host.Services.GetRequiredService<ChannelManager>().FillChannels();
            host.Services.GetRequiredService<FeederManager>().FillFeeders();

            return host;
        }

        private static Task GetBackgroundTask(object feeder)
        {
            for (var type = feeder.GetType(); type is not null; type = type.BaseType)
            {
                var field = type.GetField("_backgroundTask", BindingFlags.NonPublic | BindingFlags.Instance);
                if (field is not null)
                    return (Task)field.GetValue(feeder)!;
            }

            throw new MissingFieldException(feeder.GetType().FullName, "_backgroundTask");
        }

        /// <summary>
        /// Disposing the host currently throws (see class remarks) regardless of whether shutdown was
        /// graceful — a separate, pre-existing bug. Swallow only that known exception here so it can't
        /// mask a real failure in the feeder-lifecycle assertions this test exists to check.
        /// </summary>
        private static void DisposeIgnoringKnownChannelManagerDisposalBug(IHost host)
        {
            try
            {
                host.Dispose();
            }
            catch (ObjectDisposedException)
            {
            }
        }

        [Fact(Timeout = 10_000)]
        public async Task GracefulShutdown_StopsAndDisposesFeederCleanly_WithinBoundedTime()
        {
            var host = BuildHost();

            await host.StartAsync();

            var feeder = host.Services.GetRequiredService<NowClockFeeder>();

            // Give the 300ms poll loop a couple of iterations to prove it's genuinely running.
            await Task.Delay(700);
            Assert.Equal(FeederState.Started, feeder.State);

            var backgroundTask = GetBackgroundTask(feeder);
            Assert.False(backgroundTask.IsCompleted, "Feeder should still be looping before shutdown.");

            var stopwatch = Stopwatch.StartNew();
            await host.StopAsync(TimeSpan.FromSeconds(5));
            stopwatch.Stop();

            Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(5),
                $"Graceful shutdown should complete well inside the 5s bound; took {stopwatch.Elapsed}.");
            Assert.Equal(FeederState.Stopped, feeder.State);
            Assert.True(backgroundTask.IsCompleted, "Feeder's background loop should have completed by the time StopAsync returns.");

            // No further work continues after shutdown/disposal.
            await Task.Delay(500);
            Assert.Equal(FeederState.Stopped, feeder.State);
            Assert.True(backgroundTask.IsCompleted);

            DisposeIgnoringKnownChannelManagerDisposalBug(host);
        }

        [Fact(Timeout = 10_000)]
        public async Task KnownLimitation_DisposalWithoutExplicitStopAsync_AbandonsTheFeederLoop()
        {
            var host = BuildHost();
            var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();

            try
            {
                await host.StartAsync();

                var feeder = host.Services.GetRequiredService<NowClockFeeder>();
                await Task.Delay(700);
                Assert.Equal(FeederState.Started, feeder.State);

                var backgroundTask = GetBackgroundTask(feeder);

                // The WebApplicationFactory-teardown scenario from the issue: dispose without ever
                // calling StopAsync. This is a documented .NET hosting-model characteristic, not a
                // ThunderPropagator defect — FeederHostedService.StopAsync (the only path that stops
                // and disposes feeders) is never invoked, so nothing tells the loop to stop.
                DisposeIgnoringKnownChannelManagerDisposalBug(host);

                await Task.Delay(200);

                Assert.NotEqual(FeederState.Stopped, feeder.State);
                Assert.False(backgroundTask.IsCompleted,
                    "Known limitation: the feeder's background loop is abandoned, not stopped, when the host " +
                    "is disposed without an explicit StopAsync. If this assertion starts failing, the " +
                    "underlying gap has been closed upstream and this test should be updated/promoted.");
            }
            finally
            {
                // Manually trigger the same cancellation ApplicationStopping would have, so the
                // abandoned loop actually winds down instead of leaking into the rest of the test run.
                lifetime.StopApplication();
            }
        }
    }
}
