using EpinelPS.Data;
using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Shop
{
    [PacketPath("/shop/productlist")]
    public class GetShopProductList : LobbyMsgHandler
    {
        protected override async Task HandleAsync()
        {
            ReqShopProductList req = await ReadData<ReqShopProductList>();
            ResShopProductList response = new();

            response.Shops.Add(ShopHelper.InitShopData(ShopCategoryType.ShopNormal));
            response.Shops.Add(ShopHelper.InitShopData(ShopCategoryType.ShopGuild));
            response.Shops.Add(ShopHelper.InitShopData(ShopCategoryType.ShopDisassemble));
            response.Shops.Add(ShopHelper.InitShopData(ShopCategoryType.ShopMaze));
            response.Shops.Add(ShopHelper.InitShopData(ShopCategoryType.ShopPvP));
            response.Shops.Add(ShopHelper.InitShopData(ShopCategoryType.ShopCooperationEvent));
            response.Shops.Add(ShopHelper.InitShopData(ShopCategoryType.ShopStoryEvent));
            response.Shops.Add(ShopHelper.InitShopData(ShopCategoryType.ShopMileage));
            response.Shops.Add(ShopHelper.InitShopData(ShopCategoryType.ShopTrade));

            await WriteDataAsync(response);
        }
    }
}
