namespace EpinelPS.LobbyServer.Episode;

[GameRequest("/episode/mission/enter")]
public class ListMission : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        _ = await ReadData<ReqListValidEpMission>();

        await WriteDataAsync(EpisodeHelper.BuildValidMissionResponse());
    }
}
