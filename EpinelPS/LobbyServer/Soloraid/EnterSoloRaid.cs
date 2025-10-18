using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Soloraid;

[PacketPath("/soloraid/enter")]
public class EnterSoloRaid : LobbyMsgHandler
{
    protected override async Task HandleAsync()
    {
        ReqEnterSoloRaid req = await ReadData<ReqEnterSoloRaid>();

        ResEnterSoloRaid response = new()
        {
            PeriodResult = SoloRaidPeriodResult.Success,
        };

        await WriteDataAsync(response);
    }
}