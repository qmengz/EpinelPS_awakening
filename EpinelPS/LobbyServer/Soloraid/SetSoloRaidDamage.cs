using EpinelPS.Data;
using EpinelPS.Database;
using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Soloraid;

[PacketPath("/soloraid/setdamage")]
public class SetSoloRaidDamage : LobbyMsgHandler
{
    protected override async Task HandleAsync()
    {
        ReqSetSoloRaidDamage req = await ReadData<ReqSetSoloRaidDamage>();
        User user = GetUser();

        ResSetSoloRaidDamage response = new();

        var userSoloRaidInfo = user.SoloRaidData.Values.FirstOrDefault(s => s.RaidLevel == req.RaidLevel);
        if (userSoloRaidInfo != null)
        {

            userSoloRaidInfo.TotalDamage += req.Damage;
            userSoloRaidInfo.RaidJoinCount++;
            userSoloRaidInfo.SoloRaidDamageList.Add(req);

            if (userSoloRaidInfo.TotalDamage >= userSoloRaidInfo.Hp)
            {
                userSoloRaidInfo.TotalDamage = userSoloRaidInfo.Hp;
                userSoloRaidInfo.RaidOpenCount = 1; // Reset open count on kill
                userSoloRaidInfo.Status = SoloRaidStatus.Kill;
                SoloRaidPresetRecord? presetData = GameData.Instance.SoloRaidPresetTable.Values.FirstOrDefault(r =>
                    r.Wave == req.AntiCheatBattleData.WaveId && r.WaveOrder == req.RaidLevel);
                if (presetData != null)
                {
                    response.Reward = RewardUtils.RegisterRewardsForUser(user, presetData.RewardId);
                    response.FirstClearReward = RewardUtils.RegisterRewardsForUser(user, presetData.FirstClearRewardId);
                }
            }
            else
            {
                userSoloRaidInfo.Status = SoloRaidStatus.Alive;
            }

            response.Info = new NetNormalSoloRaid
            {
                Level = req.RaidLevel,
                Hp = userSoloRaidInfo.Hp - userSoloRaidInfo.TotalDamage,
            };

            response.RaidJoinCount = userSoloRaidInfo.RaidJoinCount;
            response.Status = userSoloRaidInfo.Status;
            response.PeriodResult = SoloRaidPeriodResult.Success;
            response.JoinData = new NetSoloRaidJoinData
            {
                CsnList = { req.AntiCheatBattleData.Characters.Select(c => c.Csn) },
            };
        }

        JsonDb.Save();
        await WriteDataAsync(response);
    }
}