
namespace ThunderPropagator.UnitTests.Demo.Portfolio.Pipelines
{
    public class PortfolioDemoChannelPipelinesTests
    {
        [Fact]
        public void PortfolioDemoChannelBuyReceiverPipeline_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Demo.Portfolio.Pipelines.PortfolioDemoChannelBuyReceiverPipeline);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void PortfolioDemoChannelSellReceiverPipeline_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Demo.Portfolio.Pipelines.PortfolioDemoChannelSellReceiverPipeline);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void PortfolioDemoChannelReceiverPipelineRequestDto_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Demo.Portfolio.Pipelines.Dtos.PortfolioDemoChannelReceiverPipelineRequestDto);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void PortfolioDemoChannelReceiverPipelineResponseDto_IsPublic()
        {
            var type = typeof(ThunderPropagator.Channels.Demo.Portfolio.Pipelines.Dtos.PortfolioDemoChannelReceiverPipelineResponseDto);
            Assert.True(type.IsPublic);
        }
    }
}

