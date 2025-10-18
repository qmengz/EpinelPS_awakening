using EpinelPS.Data;
using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Soloraid;

[PacketPath("/soloraid/get")]
public class GetSoloRaidInfo : LobbyMsgHandler
{
    protected override async Task HandleAsync()
    {
        ReqGetSoloRaidInfo req = await ReadData<ReqGetSoloRaidInfo>();
        User user = GetUser();

        ResGetSoloRaidInfo response = new()
        {
            Info = new NetUserSoloRaidInfo(),
            PeriodResult = SoloRaidPeriodResult.Success
        };
        int LastClearLevel = 0;
        int SoloRaidManagerTid = GameData.Instance.SoloRaidManagerTable.Keys.Max();
        NetUserSoloRaidInfo info = new();

        foreach (var raid in user.SoloRaidData.Values.Where(r => r.RaidId == SoloRaidManagerTid).OrderBy(r => r.RaidLevel))
        {
            if (raid.RaidLevel > LastClearLevel && raid.Status == SoloRaidStatus.Kill)
            {
                LastClearLevel = raid.RaidLevel;
            }
            else if (raid.RaidLevel == LastClearLevel && raid.Status != SoloRaidStatus.Kill)
            {
                LastClearLevel = raid.RaidLevel;
                info.LastOpenRaid = new NetSoloRaid
                {
                    Level = raid.RaidLevel,
                    Type = raid.Type,
                };
            }
            else if (raid.RaidLevel == LastClearLevel && raid.Status == SoloRaidStatus.Kill)
            {
                LastClearLevel = raid.RaidLevel + 1;
            }
            else if (raid.RaidLevel == 8)
            {
                LastClearLevel = 8;
                break;
            }
        }

        info.SoloRaidManagerTid = SoloRaidManagerTid;
        info.LastClearLevel = LastClearLevel;

        info.Period = new NetSoloRaidPeriodData
        {
            VisibleDate = DateTime.Now.AddDays(-10).Ticks,
            StartDate = DateTime.Now.AddDays(-5).Ticks,
            EndDate = DateTime.Now.AddDays(5).Ticks,
            DisableDate = DateTime.Now.AddDays(10).Ticks,
            SettleDate = DateTime.Now.AddDays(15).Ticks,
        };

        response.Info = info;

        await WriteDataAsync(response);
    }
}