using Quartz;
using Serilog;
using System.Threading.Tasks;
using TamagotchiBot.Models.Answers;
using TamagotchiBot.Services.Interfaces;
using TamagotchiBot.UserExtensions;

namespace TamagotchiBot.Jobs
{
    public class AutoFeedJob : IJob
    {
        private readonly IApplicationServices _appServices;
        public AutoFeedJob(IApplicationServices applicationServices)
        {
            _appServices = applicationServices;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            Log.Information("AutoFeedJob starting...");
            var petsToFeed = _appServices.PetService.GetAutoFeedingPets();

            foreach (var pet in petsToFeed)
            {
                var user = _appServices.UserService.Get(pet.UserId);
                if (user == null) continue;

                if (user.AutoFeedCharges > 0)
                {
                    // Feed
                    var newSatiety = pet.Satiety + Constants.AutoFeed.AutoFeedAmount;
                    if (newSatiety > 100) newSatiety = 100;

                    _appServices.PetService.UpdateSatiety(pet.UserId, newSatiety);
                    _appServices.UserService.UpdateAutoFeedCharges(pet.UserId, user.AutoFeedCharges - 1);

                    Log.Information($"AutoFed pet {pet.Name} (User: {pet.UserId}). Charges left: {user.AutoFeedCharges - 1}");

                    // Notify user
                    var userCulture = new System.Globalization.CultureInfo(user.Culture ?? "ru");
                    await _appServices.BotControlService.SendAnswerMessageAsync(new AnswerMessage()
                    {
                        Text = string.Format(nameof(Resources.Resources.autoFeedUsedNotification).UseCulture(userCulture), pet.Name, Constants.AutoFeed.AutoFeedAmount, user.AutoFeedCharges - 1),
                        ParseMode = Telegram.Bot.Types.Enums.ParseMode.Html,
                        StickerId = Constants.StickersId.PetAutoFeederUsedSticker
                    }, pet.UserId);

                    if (user.AutoFeedCharges - 1 == 0)
                    {
                        _appServices.PetService.UpdateIsAutoFeedEnabled(pet.UserId, false);
                        await _appServices.BotControlService.SendAnswerMessageAsync(new AnswerMessage()
                        {
                            Text = Resources.Resources.autoFeedDisabled,
                            ParseMode = Telegram.Bot.Types.Enums.ParseMode.Html
                        }, pet.UserId);
                    }
                }
                else
                {
                    // Should not happen usually if we disable it when it hits 0, but just in case
                    _appServices.PetService.UpdateIsAutoFeedEnabled(pet.UserId, false);
                }
            }
            Log.Information("AutoFeedJob finished.");
        }
    }
}
