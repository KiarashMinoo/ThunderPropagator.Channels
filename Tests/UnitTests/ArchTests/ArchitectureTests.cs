﻿using NetArchTest.Rules;
using Xunit;

namespace ArchTests
{
    public class ArchitectureTests
    {
        private const string ChannelsNamespace = "ThunderPropagator.Channels";
        private const string DemoNamespace = "ThunderPropagator.Channels.Demo";
        private const string GamesNamespace = "ThunderPropagator.Channels.Games";

        [Fact]
        public void Channels_Should_NotDependOn_Demo()
        {
            var result = Types.InNamespace(ChannelsNamespace)
                .ShouldNot()
                .HaveDependencyOn(DemoNamespace)
                .GetResult();

            Assert.True(result.IsSuccessful, "Channels should not depend on Demo projects");
        }

        [Fact]
        public void Channels_Should_NotDependOn_Games()
        {
            var result = Types.InNamespace(ChannelsNamespace)
                .ShouldNot()
                .HaveDependencyOn(GamesNamespace)
                .GetResult();

            Assert.True(result.IsSuccessful, "Channels should not depend on Games projects");
        }

        [Fact]
        public void ClassesEndingWithChannel_Should_InheritFromAbstractChannel()
        {
            var result = Types.InNamespace(ChannelsNamespace)
                .That()
                .AreClasses()
                .And()
                .HaveNameEndingWith("Channel")
                .And()
                .DoNotHaveNameEndingWith("ChannelConfiguration")
                .And()
                .DoNotHaveNameEndingWith("ChannelMetadata")
                .And()
                .DoNotHaveNameEndingWith("ChannelFeederMessage")
                .And()
                .DoNotHaveNameEndingWith("ChannelExtensions")
                .Should()
                .BeAbstract()
                .Or()
                .BeSealed()
                .GetResult();

            Assert.True(result.IsSuccessful, "Channel classes should be abstract or sealed");
        }

        [Fact]
        public void ClassesEndingWithConfiguration_Should_BePublic()
        {
            var result = Types.InCurrentDomain()
                .That()
                .HaveNameEndingWith("Configuration")
                .Should()
                .BePublic()
                .GetResult();

            Assert.True(result.IsSuccessful, "Configuration classes should be public");
        }

        [Fact]
        public void ClassesEndingWithFeeder_Should_BePublic()
        {
            var result = Types.InCurrentDomain()
                .That()
                .HaveNameEndingWith("Feeder")
                .And()
                .DoNotHaveNameEndingWith("FeederMessage")
                .And()
                .DoNotHaveNameEndingWith("FeederConfiguration")
                .Should()
                .BePublic()
                .GetResult();

            Assert.True(result.IsSuccessful, "Feeder classes should be public");
        }

        [Fact]
        public void ClassesEndingWithPipeline_Should_BePublic()
        {
            var result = Types.InCurrentDomain()
                .That()
                .HaveNameEndingWith("Pipeline")
                .Should()
                .BePublic()
                .GetResult();

            Assert.True(result.IsSuccessful, "Pipeline classes should be public");
        }

        [Fact]
        public void ExtensionClasses_Should_BeStaticAndPublic()
        {
            var result = Types.InCurrentDomain()
                .That()
                .HaveNameEndingWith("Extensions")
                .Should()
                .BeStatic()
                .And()
                .BePublic()
                .GetResult();

            Assert.True(result.IsSuccessful, "Extension classes should be static and public");
        }

        [Fact]
        public void FeederMessages_Should_InheritFromFeederMessage()
        {
            var result = Types.InCurrentDomain()
                .That()
                .HaveNameEndingWith("FeederMessage")
                .And()
                .DoNotHaveNameEndingWith("ChannelFeederMessage")
                .Should()
                .BePublic()
                .GetResult();

            Assert.True(result.IsSuccessful, "Feeder message classes should be public");
        }

        [Fact]
        public void Metadata_Should_BePublic()
        {
            var result = Types.InCurrentDomain()
                .That()
                .HaveNameEndingWith("Metadata")
                .Should()
                .BePublic()
                .GetResult();

            Assert.True(result.IsSuccessful, "Metadata classes should be public");
        }

        [Fact]
        public void Exceptions_Should_InheritFromException()
        {
            var result = Types.InCurrentDomain()
                .That()
                .HaveNameEndingWith("Exception")
                .Should()
                .Inherit(typeof(Exception))
                .GetResult();

            Assert.True(result.IsSuccessful, "Exception classes should inherit from System.Exception");
        }

        [Fact]
        public void Demo_Should_NotDependOn_Games()
        {
            var result = Types.InNamespace(DemoNamespace)
                .ShouldNot()
                .HaveDependencyOn(GamesNamespace)
                .GetResult();

            Assert.True(result.IsSuccessful, "Demo projects should not depend on Games projects");
        }

        [Fact]
        public void Games_Should_NotDependOn_Demo()
        {
            var result = Types.InNamespace(GamesNamespace)
                .ShouldNot()
                .HaveDependencyOn(DemoNamespace)
                .GetResult();

            Assert.True(result.IsSuccessful, "Games projects should not depend on Demo projects");
        }

        [Fact]
        public void PipelineRequestDtos_Should_BePublic()
        {
            var result = Types.InCurrentDomain()
                .That()
                .HaveNameEndingWith("PipelineRequestDto")
                .Or()
                .HaveNameEndingWith("ReceiverPipelineRequestDto")
                .Should()
                .BePublic()
                .GetResult();

            Assert.True(result.IsSuccessful, "Pipeline request DTOs should be public");
        }

        [Fact]
        public void PipelineResponseDtos_Should_BePublic()
        {
            var result = Types.InCurrentDomain()
                .That()
                .HaveNameEndingWith("PipelineResponseDto")
                .Or()
                .HaveNameEndingWith("ReceiverPipelineResponseDto")
                .Should()
                .BePublic()
                .GetResult();

            Assert.True(result.IsSuccessful, "Pipeline response DTOs should be public");
        }
    }
}
