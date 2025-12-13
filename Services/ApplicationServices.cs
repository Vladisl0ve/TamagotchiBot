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

        public ChatsMPService ChatsMPService { get; }

        public DailyInfoService DailyInfoService { get; }

        public PetService PetService { get; }

        public SInfoService SInfoService { get; }

        public UserService UserService { get; }

        public MetaUserService MetaUserService { get; }

        public BotControlService BotControlService { get; }

        public ReferalInfoService ReferalInfoService { get; }

        public ApplicationServices(AdsProducersService adsProducersService,
                                   AllUsersDataService allUsersDataService,
                                   AppleGameDataService appleGameDataService,
                                   BannedUsersService bannedUsersService,
                                   ChatService chatService,
                                   DailyInfoService dailyInfoService,
                                   PetService petService,
                                   SInfoService sInfoService,
                                   UserService userService,
                                   MetaUserService metaUserService,
                                   ChatsMPService chatsMPService,
                                   ReferalInfoService referalInfoService,
                                   DiamondService diamondService,
                                   TicTacToeGameDataService ticTacToeGameDataService,
                                   HangmanGameDataService hangmanGameDataService,
                                   BotControlService botControlService)
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
            MetaUserService = metaUserService;
            BotControlService = botControlService;
            ChatsMPService = chatsMPService;
            ReferalInfoService = referalInfoService;
            DiamondService = diamondService;
            TicTacToeGameDataService = ticTacToeGameDataService;
            HangmanGameDataService = hangmanGameDataService;
        }

        public DiamondService DiamondService { get; }
        public TicTacToeGameDataService TicTacToeGameDataService { get; }
        public HangmanGameDataService HangmanGameDataService { get; }
    }
}
