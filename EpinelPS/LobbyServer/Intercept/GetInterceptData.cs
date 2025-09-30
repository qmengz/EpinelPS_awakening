using EpinelPS.Database;
using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Intercept
{
    [PacketPath("/intercept/get")]
    public class GetInterceptData : LobbyMsgHandler
    {
        protected override async Task HandleAsync()
        {
            ReqGetInterceptData req = await ReadData<ReqGetInterceptData>();

            ResGetInterceptData response = new()
            {
                NormalInterceptGroup = 1,
                SpecialInterceptId = GetCurrentSpecialInterceptId(), // TODO switch this out each reset
                TicketCount = User.ResetableData.InterceptionTickets,
                MaxTicketCount = JsonDb.Instance.MaxInterceptionCount
            };

            await WriteDataAsync(response);
        }
        
        private static int GetCurrentSpecialInterceptId()
        {
            // TODO: Implement logic to get the current special intercept ID
            // For now, return a fixed value or a value based on the current date
            DateTime today = DateTime.Now;
            int number = (today.Day % 5 == 0) ? 5 : today.Day % 5;
            Logging.WriteLine($"Special Intercept ID for today ({today:d}): {number}", LogType.Debug);
            return number;
        }
    }
}
