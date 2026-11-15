namespace EpinelPS.LobbyServer.Character;

[GameRequest("/character/attractive/get")]
public class GetCharacterAttractiveList : LobbyMessage
{
    protected override async Task HandleAsync()
    {
        _ = await ReadData<ReqGetAttractiveList>();
        User user = GetUser();

        user.ResetDataIfNeeded();
        int remainingCounselCount = user.ResetableData.DailyCounselCount.GetValueOrDefault(1, 3);

        ResGetAttractiveList response = new()
        {
            CounselAvailableCount = remainingCounselCount
        };

        foreach (NetUserAttractiveData item in user.BondInfo)
        {
            response.Attractives.Add(item);
            item.CanCounselToday = remainingCounselCount > 0;
        }

        // TODO: Validate response from real server and pull info from user info
        await WriteDataAsync(response);
    }
}
