namespace EpinelPS.LobbyServer.Episode;

[GameRequest("/episode/mission/reward")]
public class ObtainReward : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqObtainEpMissionReward req = await ReadData<ReqObtainEpMissionReward>();

        await WriteDataAsync(EpisodeHelper.ObtainReward(GetUser(), req));
    }
}
