using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;
using TamagotchiBot.Extensions;
using TamagotchiBot.Models;
using TamagotchiBot.Services;
using Telegram.Bot.Types;
using User = TamagotchiBot.Models.User;

namespace TamagotchiBot.Controllers
{
    public class GameController
    {
        private readonly UserService _userService;
        private readonly PetService _petService;
        private readonly Message message;

        private CultureInfo localCulture;

        public GameController(UserService userService, PetService petService, Message message)
        {
            _userService = userService;
            _petService = petService;
            this.message = message;

        }

        public string Start()
        {
            var userMessage = message.From;
            User userDb = _userService.Get(userMessage.Id);
            if (userDb == null)
            {
                _userService.Create(userMessage);
                Log.Information($"User {userMessage.Username} has been added to Db");

                localCulture = new CultureInfo(userMessage.LanguageCode);
                Resources.Resources.Culture = localCulture;
                return Resources.Resources.Welcome;
            }
            else if (userMessage.Username != userDb.Username || userMessage.LastName != userDb.LastName || userMessage.FirstName != userDb.FirstName)
            {
                _userService.Update(userDb.UserId, userMessage);
                Log.Information($"User {userMessage.Username} has been updated in Db");

                var pet = _petService.Get(userDb.UserId);
                if (pet == null || pet.Name == null)
                    return CreatePet();
                else
                    return CommandHandler();
            }
            else
            {
                var pet = _petService.Get(userDb.UserId);
                if (pet == null || pet.Name == null)
                    return CreatePet();
                else
                    return CommandHandler();
            }
        }

        private string CreatePet()
        {
            if (message.Text == null)
                return null;

            Pet pet = _petService.Get(message.From.Id);

            if (pet == null)
            {
                _petService.Create(new Pet()
                {
                    Name = null,
                    Level = 1,
                    BirthDateTime = DateTime.UtcNow,
                    EXP = 0,
                    HP = 100,
                    Joy = 100,
                    Starving = 0,
                    Type = null,
                    UserId = message.From.Id
                });
                return Resources.Resources.ChooseName;
            }

            if (pet.Name == null)
            {
                _petService.UpdateName(message.From.Id, message.Text);
                return Resources.Resources.ConfirmedName;
            }

            return null;
        }

        public string ExtrasHandler() //catching exceptional situations (but not exceptions!)
        {
            User user = _userService.Get(message.From.Id);

            if (user.Culture == null)
            {
                if (message.Text == null)
                    return null;

                string last = message.Text.Split(' ').Last();
                string culture = last.Culture();

                if (culture == null)
                    return null;

                localCulture = new CultureInfo(culture);
                Resources.Resources.Culture = localCulture;

                _userService.UpdateLanguage(user.UserId, culture);
                return Resources.Resources.ConfirmedLanguage;
            }

            return null;
        }

        public string CommandHandler()
        {
            string textRecieved = message.Text;
            if (textRecieved == null)
                return null;

            textRecieved = textRecieved.ToLower();
            if (textRecieved == "/pet")
            {
                Pet pet = _petService.Get(message.From.Id);
                string toSend = string.Format(Resources.Resources.petCommand, pet.Name, pet.BirthDateTime, pet.HP, pet.EXP, pet.Level);

                return toSend;
            }

            if (textRecieved == "/language")
            {
                User user = _userService.UpdateLanguage(message.From.Id, null);
                return Resources.Resources.ChangeLanguage;

            }

            if (textRecieved == "/bathroom")
            {
                return Resources.Resources.DevelopWarning;
            }

            if (textRecieved == "/kitchen")
            {
                return Resources.Resources.DevelopWarning;
            }

            if (textRecieved == "/gameroom")
            {
                return Resources.Resources.DevelopWarning;
            }

            if (textRecieved == "/ranks")
            {
                return Resources.Resources.DevelopWarning;
            }

            if (textRecieved == "/sleep")
            {
                return Resources.Resources.DevelopWarning;
            }



            return ExtrasHandler();
        }
    }
}
