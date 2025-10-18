using EpinelPS.Data;
using EpinelPS.Database;

namespace EpinelPS.Utils
{
    public static class InterceptionHelper
    {
        public static InterceptionClearResult Clear(User user, int type, int id, long damage = 0)
        {
            InterceptionClearResult response = new();

            // type: 0 leveld 1 normal levels, 2 special

            int GroupId;
            List<int> percentGroupIds = [];

            // Validate type and id
            if ((type == 0 || type == 1) && GameData.Instance.InterceptNormal.TryGetValue(id, out InterceptNormalRecord? temp))
            {
                GroupId = temp.ConditionRewardGroup;
                percentGroupIds.Add(temp.PercentConditionRewardGroup);
            }
            else if (type == 2 && GameData.Instance.InterceptSpecial.TryGetValue(id, out InterceptSpecialRecord? SpecialTemp))
            {
                GroupId = SpecialTemp.ConditionRewardGroup;
                percentGroupIds.Add(SpecialTemp.PercentConditionRewardGroup);
            }
            else if (type == 3 && GameData.Instance.InterceptAnomalous.TryGetValue(id, out InterceptAnomalousRecord_Raw? AnomalousTemp))
            {
                GroupId = AnomalousTemp.ConditionRewardGroup;
                percentGroupIds.AddRange([.. AnomalousTemp.PercentConditionRewardGroups.Select(gid => gid.PercentConditionRewardGroup)]);
            }
            else
            {
                Logging.WriteLine($"Invalid interception type {type} or id {id}", LogType.Error);
                return response;
            }



            // Calculate rewards based on damage
            // Normal reward is based on condition_reward_group
            int normReward = GameData.Instance.GetConditionReward(GroupId, damage);
            if (normReward != 0)
            {
                response.NormalReward = RewardUtils.RegisterRewardsForUser(user, normReward);
            }
            else
            {
                Logging.WriteLine($"No normal reward found for condition group {GroupId} with damage {damage}", LogType.Warning);
            }

            // Bonus reward is based on percent_condition_reward_group
            foreach (var percentGroupId in percentGroupIds)
            {
                if (percentGroupId != 0)
                {
                    int percentReward = GameData.Instance.GetConditionReward(percentGroupId, damage);
                    if (percentReward != 0)
                    {
                        response.BonusReward = RewardUtils.RegisterRewardsForUser(user, percentReward, true);
                    }
                }
            }

            JsonDb.Save();

            return response;
        }
    }

    public class InterceptionClearResult
    {
        public NetRewardData NormalReward = new();
        public NetRewardData BonusReward = new();
    }
}