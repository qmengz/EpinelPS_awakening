using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Soloraid;

[PacketPath("/soloraid/practice/setdamage")]
public class SetSoloRaidPracticeDamage : LobbyMsgHandler
{
    protected override async Task HandleAsync()
    {
        ReqSetSoloRaidPracticeDamage req = await ReadData<ReqSetSoloRaidPracticeDamage>();

        ResSetSoloRaidPracticeDamage response = new();

        // TODO

        await WriteDataAsync(response);
    }
}