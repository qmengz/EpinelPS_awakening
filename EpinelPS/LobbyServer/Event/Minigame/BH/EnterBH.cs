using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Event.Minigame.BH
{
    [PacketPath("/event/minigame/bh/enter")]
    public class EnterBH : LobbyMsgHandler
    {
        protected override async Task HandleAsync()
        {
            // bHId, playCharacterId
            ReqEnterMiniGameBH req = await ReadData<ReqEnterMiniGameBH>();
            User user = GetUser();

            ResEnterMiniGameBH response = new();

            await WriteDataAsync(response);
        }
    }
}