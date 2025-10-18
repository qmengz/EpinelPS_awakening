using EpinelPS.Data;

namespace EpinelPS.Utils
{
    // Calculate rewards for various messages
    public class RewardUtils
    {
        public static NetRewardData RegisterRewardsForUser(User user, int rewardId, bool isPercen = false)
        {
            RewardRecord rewardData = GameData.Instance.GetRewardTableEntry(rewardId) ?? throw new Exception($"unknown reward Id {rewardId}");
            return RegisterRewardsForUser(user, rewardData, isPercen);
        }
        public static NetRewardData RegisterRewardsForUser(User user, RewardRecord rewardData, bool isPercen = false)
        {
            NetRewardData ret = new()
            {
                PassPoint = new()
            };
            if (rewardData.Rewards == null) return ret;

            if (rewardData.UserExp != 0)
            {
                int newXp = rewardData.UserExp + user.userPointData.ExperiencePoint;

                int newLevelExp = GameData.Instance.GetUserMinXpForLevel(user.userPointData.UserLevel);
                int newLevel = user.userPointData.UserLevel;

                if (newLevelExp == -1)
                {
                    Console.WriteLine("Unknown user level value for xp " + newXp);
                }

                int newGems = 0;

                while (newXp >= newLevelExp)
                {
                    newLevel++;
                    newGems += 30;
                    newXp -= newLevelExp;
                    if (user.Currency.ContainsKey(CurrencyType.FreeCash))
                        user.Currency[CurrencyType.FreeCash] += 30;
                    else
                        user.Currency.Add(CurrencyType.FreeCash, 30);

                    newLevelExp = GameData.Instance.GetUserMinXpForLevel(newLevel);
                }


                // TODO: what is the difference between IncreaseExp and GainExp
                // NOTE: Current Exp/Lv refers to after XP was added.

                ret.UserExp = new NetIncreaseExpData()
                {
                    BeforeExp = user.userPointData.ExperiencePoint,
                    BeforeLv = user.userPointData.UserLevel,

                    // IncreaseExp = rewardData.UserExp,
                    CurrentExp = newXp,
                    CurrentLv = newLevel,

                    GainExp = rewardData.UserExp,

                };
                user.userPointData.ExperiencePoint = newXp;

                user.userPointData.UserLevel = newLevel;
            }

            foreach (var item in rewardData.Rewards)
            {
                if (item.RewardType != RewardType.None)
                {
                    if (item.RewardPercent != 1000000 && isPercen)
                    {
                        Logging.WriteLine($"Rolling for reward {item.RewardId} of type {item.RewardType} with percent {item.RewardPercent / 10000.0}", LogType.Debug);
                        Random rand = new();
                        int roll = rand.Next(1, 1000000); // 1 到 1,000,000 的随机数
                        if (roll <= item.RewardPercent)
                        {
                            AddSingleObject(user, ref ret, item.RewardId, item.RewardType, item.RewardValue);
                        }
                    }
                    else
                    {
                        AddSingleObject(user, ref ret, item.RewardId, item.RewardType, item.RewardValue);
                    }
                }
            }

            return ret;
        }
        public static void AddSingleCurrencyObject(User user, ref NetRewardData ret, CurrencyType currencyType, long rewardCount)
        {
            bool found = user.Currency.Any(pair => pair.Key == currencyType);

            if (found)
            {
                user.Currency[currencyType] += rewardCount;
            }
            else
            {
                user.Currency.Add(currencyType, rewardCount);
            }
            ret.Currency.Add(new NetCurrencyData()
            {
                FinalValue = found ? user.GetCurrencyVal(currencyType) : rewardCount,
                Value = rewardCount,
                Type = (int)currencyType
            });
        }
        /// <summary>
        /// Adds a single item to users inventory, and also adds it to ret parameter.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="ret"></param>
        /// <param name="rewardId"></param>
        /// <param name="rewardType"></param>
        /// <param name="rewardCount"></param>
        /// <exception cref="Exception"></exception>
        public static void AddSingleObject(User user, ref NetRewardData ret, int rewardId, RewardType rewardType, int rewardCount)
        {
            Logging.WriteLine($"AddSingleObject: rewardId={rewardId}, rewardType={rewardType}, rewardCount={rewardCount}", LogType.Debug);
            if (rewardId == 0 || rewardType == RewardType.None) return;

            if (rewardType == RewardType.Currency)
            {
                AddSingleCurrencyObject(user, ref ret, (CurrencyType)rewardId, rewardCount);
            }
            else if (rewardType == RewardType.Item ||
                rewardType.ToString().StartsWith("Equipment_"))
            {
                // Check if user already has saId item. If it is level 1, increase item count.
                // If user does not have item, generate a new item ID
                if (user.Items.Where(x => x.ItemType == rewardId).Any()
                        && !GameData.Instance.ItemEquipTable.TryGetValue(rewardId, out _)) // Equipment items do not stack
                {
                    ItemData? newItem = user.Items.Where(x => x.ItemType == rewardId).FirstOrDefault();
                    if (newItem != null)
                    {
                        // Increase item count
                        // Except if it's a harmony cube, those do not stack
                        if (GameData.Instance.ItemHarmonyCubeTable.TryGetValue(rewardId, out _))
                        {
                            newItem.Count = rewardCount;
                        }
                        else
                        {
                            newItem.Count += rewardCount;
                        }

                        // Tell the client the reward and its amount
                        ret.Item.Add(new NetItemData()
                        {
                            Count = rewardCount,
                            Tid = rewardId,
                            //Isn = newItem.Isn
                        });

                        // Tell the client the new amount of this item
                        ret.UserItems.Add(NetUtils.UserItemDataToNet(newItem));
                    }
                    else
                    {
                        Logging.WriteLine($"ERROR: failed to find item {rewardId} in user inventory after confirming it exists", LogType.Error);
                    }
                }
                else
                {

                    // Add new item to user inventory
                    ItemData newItem = new()
                    {
                        ItemType = rewardId,
                        Isn = user.GenerateUniqueItemId(),
                        Level = 1,
                        Exp = 0,
                        Count = rewardCount,
                        Corp = GetCorporationId(rewardType)
                    };

                    user.Items.Add(newItem);
                    ret.Item.Add(NetUtils.ItemDataToNet(newItem));

                    // Tell the client the new amount of this item (which is the same as user dId not have item previously)
                    ret.UserItems.Add(NetUtils.UserItemDataToNet(newItem));
                }
            }
            else if (rewardType == RewardType.Memorial)
            {
                if (!user.Memorial.Contains(rewardId))
                {
                    ret.Memorial.Add(rewardId);
                    user.Memorial.Add(rewardId);
                }
            }
            else if (rewardType == RewardType.Bgm)
            {
                if (!user.JukeboxBgm.Contains(rewardId))
                {
                    ret.JukeboxBgm.Add(rewardId);
                    user.JukeboxBgm.Add(rewardId);
                }
            }
            else if (rewardType == RewardType.InfraCoreExp)
            {
                int beforeLv = user.InfraCoreLvl;
                int beforeExp = user.InfraCoreExp;

                user.InfraCoreExp += rewardCount;

                // Check for level ups
                Dictionary<int, InfraCoreGradeRecord> gradeTable = GameData.Instance.InfracoreTable;
                int newLevel = user.InfraCoreLvl;

                foreach (var grade in gradeTable.Values.OrderBy(g => g.Grade))
                {
                    if (user.InfraCoreExp >= grade.InfraCoreExp)
                    {
                        newLevel = grade.Grade + 1;
                    }
                    else
                    {
                        break;
                    }
                }

                if (newLevel > user.InfraCoreLvl)
                {
                    user.InfraCoreLvl = newLevel;
                }

                ret.InfraCoreExp = new NetIncreaseExpData()
                {
                    BeforeLv = beforeLv,
                    BeforeExp = beforeExp,
                    CurrentLv = user.InfraCoreLvl,
                    CurrentExp = user.InfraCoreExp,
                    GainExp = rewardCount
                };
            }
            else if (rewardType == RewardType.ItemRandomBox)
            {
                ItemConsumeRecord? cItem = GameData.Instance.ConsumableItems.Where(x => x.Value.Id == rewardId).FirstOrDefault().Value;

                if (cItem.ItemSubType == ItemSubType.ItemRandomBoxList)
                {
                    NetRewardData reward = NetUtils.UseLootBox(user, rewardId, rewardCount);

                    ret = NetUtils.MergeRewards([ret, reward], user);
                }
                else
                {
                    NetItemData itm = new()
                    {
                        Count = rewardCount,
                        Tid = cItem.Id,
                        Isn = user.GenerateUniqueItemId()
                    };
                    ret.Item.Add(itm);

                    user.Items.Add(new ItemData() { Count = rewardCount, Isn = itm.Isn, ItemType = itm.Tid });
                }
            }
            else if (rewardType == RewardType.FavoriteItem)
            {

                NetUserFavoriteItemData newFavoriteItem = new()
                {
                    FavoriteItemId = user.GenerateUniqueItemId(),
                    Tid = rewardId,
                    Csn = 0,
                    Lv = 0,
                    Exp = 0
                };
                user.FavoriteItems.Add(newFavoriteItem);

                ret.UserFavoriteItems.Add(newFavoriteItem);

                NetFavoriteItemData favoriteItemData = new NetFavoriteItemData
                {
                    FavoriteItemId = newFavoriteItem.FavoriteItemId,
                    Tid = newFavoriteItem.Tid,
                    Csn = newFavoriteItem.Csn,
                    Lv = newFavoriteItem.Lv,
                    Exp = newFavoriteItem.Exp
                };
                ret.FavoriteItems.Add(favoriteItemData);

            }
            else
            {
                Logging.WriteLine($"WARNING: unknown reward type {rewardType}, id {rewardId}, count {rewardCount}", LogType.Warning);
            }
        }

        /// <summary>
        /// Get corporation ID for a given reward type. If the reward type has a random corporation setting,
        /// it will randomly select one based on the defined ratios.
        /// </summary>
        /// <param name="type"> The reward type. </param>
        /// <returns> corporation ID </returns>
        private static int GetCorporationId(RewardType type)
        {
            try
            {
                if (type.ToString().StartsWith("Equipment_"))
                {
                    var equipCorpSetting = GameData.Instance.ItemEquipCorpSettingTable.Values.FirstOrDefault(x => x.Key == type);
                    if (equipCorpSetting != null)
                    {
                        if (equipCorpSetting.CorpType == CorporationType.RANDOM)
                        {
                            List<int> crops = [];
                            if (equipCorpSetting.RatioNone != 0) crops.Add(0);
                            if (equipCorpSetting.RatioMissilis != 0) crops.Add(1);
                            if (equipCorpSetting.RatioElysion != 0) crops.Add(2);
                            if (equipCorpSetting.RatioTetra != 0) crops.Add(3);
                            if (equipCorpSetting.RatioPilgrim != 0) crops.Add(4);
                            if (equipCorpSetting.RatioAbnormal != 0) crops.Add(7);
                            var random = new Random();
                            int index = random.Next(crops.Count);
                            return crops[index];
                        }
                        else
                        {
                            return (int)equipCorpSetting.CorpType;
                        }
                    }

                }
                return 0;
            }
            catch (Exception ex)
            {
                Logging.WriteLine($"Error getting corporation ID for reward type {type}: {ex.Message}", LogType.Error);
                return 0;
            }
        }
    }
}
