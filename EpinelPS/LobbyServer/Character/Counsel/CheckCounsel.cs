namespace EpinelPS.LobbyServer.Character.Counsel;

[GameRequest("/character/counsel/check")]
public class CheckCounsel : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqCounseledBefore req = await ReadData<ReqCounseledBefore>();

        await WriteDataAsync(AttractiveHelper.BuildCounseledBeforeResponse(GetUser(), req));
    }
}
