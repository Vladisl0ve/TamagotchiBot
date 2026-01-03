using System;
using System.Linq;
using TamagotchiBot.Services.Interfaces;
using TamagotchiBot.UserExtensions;

namespace TamagotchiBot.Services.Helpers
{
    public static class DevNotifyHelper
    {
        public static string UpdateAndGetDevNotifyReport(IApplicationServices appServices)
        {
            int playedUsersToday = appServices.AllUsersDataService.GetAll().Count(p => p.Updated.Date == DateTime.UtcNow.Date);

            var dailyInfoDB = appServices.DailyInfoService.GetToday() ?? appServices.DailyInfoService.CreateDefault();
            long messagesSent = appServices.AllUsersDataService.GetAll().Select(u => u.MessageCounter).Sum();
            long messagesSentToday = messagesSent - (appServices.DailyInfoService.GetAll().Where(u => u.DateInfo.Date <= DateTime.UtcNow.AddDays(-1).Date).OrderByDescending(i => i.DateInfo).FirstOrDefault()?.MessagesSent ?? 0);
            long callbacksSent = appServices.AllUsersDataService.GetAll().Select(u => u.CallbacksCounter).Sum();
            long callbacksSentToday = callbacksSent - (appServices.DailyInfoService.GetAll().Where(u => u.DateInfo.Date <= DateTime.UtcNow.AddDays(-1).Date).OrderByDescending(i => i.DateInfo).FirstOrDefault()?.CallbacksSent ?? 0);
            long registeredPets = appServices.PetService.Count();
            long registeredPetsShortAFK = appServices.PetService.CountShortAFK();
            long registeredPetsMediumAFK = appServices.PetService.CountMediumAFK();
            long registeredPetsLongAFK = appServices.PetService.CountLongAFK();
            long registeredUsers = appServices.UserService.Count();
            var allAUDUsers = appServices.AllUsersDataService.CountAllAUD();
            var allRefUsers = appServices.ReferalInfoService.CountAllRefUsers();
            var allPetsLastWeek = appServices.PetService.CountLastWeekPlayed(); //PetsLastWeek == PLW

            string deltaUsers = "-", deltaPets = "-", deltaPetsShortAFK = "-", deltaPetsMediumAFK = "-", deltaPetsLongAFK = "-", deltaAUD = "-", deltaRef = "-", deltaPLW = "-";
            var prevDay = appServices.DailyInfoService.GetPreviousDay();
            if (prevDay != default)
            {
                deltaPets = (registeredPets - prevDay.PetCounter).ToStringWithCommas();
                deltaPetsShortAFK = (registeredPetsShortAFK - prevDay.PetShortAFKCounter).ToStringWithCommas();
                deltaPetsMediumAFK = (registeredPetsMediumAFK - prevDay.PetMediumAFKCounter).ToStringWithCommas();
                deltaPetsLongAFK = (registeredPetsLongAFK - prevDay.PetLongAFKCounter).ToStringWithCommas();
                deltaUsers = (registeredUsers - prevDay.UserCounter).ToStringWithCommas();
                deltaAUD = (allAUDUsers - prevDay.AUDCounter).ToStringWithCommas();
                deltaRef = (allRefUsers - prevDay.ReferalsCounter).ToStringWithCommas();
                deltaPLW = (allPetsLastWeek - prevDay.PetPlayedLastWeek).ToStringWithCommas();
            }

            dailyInfoDB.UsersPlayed = playedUsersToday;
            dailyInfoDB.MessagesSent = messagesSent;
            dailyInfoDB.CallbacksSent = callbacksSent;
            dailyInfoDB.DateInfo = DateTime.UtcNow;
            dailyInfoDB.TodayCallbacks = (int)callbacksSentToday;
            dailyInfoDB.TodayMessages = (int)messagesSentToday;
            dailyInfoDB.UserCounter = registeredUsers;
            dailyInfoDB.PetCounter = registeredPets;
            dailyInfoDB.PetShortAFKCounter = registeredPetsShortAFK;
            dailyInfoDB.PetMediumAFKCounter = registeredPetsMediumAFK;
            dailyInfoDB.PetLongAFKCounter = registeredPetsLongAFK;
            dailyInfoDB.AUDCounter = allAUDUsers;
            dailyInfoDB.ReferalsCounter = allRefUsers;
            dailyInfoDB.PetPlayedLastWeek = allPetsLastWeek;

            appServices.DailyInfoService.UpdateOrCreate(dailyInfoDB);
            string text = $"{dailyInfoDB.DateInfo:G}" + Environment.NewLine
                        + $"Played pets TODAY: {playedUsersToday.ToStringWithCommas()}" + Environment.NewLine
                        + $"Played pets   WEEK: {allPetsLastWeek.ToStringWithCommas()}" + Environment.NewLine + Environment.NewLine
                        + $"Messages     TODAY: {messagesSentToday.ToStringWithCommas()}" + Environment.NewLine
                        + $"Callbacks    TODAY: {callbacksSentToday.ToStringWithCommas()}" + Environment.NewLine
                        + $"------------------------" + Environment.NewLine
                        + $"Users: {registeredUsers.ToStringWithCommas()}" + Environment.NewLine
                        + $"ΔUsers: {deltaUsers}" + Environment.NewLine 
                        + Environment.NewLine
                        + $"Pets: {registeredPets.ToStringWithCommas()}" + Environment.NewLine
                        + $"ΔPets: {deltaPets}" + Environment.NewLine
                        + Environment.NewLine
                        + $"PetsShortAFK: {registeredPetsShortAFK}" + Environment.NewLine
                        + $"ΔPetsShortAFK: {deltaPetsShortAFK}" + Environment.NewLine + Environment.NewLine
                        + $"PetsMediumAFK: {registeredPetsMediumAFK}" + Environment.NewLine
                        + $"ΔPetsMediumAFK: {deltaPetsMediumAFK}" + Environment.NewLine + Environment.NewLine
                        + $"PetsLongAFK: {registeredPetsLongAFK}" + Environment.NewLine
                        + $"ΔPetsLongAFK: {deltaPetsLongAFK}" + Environment.NewLine
                        + Environment.NewLine
                        + $"AUD: {allAUDUsers.ToStringWithCommas()}" + Environment.NewLine
                        + $"ΔAUD: {deltaAUD}" + Environment.NewLine
                        + Environment.NewLine
                        + $"ΔPLW: {deltaPLW}" + Environment.NewLine
                        + $"ΔReferals: {deltaRef}" + Environment.NewLine
                         + $"Referals: {allRefUsers.ToStringWithCommas()}" + Environment.NewLine
                        + $"------------------------" + Environment.NewLine
                        + $"Messages sent: {messagesSent.ToStringWithCommas()}" + Environment.NewLine
                        + $"Callbacks sent  : {callbacksSent.ToStringWithCommas()}";

            return text;
        }
    }
}
