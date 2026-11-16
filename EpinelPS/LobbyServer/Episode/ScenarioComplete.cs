namespace EpinelPS.LobbyServer.Episode;

[GameRequest("/episode/mission/scenario/complete")]
public class ScenarioComplete : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqSetEpMissionScenarioComplete req = await ReadData<ReqSetEpMissionScenarioComplete>();

        await WriteDataAsync(EpisodeHelper.SetScenarioComplete(GetUser(), req));
    }
}
