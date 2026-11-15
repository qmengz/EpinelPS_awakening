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

        // CanCounselToday is maintained in the save data: reset to true daily and
        // cleared when that character is advised. Do not overwrite it here with
        // the global allowance, or the client's per-character daily limit breaks.
        foreach (NetUserAttractiveData item in user.BondInfo)
        {
            response.Attractives.Add(item);
        }

        // TODO: Validate response from real server and pull info from user info
        await WriteDataAsync(response);
    }
}
