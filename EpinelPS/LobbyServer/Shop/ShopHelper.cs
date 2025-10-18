using EpinelPS.Data;
using EpinelPS.Database;
using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Shop
{
    public static class ShopHelper
    {
        public static void BuyShopProduct(User? user, ref ResShopBuyProduct response, int productTid, int quantity)
        {
            // Validate user and request data
            if (user == null || quantity <= 0 || productTid <= 0)
            {
                response.Result = ShopBuyProductResult.Expired;
                return;
            }

            NetShopBuyProductData productData = new();

            if (!GameData.Instance.ContentsShopProductTable.TryGetValue(productTid, out var tableProduct))
            {
                return;
            }

            if (tableProduct.PriceType == PriceType.Currency)
            {
                if (!UpdateCurrency(user, tableProduct.PriceId, tableProduct.PriceValue, quantity, ref response))
                {
                    return; // Exit if currency update failed
                }
            }
            else if (tableProduct.PriceType == PriceType.Item)
            {
                if (!UpdateItem(user, tableProduct.PriceId, tableProduct.PriceValue, quantity, ref response))
                {
                    return; // Exit if item update failed
                }
            }
            else
            {
                Logging.WriteLine($"Unsupported PriceType: {tableProduct.PriceType}");
            }

            if (tableProduct.GoodsType == RewardType.Item)
            {
                var userItem = user.Items.FirstOrDefault(i => i.ItemType == tableProduct.GoodsId);
                var itemEquip = GameData.Instance.ItemEquipTable.GetValueOrDefault(tableProduct.GoodsId);
                if (userItem != null)
                {

                    if (itemEquip != null)
                    {
                        // the item is not stackable, we need to create new entries for each quantity
                        for (int i = 0; i < quantity; i++)
                        {
                            ItemData newItem = new()
                            {
                                ItemType = tableProduct.GoodsId,
                                Count = 1,
                                Csn = 0,
                                Level = 0,
                                Exp = 0,
                                Position = 0,
                                Corp = 0,
                                Isn = user.GenerateUniqueItemId()
                            };
                            user.Items.Add(newItem);
                        }
                    }

                    userItem.Count += tableProduct.GoodsValue * quantity;
                    productData.Item.Add(new NetItemData
                    {
                        Tid = userItem.ItemType,
                        Count = quantity,
                        Isn = userItem.Isn
                    });
                }
                else
                {
                    ItemData itemData = new()
                    {
                        ItemType = tableProduct.GoodsId,
                        Count = tableProduct.GoodsValue * quantity,
                        Csn = 0,
                        Level = 0,
                        Exp = 0,
                        Position = 0,
                        Corp = 0,
                        Isn = user.GenerateUniqueItemId()
                    };
                    user.Items.Add(itemData);
                    productData.Item.Add(new NetItemData
                    {
                        Tid = itemData.ItemType,
                        Count = quantity,
                        Isn = itemData.Isn
                    });
                }


                productData.BuyCount = quantity;
            }
            else
            {
                Logging.WriteLine($"Unsupported GoodsType: {tableProduct.GoodsType}");
            }

            // Update product data
            response.Product = productData;
            // Save changes to the database
            JsonDb.Save();
        }

        /// <summary>
        /// Initialize Shop Data
        /// </summary>
        /// <param name="category">Shop Category Type</param>
        /// <returns>Shop Data</returns>
        public static NetShopProductData InitShopData(ShopCategoryType category)
        {
            NetShopProductData shop = new()
            {
                ShopCategory = (int)category,
                NextRenewAt = DateTime.Now.AddDays(1).Ticks,
            };

            if (category == ShopCategoryType.ShopNormal)
            {
                shop.FreeRenewCount = 1;
            }

            try
            {
                ContentsShopRecord? maxContentsShop = GameData.Instance.ContentsShopTable.Values
                    .Where(cs => cs.ShopCategory == category).MaxBy(cs => cs.Id);
                if (maxContentsShop != null)
                {
                    shop.ShopTid = maxContentsShop.Id;
                    GameData.Instance.ContentsShopProductTable.Values
                        .Where(csp => csp.BundleId == maxContentsShop.BundleId).ToList().ForEach(csp =>
                        {
                            shop.List.Add(new NetShopProductInfoData
                            {
                                Order = csp.ProductOrder,
                                ProductId = csp.Id,
                                BuyLimitCount = csp.BuyLimitCount,
                                // Discount = csp.DiscountProbId,
                            });
                        });
                }

                return shop;
            }
            catch (Exception ex)
            {
                Logging.WriteLine($"Error in InitShopData: {ex}");
                return shop;
            }
        }

        public static bool UpdateCurrency(User user, int priceId, int priceValue, int quantity, ref ResShopBuyProduct response)
        {
            long totalPrice = priceValue * quantity;
            if (!user.Currency.TryGetValue((CurrencyType)priceId, out var currentAmount))
            {
                Logging.WriteLine($"Insufficient funds: Have {currentAmount}, need {totalPrice}");
                return false;
            }

            if (currentAmount < totalPrice)
            {
                Logging.WriteLine($"Insufficient funds: Have {currentAmount}, need {totalPrice}");
                return false;
            }
            CurrencyType currencyType = (CurrencyType)priceId; // Assuming PriceId maps directly to CurrencyType
            long newAmount = currentAmount - totalPrice; // calculate new amount
            user.Currency[currencyType] = newAmount; // update user currency
            response.Result = ShopBuyProductResult.Success; // set success result
            response.Currencies.Add(new NetUserCurrencyData // Update response currency
            {
                Type = (int)currencyType,
                Value = newAmount
            });
            return true;
        }

        public static bool UpdateItem(User user, int priceId, int priceValue, int quantity, ref ResShopBuyProduct response)
        {
            var item = user.Items.FirstOrDefault(i => i.ItemType == priceId);
            if (item == null || item.Count < quantity)
            {
                Logging.WriteLine($"Insufficient item funds: Have {item?.Count ?? 0}, need {priceValue * quantity}");
                return false; // Not enough items
            }
            else
            {
                item.Count -= priceValue * quantity; // Deduct the item cost
                if (item.Count <= 0)
                {
                    user.Items.Remove(item); // Remove item if count is zero or less
                }
                response.Result = ShopBuyProductResult.Success; // set success result
                                                                // Update response items
                response.Item = new()
                {
                    Tid = item.ItemType,
                    Count = item.Count,
                    Isn = item.Isn
                };
            }
            return true;
        }

        public static int GetItemPos(ItemSubType subType)
        {
            return subType switch
            {
                ItemSubType.Module_A => 0,
                ItemSubType.Module_B => 1,
                ItemSubType.Module_C => 2,
                ItemSubType.Module_D => 3,
                _ => 0,
            };
        }
    }
}