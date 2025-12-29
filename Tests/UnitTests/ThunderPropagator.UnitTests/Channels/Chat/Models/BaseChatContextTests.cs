using ThunderPropagator.Channels.Chat.Models;

namespace ThunderPropagator.UnitTests.Channels.Chat.Models
{
    public class BaseChatContextTests
    {
        [Fact]
        public void BaseChatContext_IsPublic()
        {
            var type = typeof(BaseChatContext);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void BaseChatContext_IsAbstract()
        {
            var type = typeof(BaseChatContext);
            Assert.True(type.IsAbstract);
        }

        [Fact]
        public void BaseChatContext_HasGetAsyncMethods()
        {
            var type = typeof(BaseChatContext);
            var methods = type.GetMethods().Where(m => m.Name == "GetAsync").ToArray();
            
            Assert.True(methods.Length >= 2);
        }

        [Fact]
        public void BaseChatContext_HasGetAllAsyncMethods()
        {
            var type = typeof(BaseChatContext);
            var methods = type.GetMethods().Where(m => m.Name == "GetAllAsync").ToArray();
            
            Assert.True(methods.Length >= 2);
        }

        [Fact]
        public void BaseChatContext_HasCreateAsyncMethod()
        {
            var type = typeof(BaseChatContext);
            var method = type.GetMethod("CreateAsync");
            
            Assert.NotNull(method);
        }

        [Fact]
        public void BaseChatContext_HasUpdateAsyncMethod()
        {
            var type = typeof(BaseChatContext);
            var method = type.GetMethod("UpdateAsync");
            
            Assert.NotNull(method);
        }

        [Fact]
        public void BaseChatContext_HasDeleteAsyncMethod()
        {
            var type = typeof(BaseChatContext);
            var method = type.GetMethod("DeleteAsync");
            
            Assert.NotNull(method);
        }
    }
}
