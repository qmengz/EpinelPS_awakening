using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Soloraid;

[PacketPath("/soloraid/practice/getlevel")]
public class GetLevelPracticeSoloRaid : LobbyMsgHandler
{
    protected override async Task HandleAsync()
    {
        ReqGetLevelPracticeSoloRaid req = await ReadData<ReqGetLevelPracticeSoloRaid>();

        ResGetLevelPracticeSoloRaid response = new()
        {
            Raid = new NetPracticeSoloRaid
            {
                Level = req.RaidLevel,
                Hp = 1000000,
                Damage = 0,
            },
            JoinData = new NetSoloRaidJoinData
            {
                CsnList = { 206228857, 314243734, 55434962, 993944986, 1984601008 },
            },
            PeriodResult = SoloRaidPeriodResult.Success,
        };

        // TODO

        await WriteDataAsync(response);
    }
}