﻿using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace TamagotchiBot.Models.Mongo
{
    public class AllUsersData : MongoModelBase
    {
        #region Private
        [BsonElement("UserId")]
        public long UserId { get; set; }

        [BsonElement("Username")]
        public string Username { get; set; }

        [BsonElement("ChatId")]
        public long ChatId { get; set; }

        [BsonElement("LastMessage")]
        public string LastMessage { get; set; }

        [BsonElement("Culture")]
        public string Culture { get; set; }

        [BsonElement("MessageCounter")]
        public long MessageCounter { get; set; }

        [BsonElement("CallbacksCounter")]
        public long CallbacksCounter { get; set; }

        [BsonElement("BreadEatenCounter")]
        public long BreadEatenCounter { get; set; }

        [BsonElement("GoldEarnedCounter")]
        public long GoldEarnedCounter { get; set; }

        [BsonElement("GoldSpentCounter")]
        public long GoldSpentCounter { get; set; }

        [BsonElement("AppleEatenCounter")]
        public long AppleEatenCounter { get; set; }

        [BsonElement("ChocolateEatenCounter")]
        public long ChocolateEatenCounter { get; set; }

        [BsonElement("LollypopEatenCounter")]
        public long LollypopEatenCounter { get; set; }

        [BsonElement("SleepenTimesCounter")]
        public long SleepenTimesCounter { get; set; }

        [BsonElement("DicePlayedCounter")]
        public long DicePlayedCounter { get; set; }

        [BsonElement("WorkOnPCCounter")]
        public long WorkOnPCCounter { get; set; } 
        
        [BsonElement("WorkFlyersCounter")]
        public long WorkFlyersCounter { get; set; }

        [BsonElement("AppleGamePlayedCounter")]
        public long AppleGamePlayedCounter { get; set; }

        [BsonElement("CardsPlayedCounter")]
        public long CardsPlayedCounter { get; set; }

        [BsonElement("PillEatenCounter")]
        public long PillEatenCounter { get; set; }

        [BsonElement("ExtraInfoShowedTimesCounter")]
        public long ExtraInfoShowedTimesCounter { get; set; }

        [BsonElement("KitchenCommandCounter")]
        public long KitchenCommandCounter { get; set; }

        [BsonElement("PetCommandCounter")]
        public long PetCommandCounter { get; set; }

        [BsonElement("FarmCommandCounter")]
        public long FarmCommandCounter { get; set; }

        [BsonElement("ChangeTypeButtonCounter")]
        public long ChangeTypeButtonCounter { get; set; }  
        
        [BsonElement("ChangedToCatCounter")]
        public long ChangedToCatCounter { get; set; }

        [BsonElement("ChangedToDogCounter")]
        public long ChangedToDogCounter { get; set; }

        [BsonElement("ChangedToFoxCounter")]
        public long ChangedToFoxCounter { get; set; }

        [BsonElement("ChangedToMouseCounter")]
        public long ChangedToMouseCounter { get; set; }

        [BsonElement("ChangedToPandaCounter")]
        public long ChangedToPandaCounter { get; set; }

        [BsonElement("SleepCommandCounter")]
        public long SleepCommandCounter { get; set; }

        [BsonElement("GameroomCommandCounter")]
        public long GameroomCommandCounter { get; set; }

        [BsonElement("RanksCommandCounter")]
        public long RanksCommandCounter { get; set; }

        [BsonElement("HospitalCommandCounter")]
        public long HospitalCommandCounter { get; set; }

        [BsonElement("BathroomCommandCounter")]
        public long BathroomCommandCounter { get; set; }

        [BsonElement("WorkCommandCounter")]
        public long WorkCommandCounter { get; set; }

        [BsonElement("RewardCommandCounter")]
        public long RewardCommandCounter { get; set; }

        [BsonElement("MenuCommandCounter")]
        public long MenuCommandCounter { get; set; }

        [BsonElement("LanguageCommandCounter")]
        public long LanguageCommandCounter { get; set; }

        [BsonElement("HelpCommandCounter")]
        public long HelpCommandCounter { get; set; }

        [BsonElement("RenameCommandCounter")]
        public long RenameCommandCounter { get; set; }
#endregion

        #region MP
        [BsonElement("DuelsStartedCounter")]
        public long DuelsStartedCounter { get; set; }
        
        [BsonElement("DuelsAcceptedCounter")]
        public long DuelsAcceptedCounter { get; set; }
        
        [BsonElement("DuelsWinCounter")]
        public long DuelsWinCounter { get; set; }

        #endregion
    }
}
