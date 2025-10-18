using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Event.Minigame.BH
{
    [PacketPath("/event/minigame/bh/tutorial/get/data")]
    public class GetBHTutorial : LobbyMsgHandler
    {
        protected override async Task HandleAsync()
        {
            // bHId
            ReqGetMiniGameBHTutorialData req = await ReadData<ReqGetMiniGameBHTutorialData>();
            User user = GetUser();

            ResGetMiniGameBHTutorialData response = new()
            {
                // tutorialIdList
                TutorialIdList = { }
            };
            user.MiniGameBHTutorialInfo.Values.ToList().ForEach(tutorial =>
            {
                if (tutorial.BHId == req.BHId)
                    response.TutorialIdList.Add(tutorial.TutorialId);
            });

            await WriteDataAsync(response);
        }
    }
}