namespace EpinelPS.LobbyServer.Character.Counsel;

[GameRequest("/character/counsel/progress/reward")]
public class ProgressReward : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqObtainCounselProgressReward req = await ReadData<ReqObtainCounselProgressReward>();

        await WriteDataAsync(AttractiveHelper.ObtainProgressReward(GetUser(), req));
    }
}
