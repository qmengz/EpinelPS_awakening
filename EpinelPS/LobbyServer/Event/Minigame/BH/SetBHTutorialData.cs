using EpinelPS.Database;
using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Event.Minigame.BH
{
    [PacketPath("/event/minigame/bh/tutorial/set/data")]
    public class SetBHTutorialData : LobbyMsgHandler
    {
        protected override async Task HandleAsync()
        {
            // bHId, tutorialId
            ReqSetMiniGameBHTutorialData req = await ReadData<ReqSetMiniGameBHTutorialData>();
            User user = GetUser();

            int key = int.Parse($"{req.BHId}{req.TutorialId}");
            if (user.MiniGameBHTutorialInfo.TryGetValue(key, out MiniGameBHTutorialData? data))
            {
                data.TutorialId = req.TutorialId;
                data.UpdatedAt = DateTimeOffset.UtcNow.Ticks;
            }
            else
            {
                user.MiniGameBHTutorialInfo.Add(key, new MiniGameBHTutorialData
                {
                    BHId = req.BHId,
                    TutorialId = req.TutorialId,
                    UpdatedAt = DateTimeOffset.UtcNow.Ticks
                });
            }
            JsonDb.Save();
            ResSetMiniGameBHTutorialData response = new();

            await WriteDataAsync(response);
        }
    }
}