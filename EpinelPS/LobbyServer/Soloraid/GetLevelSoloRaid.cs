using EpinelPS.Data;
using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Soloraid;

[PacketPath("/soloraid/getlevel")]
public class GetLevelSoloRaid : LobbyMsgHandler
{
    protected override async Task HandleAsync()
    {
        ReqGetLevelSoloRaid req = await ReadData<ReqGetLevelSoloRaid>();

        long levelHp = 0;
        User user = GetUser();
        int SoloRaidManagerTid = GameData.Instance.SoloRaidManagerTable.Keys.Max();
        var userSoloRaidInfo = user.SoloRaidData.Values.FirstOrDefault(s => s.RaidLevel == req.RaidLevel && s.RaidId == SoloRaidManagerTid);

        if (userSoloRaidInfo != null)
        {
            levelHp = userSoloRaidInfo.Hp - userSoloRaidInfo.TotalDamage;
        }

        ResGetLevelSoloRaid response = new()
        {
            Raid = new NetNormalSoloRaid
            {
                Level = req.RaidLevel,
                Hp = levelHp,
            },
            PeriodResult = SoloRaidPeriodResult.Success,
            JoinData = new NetSoloRaidJoinData
            {
                CsnList = { }
            }

        };

        // TODO

        await WriteDataAsync(response);
    }
}