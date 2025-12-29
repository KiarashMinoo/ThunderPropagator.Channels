
namespace ThunderPropagator.UnitTests.Demo.Portfolio.Pipelines
{
    public class PortfolioDemoChannelPipelinesTests
    {
        [Fact]
        public void PortfolioDemoChannelBuyReceiverPipeline_IsInternal()
        {
            var type = typeof(ThunderPropagator.Channels.Demo.Portfolio.Pipelines.PortfolioDemoChannelBuyReceiverPipeline);
            Assert.True(type.IsNotPublic);
        }

        [Fact]
        public void PortfolioDemoChannelSellReceiverPipeline_IsInternal()
        {
            var type = typeof(ThunderPropagator.Channels.Demo.Portfolio.Pipelines.PortfolioDemoChannelSellReceiverPipeline);
            Assert.True(type.IsNotPublic);
        }

        [Fact]
        public void PortfolioDemoChannelReceiverPipelineRequestDto_IsInternal()
        {
            var type = typeof(ThunderPropagator.Channels.Demo.Portfolio.Pipelines.Dtos.PortfolioDemoChannelReceiverPipelineRequestDto);
            Assert.True(type.IsNotPublic);
        }

        [Fact]
        public void PortfolioDemoChannelReceiverPipelineResponseDto_IsInternal()
        {
            var type = typeof(ThunderPropagator.Channels.Demo.Portfolio.Pipelines.Dtos.PortfolioDemoChannelReceiverPipelineResponseDto);
            Assert.True(type.IsNotPublic);
        }
    }
}

