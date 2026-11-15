namespace EpinelPS.LobbyServer.Character.Counsel;

[GameRequest("/character/attractive/counsel")]
public class DoCounsel : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqCharacterCounsel req = await ReadData<ReqCharacterCounsel>();

        await WriteDataAsync(AttractiveHelper.ApplyCounsel(GetUser(), req, GameContext));
    }
}
