namespace EpinelPS.LobbyServer.Episode;

[GameRequest("/episode/mission/scenario/get")]
public class ScenarioGet : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqListEpMissionCompletedScenario req = await ReadData<ReqListEpMissionCompletedScenario>();

        await WriteDataAsync(EpisodeHelper.BuildCompletedScenarioResponse(GetUser(), req));
    }
}
