namespace EpinelPS.LobbyServer.Character.Counsel;

[GameRequest("/character/attractive/check")]
public class CheckCharacterCounsel : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqCounseledBefore req = await ReadData<ReqCounseledBefore>();

        await WriteDataAsync(AttractiveHelper.BuildCounseledBeforeResponse(GetUser(), req));
    }
}
