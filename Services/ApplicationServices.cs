using TamagotchiBot.Services.Interfaces;
using TamagotchiBot.Services.Mongo;

namespace TamagotchiBot.Services
{
    public class ApplicationServices : IApplicationServices
    {
        public AdsProducersService AdsProducersService { get; }

        public AllUsersDataService AllUsersDataService { get; }

        public AppleGameDataService AppleGameDataService { get; }

        public BannedUsersService BannedUsersService { get; }

        public ChatService ChatService { get; }

        public DailyInfoService DailyInfoService { get; }

        public PetService PetService { get; }

        public SInfoService SInfoService { get; }

        public UserService UserService { get; }

        public BotControlService BotControlService { get; }

        public NotifyTimerService NotifyTimerService { get; }

        public ApplicationServices(AdsProducersService adsProducersService,
                                   AllUsersDataService allUsersDataService,
                                   AppleGameDataService appleGameDataService,
                                   BannedUsersService bannedUsersService,
                                   ChatService chatService,
                                   DailyInfoService dailyInfoService,
                                   PetService petService,
                                   SInfoService sInfoService,
                                   UserService userService,
                                   BotControlService botControlService,
                                   NotifyTimerService notifyTimerService)
        {
            AdsProducersService = adsProducersService;
            AllUsersDataService = allUsersDataService;
            AppleGameDataService = appleGameDataService;
            BannedUsersService = bannedUsersService;
            ChatService = chatService;
            DailyInfoService = dailyInfoService;
            PetService = petService;
            SInfoService = sInfoService;
            UserService = userService;
            BotControlService = botControlService;
            NotifyTimerService = notifyTimerService;
        }
    }
}
