using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TamagotchiBot.Models;
using TamagotchiBot.Services;
using Telegram.Bots;

namespace TamagotchiBot.Controllers
{
    public class InteractiveController
    {
        private readonly UserService userService;
        private readonly PetService petService;

        private readonly Models.User user;
        private readonly Pet pet;

        public InteractiveController(UserService userService,
                                     PetService petService)
        {
            this.userService = userService;
            this.petService = petService;
        }
    }
}
