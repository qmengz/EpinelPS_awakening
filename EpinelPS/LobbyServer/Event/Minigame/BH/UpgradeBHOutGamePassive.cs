using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Event.Minigame.BH
{
    [PacketPath("/event/minigame/bh/outgamepassive/upgrade")]
    public class UpgradeBHOutGamePassive : LobbyMsgHandler
    {
        protected override async Task HandleAsync()
        {
            // bHId outGamePassiveId
            ReqUpgradeMiniGameBHOutGamePassive req = await ReadData<ReqUpgradeMiniGameBHOutGamePassive>();
            User user = GetUser();

            ResUpgradeMiniGameBHOutGamePassive response = new();
            //   updatedGold

            await WriteDataAsync(response);
        }
    }
}