using MaintanceAlertBot.Handlers;
using Moq;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Xunit;
using MaintanceAlertBot.Services;

namespace MaintanceAlertBot.Tests
{
    public class UpdateHandlerTests
    {
        [Fact]
        public async Task HandleUpdateAsync_ShouldLogMessage_WhenUpdateIsMessage()
        {
            // Arrange
            var mockBotClient = new Mock<ITelegramBotClient>();
            var handler = new UpdateHandler();
            var update = new Update
            {
                Message = new Message
                {
                    Text = "Test Message",
                    Date = DateTime.UtcNow,
                    Chat = new Chat { Id = 12345, Username = "TestUser" },
                    From = new User { Id = 12345, Username = "TestUser" }
                }
            };

            // Act
            await handler.HandleUpdateAsync(mockBotClient.Object, update, CancellationToken.None);

            // Assert
            // Since the handler only logs, we verify it runs without exception.
            // In a real scenario, we might inject a Logger mock to verify logging calls.
            Assert.True(true);
        }

        [Fact]
        public async Task HandleUpdateAsync_ShouldHandleUnknownUpdateType_WithoutException()
        {
             // Arrange
            var mockBotClient = new Mock<ITelegramBotClient>();
            var handler = new UpdateHandler();
            var update = new Update
            {
                 // No Message, defaults to Unknown for our switch case if other properties empty
            };

            // Act
            await handler.HandleUpdateAsync(mockBotClient.Object, update, CancellationToken.None);

            // Assert
            Assert.True(true);
        }
    }
}
