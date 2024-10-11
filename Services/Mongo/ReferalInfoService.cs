using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using TamagotchiBot.Database;
using TamagotchiBot.Models;
using TamagotchiBot.Models.Mongo;
using static TamagotchiBot.UserExtensions.Constants;

namespace TamagotchiBot.Services.Mongo
{
    public class ReferalInfoService(ITamagotchiDatabaseSettings settings, UserService userService) : MongoServiceBase<ReferalInfo>(settings)
    {
        private readonly UserService _userService = userService;

        public List<ReferalInfo> GetAll() => _collection.Find(c => true).ToList();

        public ReferalInfo Get(long creatorUserId) => _collection.Find(c => c.CreatorUserId == creatorUserId).FirstOrDefault();
        public int GetDoneRefsAmount(long creatorUserId) => _collection.Find(c => c.CreatorUserId == creatorUserId).FirstOrDefault()?.RefUsers.Count(r => r.IsTaskDone) ?? 0;
        public long CountAllRefUsers() => _collection.Find(c => true).ToList().SelectMany(r => r.RefUsers).LongCount(r => r.IsTaskDone);

        public ReferalInfo Create(ReferalInfo refInfo)
        {
            refInfo.Created = DateTime.UtcNow;

            if (Get(refInfo.CreatorUserId) == null)
                _collection.InsertOne(refInfo);
            return refInfo;
        }

        public (ReferalInfo refResult, bool isSuccess) AddNewReferal(long creatorUserId, long newRefUserId)
        {
            bool isSuccess = false;
            var refInfoDB = Get(creatorUserId);
            refInfoDB ??= Create(new ReferalInfo()
            {
                CreatorUserId = creatorUserId,
                RefUsers = new List<ReferalUserModel>()
            });

            if (!refInfoDB.RefUsers.Exists(r => r.RefUserId == newRefUserId) && creatorUserId != newRefUserId)
            {
                refInfoDB.RefUsers.Add(new ReferalUserModel()
                {
                    RefUserId = newRefUserId,
                    IsTaskDone = false
                });
                Update(creatorUserId, refInfoDB);
                _userService.UpdateReferaledBy(newRefUserId, creatorUserId);
                isSuccess = true;
            }

            return (refInfoDB, isSuccess);
        }

        public bool UpdateTaskDone(long userId, bool isTaskDone)
        {
            var userDB = _userService.Get(userId);
            if (userDB != null && userDB.ReferaledBy != 0)
            {
                var creatorUserId = userDB.ReferaledBy;
                var refInfoOfCreator = Get(creatorUserId);
                if (refInfoOfCreator != null && refInfoOfCreator.RefUsers.Exists(u => u.RefUserId == userId))
                {
                    var refUsers = refInfoOfCreator.RefUsers;
                    refUsers.Remove(refUsers.Find(u => u.RefUserId == userId));
                    refUsers.Add(new ReferalUserModel()
                    {
                        RefUserId = userId,
                        IsTaskDone = isTaskDone
                    });
                    refInfoOfCreator.RefUsers = refUsers;
                    Update(creatorUserId, refInfoOfCreator);

                    if (isTaskDone)
                        _userService.UpdateGold(creatorUserId, (_userService.Get(creatorUserId)?.Gold ?? 0) + Rewards.ReferalAdded);

                    return true;
                }
            }
            return false;
        }

        public ReferalInfo Update(long creatorUserId, ReferalInfo refInfo)
        {
            refInfo.Updated = DateTime.UtcNow;
            _collection.ReplaceOne(c => c.CreatorUserId == creatorUserId, refInfo);
            return refInfo;
        }

        public void Remove(long creatorUserId) => _collection.DeleteOne(u => u.CreatorUserId == creatorUserId);
    }
}
