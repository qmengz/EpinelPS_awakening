using EpinelPS.Data;
using EpinelPS.Database;
using EpinelPS.Utils;
using log4net;
using Newtonsoft.Json;

namespace EpinelPS.LobbyServer.Inventory
{
    [PacketPath("/inventory/useselectbox")]
    public class UseSelectBox : LobbyMsgHandler
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(UseSelectBox));
        
        protected override async Task HandleAsync()
        {
            // { "isn": "92999919299991", "select": [ { "id": 1, "count": 5 }, { "id": 2, "count": 5 } ] }
            ReqUseSelectBox req = await ReadData<ReqUseSelectBox>();
            User user = GetUser();

            ResUseSelectBox response = new();

            // calculate total item count
            int itemCount = req.Select.Sum(x => x.Count);
            if (itemCount <= 0) throw new Exception("no items selected");

            // find box in inventory
            ItemData box = user.Items.Where(x => x.Isn == req.Isn).FirstOrDefault() ?? throw new InvalidDataException("cannot find box with isn " + req.Isn);
            if (itemCount > box.Count) throw new Exception("count mismatch");



            // find matching consumable entry
            ItemConsumeRecord? itemConsume = GameData.Instance.ConsumableItems.Values.Where(x => x.Id == box.ItemType).FirstOrDefault() ?? throw new Exception($"cannot find any consumable item entries with ID {box.ItemType}");
            log.Debug($"UseSelectBox {box.ItemType}: Found item consume - {JsonConvert.SerializeObject(itemConsume)}");

            NetRewardData reward = new()
            {
                PassPoint = new() { }
            };

            // find matching select options entry
            if (GameData.Instance.ItemSelectOptionTable.TryGetValue(itemConsume.UseId, out ItemSelectOptionRecord? selectOptions))
            {
                foreach (var selected in req.Select)
                {
                    // find selected option
                    SelectOptionData option = selectOptions.SelectOption[selected.Id] ?? throw new Exception($"invalid selection ID {selected.Id}");
                    if (option.SelectType == RewardType.Currency)
                    {
                        // Add currency reward
                        RewardUtils.AddSingleCurrencyObject(user, ref reward, (CurrencyType)option.SelectId, option.SelectValue * selected.Count); // value is in cents
                    }
                    else
                    {
                        // Add item reward
                        RewardUtils.AddSingleObject(user, ref reward, option.SelectId, option.SelectType, option.SelectValue * selected.Count);
                    }

                }
            }
            JsonDb.Save();
            // update client side box count
            box.Count -= itemCount;
            if (box.Count == 0) user.Items.Remove(box);
            reward.UserItems.Add(NetUtils.UserItemDataToNet(box));
            response.Reward = reward;
            await WriteDataAsync(response);
        }

    }
}