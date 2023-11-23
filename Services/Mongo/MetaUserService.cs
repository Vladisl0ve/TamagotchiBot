using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TamagotchiBot.Database;
using TamagotchiBot.Models.Mongo;
using Telegram.Bot.Types;

namespace TamagotchiBot.Services.Mongo
{
    public class MetaUserService : MainConnectService
    {
        private readonly IMongoCollection<MetaUser> _metausers;

        public MetaUserService(ITamagotchiDatabaseSettings settings) : base(settings)
        {
            _metausers = base.GetCollection<MetaUser>(nameof(MetaUser));
        }

        public List<MetaUser> GetAll() => _metausers.Find(u => true).ToList();
        public MetaUser Get(long userId) => _metausers.Find(u => u.UserId == userId).FirstOrDefault();
        public MetaUser Update(long userId, MetaUser userIn)
        {
            _metausers.ReplaceOne(u => u.UserId == userId, userIn);
            return userIn;
        }
        public MetaUser Create(MetaUser user)
        {
            _metausers.InsertOne(user);
            return user;
        }

        public bool UpdateIsPetNameAskedOnRename(long userId, bool isPetNameAskedOnRename)
        {
            var userDb = _metausers.Find(u => u.UserId == userId).FirstOrDefault();
            userDb ??= Create(new MetaUser() { UserId = userId });

            userDb.IsPetNameAskedOnRename = isPetNameAskedOnRename;
            _metausers.ReplaceOne(u => u.UserId == userId, userDb);
            return true;
        }
        public bool UpdateIsAskedToConfirmRenaming(long userId, bool isConfirmedPetRenaming)
        {
            var userDb = _metausers.Find(u => u.UserId == userId).FirstOrDefault();
            userDb ??= Create(new MetaUser() { UserId = userId });

            userDb.IsAskedToConfirmRenaming = isConfirmedPetRenaming;
            _metausers.ReplaceOne(u => u.UserId == userId, userDb);
            return true;
        }
        public bool UpdateTmpPetName(long userId, string tmpName)
        {
            var userDb = _metausers.Find(u => u.UserId == userId).FirstOrDefault();
            if (userDb == null)
                return false;

            userDb.TmpPetName = tmpName;
            _metausers.ReplaceOne(u => u.UserId == userId, userDb);
            return true;
        }
        public bool UpdateMsgDuelId(long userId, int msgId)
        {
            var userDb = _metausers.Find(u => u.UserId == userId).FirstOrDefault();
            if (userDb == null)
                return false;

            userDb.MsgDuelId = msgId;
            _metausers.ReplaceOne(u => u.UserId == userId, userDb);
            return true;
        }
    }
}
