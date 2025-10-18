using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Soloraid;

[PacketPath("/soloraid/getranking")]
public class GetSoloRaidRanking : LobbyMsgHandler
{
    protected override async Task HandleAsync()
    {
        ReqGetSoloRaidRanking req = await ReadData<ReqGetSoloRaidRanking>();

        ResGetSoloRaidRanking response = new();

        // TODO

        await WriteDataAsync(response);
    }
}