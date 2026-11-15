namespace EpinelPS.LobbyServer.Character.Counsel;

[GameRequest("/character/counsel/answer/list")]
public class ListAnswered : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        ReqListAnsweredAttractiveCounsel req = await ReadData<ReqListAnsweredAttractiveCounsel>();

        await WriteDataAsync(AttractiveHelper.ListAnswered(GetUser(), req));
    }
}
