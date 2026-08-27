using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ThunderPropagator.Channels.Demo.Portfolio.Channel;
using ThunderPropagator.Channels.Demo.Portfolio.Configuration;

namespace ThunderPropagator.UnitTests.Demo.Portfolio
{
    /// <summary>
    /// Issue #11: <see cref="PortfolioDemoChannel"/>'s own background simulation loop was previously an
    /// <c>async void</c> method — any exception thrown inside it was posted to the synchronization
    /// context and unobservable by any caller (crashing the process in ASP.NET Core, or silently
    /// vanishing in other hosts). It is now <c>async Task</c>, and its returned <see cref="Task"/> is
    /// observed via a <c>ContinueWith(..., TaskContinuationOptions.OnlyOnFaulted)</c> continuation that
    /// logs the fault instead.
    /// </summary>
    public sealed class PortfolioDemoChannelSimulateFaultTests
    {
        private static IServiceProvider CreateServiceProvider(ILogger<PortfolioDemoChannel> logger, PortfolioDemoChannelConfiguration configuration)
        {
            var serviceProvider = Substitute.For<IServiceProvider>();
            serviceProvider.GetService(typeof(IHostApplicationLifetime)).Returns(Substitute.For<IHostApplicationLifetime>());
            serviceProvider.GetService(typeof(ILoggerFactory)).Returns(NullLoggerFactory.Instance);
            serviceProvider.GetService(typeof(ILogger<PortfolioDemoChannel>)).Returns(logger);
            serviceProvider.GetService(typeof(PortfolioDemoChannelConfiguration)).Returns(configuration);
            return serviceProvider;
        }

        [Fact]
        public void Constructor_DoesNotThrow()
        {
            var serviceProvider = CreateServiceProvider(NullLogger<PortfolioDemoChannel>.Instance, new PortfolioDemoChannelConfiguration
            {
                MinPollInterval = TimeSpan.FromMinutes(10),
                MaxPollInterval = TimeSpan.FromMinutes(11)
            });

            var exception = Record.Exception(() => new PortfolioDemoChannel(serviceProvider));

            Assert.Null(exception);
        }

        // MinPollInterval > MaxPollInterval makes the loop's own Random.Shared.Next(min, max) call throw
        // ArgumentOutOfRangeException synchronously, in the loop's very first iteration — a deterministic,
        // fast way to force a real fault without needing to mock the base channel's own snapshot-store
        // dependencies. Because SimulateAsync is itself an async method, this exception (even though it
        // occurs before any await) is never thrown to the constructor directly — the C# compiler always
        // captures an async method's own exception into its returned Task instead, which is exactly what
        // this test proves reaches the logger rather than disappearing.
        [Fact]
        public async Task Constructor_WhenTheBackgroundLoopFaultsImmediately_LogsTheFault_AndNeverThrowsFromTheConstructorItself()
        {
            var recordingLogger = new RecordingLogger<PortfolioDemoChannel>();
            var serviceProvider = CreateServiceProvider(recordingLogger, new PortfolioDemoChannelConfiguration
            {
                MinPollInterval = TimeSpan.FromSeconds(2),
                MaxPollInterval = TimeSpan.FromSeconds(1)
            });

            var exception = Record.Exception(() => new PortfolioDemoChannel(serviceProvider));
            Assert.Null(exception);

            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
            while (!recordingLogger.ErrorLogged && DateTime.UtcNow < deadline)
                await Task.Delay(10);

            Assert.True(recordingLogger.ErrorLogged, "expected the background loop's fault to reach the logger.");
            Assert.IsType<ArgumentOutOfRangeException>(recordingLogger.LoggedException?.InnerException ?? recordingLogger.LoggedException);
        }

        private sealed class RecordingLogger<T> : ILogger<T>
        {
            public bool ErrorLogged { get; private set; }
            public Exception? LoggedException { get; private set; }

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                if (logLevel != LogLevel.Error)
                    return;

                ErrorLogged = true;
                LoggedException = exception;
            }
        }
    }
}
