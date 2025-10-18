using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Event.Minigame.BH
{
    [PacketPath("/event/minigame/bh/collection/set/confirmed")]
    public class SetBHCollectionConfirmed : LobbyMsgHandler
    {
        protected override async Task HandleAsync()
        {
            // bHId, confirmedCollectionIdList
            ReqSetMiniGameBHCollectionConfirmed req = await ReadData<ReqSetMiniGameBHCollectionConfirmed>();
            User user = GetUser();

            ResSetMiniGameBHCollectionConfirmed response = new();

            await WriteDataAsync(response);
        }
    }
}