using EpinelPS.Utils;
using EpinelPS.Data;
using EpinelPS.Database;
using Newtonsoft.Json;
using log4net;

namespace EpinelPS.LobbyServer.Profile
{
    [PacketPath("/ProfileCard/ProfileRandomBox/Open")]
    public class OpenProfileRandomBox : LobbyMsgHandler
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(OpenProfileRandomBox));

        protected override async Task HandleAsync()
        {
            // { "isn": "3120203", "numOpens": 1 }
            ReqOpenProfileRandomBox req = await ReadData<ReqOpenProfileRandomBox>();
            User user = GetUser();

            ResOpenProfileRandomBox response = new();

            // find box in inventory
            ItemData box = user.Items.Where(x => x.Isn == req.Isn).FirstOrDefault() ?? throw new InvalidDataException("cannot find box with isn " + req.Isn);
            if (req.NumOpens > box.Count) throw new Exception("count mismatch");

            box.Count -= req.NumOpens;
            if (box.Count == 0) user.Items.Remove(box);

            // update client side box count
            response.ProfileCardTicketMaterialSync.Add(NetUtils.UserItemDataToNet(box));

            // find matching probability entries
            ItemRandomRecord[] entries = [.. GameData.Instance.RandomItem.Values.Where(x => x.GroupId == box.ItemType)];
            if (entries.Length == 0) throw new Exception($"cannot find any probability entries with ID {box.ItemType}");

            for (int i = 0; i < req.NumOpens; i++)
            {
                ItemRandomRecord winningRecord = Rng.PickWeightedItem([.. entries]);
                log.Debug($"LootBox {box.ItemType}: Won item - {JsonConvert.SerializeObject(winningRecord)}");

                if (winningRecord.RewardValueMin != winningRecord.RewardValueMax)
                {
                    Logging.WriteLine("TODO: reward_value_max", LogType.Warning);
                }

                if (winningRecord.RewardType == RewardType.ProfileCardObject)
                {

                    ItemData? existingItem = user.Items.Where(x => x.ItemType == winningRecord.RewardId).FirstOrDefault();

                    // if user already has the item, convert to ticket material instead
                    if (existingItem != null)
                    {
                        // find ticket material ID from ProfileCardObjectTable
                        if (GameData.Instance.ProfileCardObjectTable.TryGetValue(winningRecord.RewardId, out ProfileCardObjectRecord? ProfileCardObjectData))
                        {
                            int ticketMaterialTid = ProfileCardObjectData.ExchangeItemId;
                            ItemData? existingTicketMaterial = user.Items.Where(x => x.ItemType == ticketMaterialTid).FirstOrDefault();
                            if (existingTicketMaterial != null)
                            {
                                // add to existing item
                                existingTicketMaterial.Count += ProfileCardObjectData.ExchangeItemValue;
                            }
                            else
                            {
                                // create new item
                                ItemData newTicketMaterial = new()
                                {
                                    Isn = user.GenerateUniqueItemId(),
                                    ItemType = ticketMaterialTid,
                                    Count = ProfileCardObjectData.ExchangeItemValue
                                };
                                user.Items.Add(newTicketMaterial);
                                existingTicketMaterial = newTicketMaterial;
                            }
                            response.ProfileCardTicketMaterialSync.Add(NetUtils.UserItemDataToNet(existingTicketMaterial));
                        }
                        else
                        {
                            throw new Exception("cannot find ProfileCardObjectTable entry for item " + winningRecord.RewardId);
                        }
                        response.ProfileCardTicketMaterialSync.Add(NetUtils.UserItemDataToNet(existingItem));
                        response.OpeningResult.Add(new ProfileRandomBoxSingleOpeningResult()
                        {
                            ObjectTid = existingItem.ItemType,
                            ExchangedForTicketMaterial = true
                        });
                        continue;
                    }

                    // otherwise give the item
                    ItemData newItem = new()
                    {
                        Isn = user.GenerateUniqueItemId(),
                        ItemType = winningRecord.RewardId
                    };
                    user.Items.Add(newItem);
                    response.ProfileCardTicketMaterialSync.Add(NetUtils.UserItemDataToNet(newItem));
                    response.OpeningResult.Add(new ProfileRandomBoxSingleOpeningResult()
                    {
                        ObjectTid = newItem.ItemType
                    });
                }
                else
                {
                    Logging.WriteLine("TODO: handle reward type " + winningRecord.RewardType, LogType.Warning);
                }
            }

            JsonDb.Save();
            await WriteDataAsync(response);
        }
    }
}