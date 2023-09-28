using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TamagotchiBot.Database;
using TamagotchiBot.Models;
using TamagotchiBot.Models.Mongo;
using static TamagotchiBot.UserExtensions.Constants;

namespace TamagotchiBot.Services.Mongo
{
    public class ReferalInfoService : MainConnectService
    {
        private readonly IMongoCollection<ReferalInfo> _refInfos;
        private readonly UserService _userService;

        public ReferalInfoService(ITamagotchiDatabaseSettings settings, UserService userService) : base(settings)
        {
            _userService = userService;
            _refInfos = base.GetCollection<ReferalInfo>(nameof(ReferalInfo));
        }

        public List<ReferalInfo> GetAll() => _refInfos.Find(c => true).ToList();

        public ReferalInfo Get(long creatorUserId) => _refInfos.Find(c => c.CreatorUserId == creatorUserId).FirstOrDefault();
        public int GetDoneRefsAmount(long creatorUserId) => _refInfos.Find(c => c.CreatorUserId == creatorUserId).FirstOrDefault()?.RefUsers.Count(r => r.IsTaskDone) ?? 0;

        public ReferalInfo Create(ReferalInfo refInfo)
        {
            if (Get(refInfo.CreatorUserId) == null)
                _refInfos.InsertOne(refInfo);
            return refInfo;
        }

        public ReferalInfo AddNewReferal(long creatorUserId, long newRefUserId)
        {
            var refInfoDB = Get(creatorUserId);
            refInfoDB ??= Create(new ReferalInfo()
            {
                CreatorUserId = creatorUserId,
                RefUsers = new List<ReferalUserModel>()
            });

            if (!refInfoDB.RefUsers.Any(r => r.RefUserId == newRefUserId))
            {
                refInfoDB.RefUsers.Add(new ReferalUserModel()
                {
                    RefUserId = newRefUserId,
                    IsTaskDone = false
                });
                Update(creatorUserId, refInfoDB);
            }

            _userService.UpdateReferaledBy(newRefUserId, creatorUserId);
            return refInfoDB;
        }

        public bool UpdateTaskDone(long userId, bool isTaskDone)
        {
            var userDB = _userService.Get(userId);
            if (userDB != null && userDB.ReferaledBy != 0)
            {
                var creatorUserId = userDB.ReferaledBy;
                var refInfoOfCreator = Get(creatorUserId);
                if (refInfoOfCreator != null && refInfoOfCreator.RefUsers.Any(u => u.RefUserId == userId))
                {
                    var refUsers = refInfoOfCreator.RefUsers;
                    refUsers.Remove(refUsers.FirstOrDefault(u => u.RefUserId == userId));
                    refUsers.Add(new ReferalUserModel()
                    {
                        RefUserId = userId,
                        IsTaskDone = isTaskDone
                    });
                    refInfoOfCreator.RefUsers = refUsers;
                    Update(creatorUserId, refInfoOfCreator);

                    if (isTaskDone)
                        _userService.UpdateGold(creatorUserId, _userService.Get(creatorUserId).Gold += Rewards.ReferalAdded);

                    return true;
                }
            }
            return false;
        }

        public ReferalInfo Update(long creatorUserId, ReferalInfo refInfo)
        {
            _refInfos.ReplaceOne(c => c.CreatorUserId == creatorUserId, refInfo);
            return refInfo;
        }

        public void Remove(long creatorUserId) => _refInfos.DeleteOne(u => u.CreatorUserId == creatorUserId);
    }
}
