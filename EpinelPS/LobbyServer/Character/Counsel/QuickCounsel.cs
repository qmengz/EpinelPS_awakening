namespace EpinelPS.LobbyServer.Character.Counsel;

[GameRequest("/character/counsel/quick")]
public class QuickCounsel : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqCharacterQuickCounsel req = await ReadData<ReqCharacterQuickCounsel>();

        await WriteDataAsync(AttractiveHelper.ApplyQuickCounsel(GetUser(), req, GameContext));
    }
}
