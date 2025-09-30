using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Soloraid;

[PacketPath("/soloraid/fastbattle")]
public class FastBattleSoloRaid : LobbyMsgHandler
{
    protected override async Task HandleAsync()
    {
        ReqFastBattleSoloRaid req = await ReadData<ReqFastBattleSoloRaid>();

        ResFastBattleSoloRaid response = new();

        // TODO

        await WriteDataAsync(response);
    }
}