using EpinelPS.Data;
using EpinelPS.Database;
using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Character;

[GameRequest("/character/attractive/obtainreward")]
public class ObtainEpReward : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqObtainAttractiveReward req = await ReadData<ReqObtainAttractiveReward>();
        ResObtainAttractiveReward response = new();
        User user = GetUser();

        AttractiveLevelRewardRecord? levelUpRecord = GameData.Instance.AttractiveLevelReward.Values
            .FirstOrDefault(x => x.AttractiveLevel == req.Lv && x.NameCode == req.NameCode);
        if (levelUpRecord == null)
        {
            Logging.WriteLine($"No attractive level reward for NameCode={req.NameCode}, Lv={req.Lv}", LogType.Warning);
            await WriteDataAsync(response);
            return;
        }

        NetUserAttractiveData? bondInfo = user.BondInfo.FirstOrDefault(x => x.NameCode == req.NameCode);
        if (bondInfo == null || bondInfo.Lv < req.Lv)
        {
            Logging.WriteLine($"Attractive level not reached for NameCode={req.NameCode}, requested Lv={req.Lv}, actual Lv={bondInfo?.Lv ?? 0}", LogType.Warning);
            await WriteDataAsync(response);
            return;
        }

        if (bondInfo.ObtainedRewardLevels.Contains(levelUpRecord.Id))
        {
            await WriteDataAsync(response);
            return;
        }

        RewardRecord? reward = GameData.Instance.GetRewardTableEntry(levelUpRecord.RewardId);
        if (reward == null)
        {
            Logging.WriteLine($"Reward table entry {levelUpRecord.RewardId} not found for attractive reward {levelUpRecord.Id}", LogType.Warning);
            await WriteDataAsync(response);
            return;
        }

        bondInfo.ObtainedRewardLevels.Add(levelUpRecord.Id);
        response.Reward = RewardUtils.RegisterRewardsForUser(user, reward);
        JsonDb.Save();

        // /character/costume/get already returns every CharacterCostumeTable ID,
        // and costume is always 0 in AttractiveLevelRewardTable, so no costume
        // needs to be granted here.
        await WriteDataAsync(response);
    }
}
