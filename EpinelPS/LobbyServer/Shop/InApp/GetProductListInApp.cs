using EpinelPS.Data;
using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Shop.InApp
{
    [PacketPath("/inappshop/getdata")]
    public class GetProductList : LobbyMsgHandler
    {
        protected override async Task HandleAsync()
        {
            ReqGetInAppShopData x = await ReadData<ReqGetInAppShopData>();

            ResGetInAppShopData response = new();
            var now = DateTime.UtcNow;
            response.InAppShopDataList.AddRange(
                GameData.Instance.InAppShopManagerTable.Values
                    .Where(x => x.StartDate < now && x.EndDate > now)
                    .Select(x => new NetInAppShopData
                    {
                        Id = x.Id,
                        StartDate = x.StartDate.Ticks,
                        EndDate = x.EndDate.Ticks,
                    })
            );
            // response.InAppShopDataList.Add(new NetInAppShopData() { Id = 10001, StartDate = DateTime.UtcNow.Ticks, EndDate = DateTime.UtcNow.AddDays(2).Ticks });

            await WriteDataAsync(response);
        }
    }
}
