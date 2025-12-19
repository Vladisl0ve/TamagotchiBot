using MaintanceAlertBot.Handlers;
using TamagotchiBot.Services.Mongo;
using System.Globalization;

namespace MaintanceAlertBot.Tests
{
    public class UpdateHandlerTests
    {
        // Subclass to override GetUserCulture and avoid using real UserService
        public class TestableUpdateHandler : UpdateHandler
        {
            public TestableUpdateHandler(UserService userService) : base(userService)
            {
            }

            protected override CultureInfo GetUserCulture(long userId)
            {
                return new CultureInfo("en-US");
            }
        }
    }
}
