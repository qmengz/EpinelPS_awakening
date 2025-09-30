using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Soloraid;

[PacketPath("/soloraid/practice/close")]
public class CloseSoloRaidPractice : LobbyMsgHandler
{
    protected override async Task HandleAsync()
    {
        ReqCloseSoloRaidPractice req = await ReadData<ReqCloseSoloRaidPractice>();

        ResCloseSoloRaidPractice response = new();

        // TODO

        await WriteDataAsync(response);
    }
}