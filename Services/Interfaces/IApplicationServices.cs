using TamagotchiBot.Services.Mongo;

namespace TamagotchiBot.Services.Interfaces
{
    public interface IApplicationServices
    {
        AdsProducersService AdsProducersService { get; }
        AllUsersDataService AllUsersDataService { get; }
        AppleGameDataService AppleGameDataService { get; }
        BannedUsersService BannedUsersService { get; }
        ChatService ChatService { get; }
        DailyInfoService DailyInfoService { get; }
        PetService PetService { get; }
        SInfoService SInfoService { get; }
        UserService UserService { get; }
        BotControlService BotControlService { get; }
    }
}
