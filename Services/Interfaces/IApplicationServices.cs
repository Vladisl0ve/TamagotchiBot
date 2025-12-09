using System;
using TamagotchiBot.Services.Mongo;

namespace TamagotchiBot.Services.Interfaces
{
    public interface IApplicationServices
    {
        AdsProducersService AdsProducersService { get; }
        AllUsersDataService AllUsersDataService { get; }
        AppleGameDataService AppleGameDataService { get; }
        BannedUsersService BannedUsersService { get; }

        [Obsolete]
        ChatService ChatService { get; }

        ChatsMPService ChatsMPService { get; }
        DailyInfoService DailyInfoService { get; }
        PetService PetService { get; }
        SInfoService SInfoService { get; }
        UserService UserService { get; }
        MetaUserService MetaUserService { get; }
        BotControlService BotControlService { get; }
        ReferalInfoService ReferalInfoService { get; }
        DiamondService DiamondService { get; }
        TicTacToeGameDataService TicTacToeGameDataService { get; }
    }
}
