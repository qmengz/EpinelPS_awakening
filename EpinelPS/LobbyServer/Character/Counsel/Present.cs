namespace EpinelPS.LobbyServer.Character.Counsel;

[GameRequest("/character/attractive/present")]
public class Present : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqCharacterPresent req = await ReadData<ReqCharacterPresent>();

        await WriteDataAsync(AttractiveHelper.ApplyPresent(GetUser(), req, GameContext));
    }
}
