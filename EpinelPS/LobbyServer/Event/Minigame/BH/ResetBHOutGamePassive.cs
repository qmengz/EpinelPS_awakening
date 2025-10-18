using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Event.Minigame.BH
{
    [PacketPath("/event/minigame/bh/outgamepassive/reset")]
    public class ResetBHOutGamePassive : LobbyMsgHandler
    {
        protected override async Task HandleAsync()
        {
            // bHId 
            ReqResetMiniGameBHOutGamePassive req = await ReadData<ReqResetMiniGameBHOutGamePassive>();
            User user = GetUser();

            ResResetMiniGameBHOutGamePassive response = new();
            //   updatedGold

            await WriteDataAsync(response);
        }
    }
}