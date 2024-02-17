using MongoDB.Driver;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using TamagotchiBot.Database;
using TamagotchiBot.Models.Mongo;
using TamagotchiBot.UserExtensions;

namespace TamagotchiBot.Services.Mongo
{
    public class MetaUserService : MainConnectService
    {
        private readonly IMongoCollection<MetaUser> _metausers;

        public MetaUserService(ITamagotchiDatabaseSettings settings) : base(settings)
        {
            _metausers = base.GetCollection<MetaUser>();
        }

        public List<MetaUser> GetAll() => _metausers.Find(u => true).ToList();
        public MetaUser Get(long userId) => _metausers.Find(u => u.UserId == userId).FirstOrDefault();

        public List<(string userQ, string chatGPTA, DateTime revision)> GetLastChatGPTQA(long userId)
        {
            List<(string userQ, string chatGPTA, DateTime revision)> result = new List<(string userQ, string chatGPTA, DateTime revision)>();
            List<string> resultChatGPT = GetLastChatGPTQA_RAW(userId);

            foreach (var resultik in resultChatGPT)
            {
                var stringAiO = resultik.Split('|');
                if (stringAiO.Length != 3)
                {
                    Log.Error($"BD ChatGPT answer is wrong! [{resultik}], userId: {userId}");
                    continue;
                }

                //$"{userQ}|{chatGptA}|{DateTime.UtcNow:R}"
                result.Add(new()
                {
                    userQ = stringAiO[0],
                    chatGPTA = stringAiO[1],
                    revision = DateTime.ParseExact(stringAiO[2], "R", System.Globalization.CultureInfo.InvariantCulture)
                });
            }

            return result;
        }
        private List<string> GetLastChatGPTQA_RAW(long userId)
        {
            var result = _metausers.Find(u => u.UserId == userId).FirstOrDefault()?.LastChatGptQA;
            if (result == null || result.Count <= 0)
                return new List<string>();

            return result;
        }

        public MetaUser Update(long userId, MetaUser userIn)
        {
            userIn.Updated = DateTime.UtcNow;
            _metausers.ReplaceOne(u => u.UserId == userId, userIn);
            return userIn;
        }
        public MetaUser Create(MetaUser user)
        {
            user.Created = DateTime.UtcNow;
            _metausers.InsertOne(user);
            return user;
        }

        public void Remove(long userId) => _metausers.DeleteOne(u => u.UserId == userId);

        public bool UpdateIsPetNameAskedOnRename(long userId, bool isPetNameAskedOnRename)
        {
            var userDb = _metausers.Find(u => u.UserId == userId).FirstOrDefault();
            userDb ??= Create(new MetaUser() { UserId = userId });

            userDb.IsPetNameAskedOnRename = isPetNameAskedOnRename;
            userDb.Updated = DateTime.UtcNow;
            _metausers.ReplaceOne(u => u.UserId == userId, userDb);
            return true;
        }
        public bool UpdateIsAskedToConfirmRenaming(long userId, bool isConfirmedPetRenaming)
        {
            var userDb = _metausers.Find(u => u.UserId == userId).FirstOrDefault();
            userDb ??= Create(new MetaUser() { UserId = userId });

            userDb.IsAskedToConfirmRenaming = isConfirmedPetRenaming;
            userDb.Updated = DateTime.UtcNow;
            _metausers.ReplaceOne(u => u.UserId == userId, userDb);
            return true;
        }
        public bool UpdateTmpPetName(long userId, string tmpName)
        {
            var userDb = _metausers.Find(u => u.UserId == userId).FirstOrDefault();
            userDb ??= Create(new MetaUser() { UserId = userId });

            userDb.TmpPetName = tmpName;
            userDb.Updated = DateTime.UtcNow;
            _metausers.ReplaceOne(u => u.UserId == userId, userDb);
            return true;
        }
        public bool UpdateMsgDuelId(long userId, int msgId)
        {
            var userDb = _metausers.Find(u => u.UserId == userId).FirstOrDefault();
            userDb ??= Create(new MetaUser() { UserId = userId });

            userDb.MsgDuelId = msgId;
            userDb.Updated = DateTime.UtcNow;
            _metausers.ReplaceOne(u => u.UserId == userId, userDb);
            return true;
        }
        public bool UpdateMsgCreatorDuelId(long userId, int msgId)
        {
            var userDb = _metausers.Find(u => u.UserId == userId).FirstOrDefault();
            userDb ??= Create(new MetaUser() { UserId = userId });

            userDb.MsgCreatorDuelId = msgId;
            userDb.Updated = DateTime.UtcNow;
            _metausers.ReplaceOne(u => u.UserId == userId, userDb);
            return true;
        }

        public bool UpdateChatDuelId(long userId, long chatId)
        {
            var userDb = _metausers.Find(u => u.UserId == userId).FirstOrDefault();
            userDb ??= Create(new MetaUser() { UserId = userId });

            userDb.ChatDuelId = chatId;
            userDb.Updated = DateTime.UtcNow;
            _metausers.ReplaceOne(u => u.UserId == userId, userDb);
            return true;
        }

        public bool UpdateDuelStartTime(long userId, DateTime startTime)
        {
            var userDb = _metausers.Find(u => u.UserId == userId).FirstOrDefault();
            userDb ??= Create(new MetaUser() { UserId = userId });

            userDb.DuelStartTime = startTime;
            userDb.Updated = DateTime.UtcNow;
            _metausers.ReplaceOne(u => u.UserId == userId, userDb);
            return true;
        }

        public bool UpdateIsFeedingMPStarted(long userId, bool isStarted)
        {
            var userDb = _metausers.Find(u => u.UserId == userId).FirstOrDefault();
            userDb ??= Create(new MetaUser() { UserId = userId });

            userDb.IsFeedingMPStarted = isStarted;
            userDb.Updated = DateTime.UtcNow;
            _metausers.ReplaceOne(u => u.UserId == userId, userDb);
            return true;
        }

        public bool AppendNewChatGPTQA(long userId, string userQ, string chatGptA)
        {
            return AppendNewChatGPTQA(userId, $"{userQ}|{chatGptA}|{DateTime.UtcNow:R}");
        }

        private bool AppendNewChatGPTQA(long userId, string newMsg)
        {
            var metauserDb = _metausers.Find(u => u.UserId == userId).FirstOrDefault();
            metauserDb ??= Create(new MetaUser() { UserId = userId });

            var result = new List<string>();

            if (metauserDb.LastChatGptQA != null)
                result.AddRange(metauserDb.LastChatGptQA);

            result.Add(newMsg);

            if (result.Count > Constants.QA_MAX_COUNTER)
                result.RemoveAt(0);

            metauserDb.LastChatGptQA = result;
            metauserDb.Updated = DateTime.UtcNow;
            _metausers.ReplaceOne(u => u.UserId == userId, metauserDb);
            return true;
        }

        public bool UpdateLastMPFeedingTime(long userId, DateTime startTime)
        {
            var userDb = _metausers.Find(u => u.UserId == userId).FirstOrDefault();
            userDb ??= Create(new MetaUser() { UserId = userId });

            userDb.LastMPFeedingTime = startTime;
            userDb.Updated = DateTime.UtcNow;
            _metausers.ReplaceOne(u => u.UserId == userId, userDb);
            return true;
        }

        public bool UpdateNextPossibleDuelTime(long userId, DateTime nextDuelTime)
        {
            var userDb = _metausers.Find(u => u.UserId == userId).FirstOrDefault();
            userDb ??= Create(new MetaUser() { UserId = userId });

            userDb.NextPossibleDuelTime = nextDuelTime;
            userDb.Updated = DateTime.UtcNow;
            _metausers.ReplaceOne(u => u.UserId == userId, userDb);
            return true;
        }

        internal int GetDebugMessageThreadId(long userId)
        {
            var result = _metausers.Find(u => u.UserId == userId).FirstOrDefault()?.DebugMessageThreadId;
            return result ?? 0;
        }    
        
        internal bool UpdateDebugMessageThreadId(long userId, int newMsgThreadId)
        {
            var metaUserDb = _metausers.Find(u => u.UserId == userId).FirstOrDefault();
            metaUserDb ??= Create(new MetaUser() { UserId = userId });

            metaUserDb.DebugMessageThreadId = newMsgThreadId;
            metaUserDb.Updated = DateTime.UtcNow;
            _metausers.ReplaceOne(u => u.UserId == userId, metaUserDb);
            return true;
        }
    }
}
