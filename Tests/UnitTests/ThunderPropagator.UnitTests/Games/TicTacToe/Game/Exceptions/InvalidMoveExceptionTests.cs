using ThunderPropagator.Channels.Games.TicTacToe.Game.Exceptions;

namespace ThunderPropagator.UnitTests.Games.TicTacToe.Game.Exceptions
{
    public class InvalidMoveExceptionTests
    {
        [Fact]
        public void InvalidMoveException_IsPublic()
        {
            var type = typeof(InvalidMoveException);
            Assert.True(type.IsPublic);
        }

        [Fact]
        public void InvalidMoveException_InheritsFromException()
        {
            var type = typeof(InvalidMoveException);
            Assert.True(typeof(Exception).IsAssignableFrom(type));
        }

        [Fact]
        public void InvalidMoveException_CanBeThrown()
        {
            // Arrange
            InvalidMoveException? exception = null;

            // Act
            try
            {
                throw new InvalidMoveException();
            }
            catch (InvalidMoveException ex)
            {
                exception = ex;
            }

            // Assert
            Assert.NotNull(exception);
        }

        [Fact]
        public void InvalidMoveException_CanBeCaught()
        {
            // Arrange
            var exceptionCaught = false;

            // Act
            try
            {
                throw new InvalidMoveException();
            }
            catch (InvalidMoveException)
            {
                exceptionCaught = true;
            }

            // Assert
            Assert.True(exceptionCaught);
        }
    }
}
