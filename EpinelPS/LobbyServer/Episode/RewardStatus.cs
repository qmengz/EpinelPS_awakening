namespace EpinelPS.LobbyServer.Episode;

[GameRequest("/episode/mission/reward/status")]
public class RewardStatus : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqGetEpMissionRewardStatus req = await ReadData<ReqGetEpMissionRewardStatus>();

        await WriteDataAsync(EpisodeHelper.BuildRewardStatusResponse(GetUser(), req));
    }
}
