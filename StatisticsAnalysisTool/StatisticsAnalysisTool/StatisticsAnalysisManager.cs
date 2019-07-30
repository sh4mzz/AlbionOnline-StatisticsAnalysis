using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using StatisticsAnalysisTool.Models;
using StatisticsAnalysisTool.Properties;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StatisticsAnalysisTool.Utilities;

namespace StatisticsAnalysisTool
{
    public class StatisticsAnalysisManager
    {
        // Info Link -> https://github.com/broderickhyman/ao-bin-dumps
        // Models: https://github.com/broderickhyman/albiondata-models-dotNet

        private const string ItemsJsonUrl =
            "https://raw.githubusercontent.com/broderickhyman/ao-bin-dumps/master/formatted/items.json";
        private enum GameLanguage { UnitedStates, Germany, Russia, Poland, Brazil, France, Spain }
        public enum ItemTier { T1 = 0, T2 = 1, T3 = 2, T4 = 3, T5 = 4, T6 = 5, T7 = 6, T8 = 7 }
        public enum ItemLevel { Level0 = 0, Level1 = 1, Level2 = 2, Level3 = 3 }
        public enum ItemQuality { Normal = 0, Good = 1, Outstanding = 2, Excellent = 3, Masterpiece = 4 }

        private static readonly Dictionary<ItemTier, string> ItemTiers = new Dictionary<ItemTier, string>
        {
            {ItemTier.T1, "T1" },
            {ItemTier.T2, "T2" },
            {ItemTier.T3, "T3" },
            {ItemTier.T4, "T4" },
            {ItemTier.T5, "T5" },
            {ItemTier.T6, "T6" },
            {ItemTier.T7, "T7" },
            {ItemTier.T8, "T8" }
        };
        private static readonly Dictionary<ItemLevel, int> ItemLevels = new Dictionary<ItemLevel, int>
        {
            {ItemLevel.Level0, 0 },
            {ItemLevel.Level1, 1 },
            {ItemLevel.Level2, 2 },
            {ItemLevel.Level3, 3 }
        };
        private static readonly Dictionary<ItemQuality, int> ItemQualities = new Dictionary<ItemQuality, int>
        {
            {ItemQuality.Normal, 1 },
            {ItemQuality.Good, 2 },
            {ItemQuality.Outstanding, 3 },
            {ItemQuality.Excellent, 4 },
            {ItemQuality.Masterpiece, 5 }
        };
        private static readonly Dictionary<GameLanguage, string> GameLanguages = new Dictionary<GameLanguage, string>()
        {
            {GameLanguage.UnitedStates, "EN-US" },
            {GameLanguage.Germany, "DE-DE" },
            {GameLanguage.Russia, "RU-RU" },
            {GameLanguage.Poland, "PL-PL" },
            {GameLanguage.Brazil, "PT-BR" },
            {GameLanguage.France, "FR-FR" },
            {GameLanguage.Spain, "ES-ES" }
        };
        public static ObservableCollection<Item> Items;
        public static int RefreshRate = Settings.Default.RefreshRate;
        public static int UpdateItemListByDays = Settings.Default.UpdateItemListByDays;
        
        public static async Task<bool> GetItemsFromJsonAsync()
        {
            if (File.Exists($"{AppDomain.CurrentDomain.BaseDirectory}{Settings.Default.ItemListFileName}"))
            {
                var fileDateTime = File.GetLastWriteTime($"{AppDomain.CurrentDomain.BaseDirectory}{Settings.Default.ItemListFileName}");

                if (fileDateTime.AddDays(7) < DateTime.Now)
                {
                    using (var wc = new WebClient())
                    {
                        var itemString = await wc.DownloadStringTaskAsync(ItemsJsonUrl);
                        File.WriteAllText($"{AppDomain.CurrentDomain.BaseDirectory}{Settings.Default.ItemListFileName}", itemString, Encoding.UTF8);
                        
                        try
                        {
                            Items = JsonConvert.DeserializeObject<ObservableCollection<Item>>(itemString);
                        }
                        catch (Exception ex)
                        {
                            Debug.Print(ex.Message);
                            Items = null;
                            return false;
                        }
                        return true;
                    }
                }

                var localItemString = File.ReadAllText($"{AppDomain.CurrentDomain.BaseDirectory}{Settings.Default.ItemListFileName}");

                try
                {
                    Items = JsonConvert.DeserializeObject<ObservableCollection<Item>>(localItemString);
                }
                catch (Exception ex)
                {
                    Debug.Print(ex.Message);
                    Items = null;
                    return false;
                }

                return true;
            }

            using (var wc = new WebClient())
            {
                var itemsString = await wc.DownloadStringTaskAsync(ItemsJsonUrl);
                File.WriteAllText($"{AppDomain.CurrentDomain.BaseDirectory}{Settings.Default.ItemListFileName}", itemsString, Encoding.UTF8);
                Items = JsonConvert.DeserializeObject<ObservableCollection<Item>>(itemsString);
                return true;
            }
        }

        public static async Task<ItemData> GetItemDataFromJsonAsync(Item item)
        {
            try
            {
                using (var wc = new WebClient())
                {
                    var itemDataJsonUrl =
                        $"https://gameinfo.albiononline.com/api/gameinfo/items/{item.UniqueName}/data";
                    var itemString = await wc.DownloadStringTaskAsync(itemDataJsonUrl);
                    var parsedObject = JObject.Parse(itemString);

                    var itemData = new ItemData
                    {
                        ItemType = (string) parsedObject["itemType"],
                        UniqueName = (string) parsedObject["uniqueName"],
                        //UiSprite = (string)parsedObject["uiSprite"],
                        Showinmarketplace = (bool) parsedObject["showinmarketplace"],
                        Level = (int) parsedObject["level"],
                        Tier = (int) parsedObject["tier"],
                        LocalizedNames = new List<ItemData.KeyValueStruct>(),
                        //CategoryId = (string)parsedObject["categoryId"],
                        //CategoryName = (string)parsedObject["categoryName"],
                        //LocalizedDescriptions = (string)parsedObject["localizedDescriptions"]["DE-DE"],
                        //SlotType = (string)parsedObject["slotType"],
                        //Stackable = (bool)parsedObject["stackable"],
                        //Equipable = (bool)parsedObject["equipable"],
                    };

                    AddLocalizedName(ref itemData, GameLanguage.UnitedStates, parsedObject);
                    AddLocalizedName(ref itemData, GameLanguage.Germany, parsedObject);
                    AddLocalizedName(ref itemData, GameLanguage.Russia, parsedObject);
                    AddLocalizedName(ref itemData, GameLanguage.Poland, parsedObject);
                    AddLocalizedName(ref itemData, GameLanguage.Brazil, parsedObject);
                    AddLocalizedName(ref itemData, GameLanguage.France, parsedObject);
                    AddLocalizedName(ref itemData, GameLanguage.Spain, parsedObject);

                    return itemData;
                }
            }
            catch (Exception ex)
            {
                Debug.Print(ex.ToString());
                return null;
            }
        }

        public static async Task<List<Item>> FindItemsAsync(string searchText)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var obsCollection = Items?.Where(s => (s.LocalizedName().ToLower().Contains(searchText.ToLower())));
                    return new List<Item>(obsCollection ?? throw new InvalidOperationException());

                }
                catch (Exception ex)
                {
                    Debug.Print(ex.Message);
                    return null;
                }
            });
        }

        public static async Task<List<MarketResponse>> GetItemPricesFromJsonAsync(string uniqueName, bool showVillages = false)
        {
            using (var wc = new WebClient())
            {
                var statPricesDataJsonUrl = "https://www.albion-online-data.com/api/v2/stats/prices/" +
                                            uniqueName +
                                            "?locations=Caerleon,Bridgewatch,Thetford,FortSterling,Lymhurst,Martlock,";

                if (showVillages)
                    statPricesDataJsonUrl = "https://www.albion-online-data.com/api/v2/stats/prices/" +
                                            uniqueName +
                                            "?locations=Caerleon,Bridgewatch,Thetford,FortSterling,Lymhurst,Martlock," +
                                            "ForestCross,SteppeCross,HighlandCross,MountainCross,SwampCross,BlackMarket";

                var itemString = await wc.DownloadStringTaskAsync(statPricesDataJsonUrl);
                return JsonConvert.DeserializeObject<List<MarketResponse>>(itemString);
            }
        }
        
        public static ItemTier GetItemTier(string uniqueName) => ItemTiers.FirstOrDefault(x => x.Value == uniqueName.Split('_')[0]).Key;

        public static ItemLevel GetItemLevel(string uniqueName)
        {
            if (!uniqueName.Contains("@"))
                return ItemLevel.Level0;

            if(int.TryParse(uniqueName.Split('@')[1], out int number))
                return ItemLevels.First(x => x.Value == number).Key;
            return ItemLevel.Level0;
        }

        public static int GetQuality(ItemQuality value) => ItemQualities.FirstOrDefault(x => x.Key == value).Value;

        public static ItemQuality GetQuality(int value) => ItemQualities.FirstOrDefault(x => x.Value == value).Key;

        public static ObservableCollection<MarketStatChartItem> MarketStatChartItemList = new ObservableCollection<MarketStatChartItem>();

        public static async Task<string> GetMarketStatAvgPriceAsync(string uniqueName, Location location)
        {
            try
            {
                using (var wc = new WebClient())
                {
                    var apiString = "https://www.albion-online-data.com/api/v1/stats/charts/" +
                                    $"{FormattingUniqueNameForApi(uniqueName)}?date={DateTime.Now:MM-dd-yyyy}";

                    var itemCheck = MarketStatChartItemList?.FirstOrDefault(i => i.UniqueName == uniqueName);

                    if (itemCheck == null)
                    {
                        var itemString = await wc.DownloadStringTaskAsync(apiString);
                        var values = JsonConvert.DeserializeObject<List<MarketStatChartResponse>>(itemString);

                        var newItem = new MarketStatChartItem()
                        {
                            UniqueName = uniqueName,
                            MarketStatChartResponse = values,
                            LastUpdate = DateTime.Now
                        };

                        MarketStatChartItemList?.Add(newItem);

                        var data = newItem.MarketStatChartResponse
                            .FirstOrDefault(itm => itm.Location == Locations.GetName(location))?.Data;
                        var findIndex = data?.TimeStamps?.FindIndex(t => t == data.TimeStamps.Max());

                        if (findIndex != null)
                            return data.PricesAvg[(int) findIndex].ToString("N", LanguageController.DefaultCultureInfo);
                        return "-";
                    }

                    if (itemCheck.LastUpdate <= DateTime.Now.AddHours(-1))
                    {
                        var itemString = await wc.DownloadStringTaskAsync(apiString);
                        var values = JsonConvert.DeserializeObject<List<MarketStatChartResponse>>(itemString);

                        itemCheck.LastUpdate = DateTime.Now;
                        itemCheck.MarketStatChartResponse = values;
                    }

                    var itemCheckData = itemCheck.MarketStatChartResponse
                        .FirstOrDefault(itm => itm.Location == Locations.GetName(location))?.Data;
                    var itemCheckFindIndex =
                        itemCheckData?.TimeStamps?.FindIndex(t => t == itemCheckData.TimeStamps.Max());

                    if (itemCheckFindIndex != null)
                        return itemCheckData.PricesAvg[(int) itemCheckFindIndex]
                            .ToString("N", LanguageController.DefaultCultureInfo);
                    return "-";
                }
            }
            catch (Exception ex)
            {
                Debug.Print(ex.StackTrace);
                Debug.Print(ex.Message);
                return "-";
            }
        }

        #region Support methods

        /// <summary>
        /// Formatting uniqueName for /api/v1/stats/Charts/{itemId}
        /// </summary>
        /// <param name="uniqueName"></param>
        /// <returns></returns>
        private static string FormattingUniqueNameForApi(string uniqueName)
        {
            if (uniqueName.Contains("@1"))
                return uniqueName.Replace("@1", "%401");
            
            if (uniqueName.Contains("@2"))
                return uniqueName.Replace("@2", "%402");
            
            if (uniqueName.Contains("@3"))
                return uniqueName.Replace("@3", "%403");

            return uniqueName;
        }

        private static void AddLocalizedName(ref ItemData itemData, GameLanguage gameLanguage, JObject parsedObject)
        {
            var cultureCode = GameLanguages.FirstOrDefault(x => x.Key == gameLanguage).Value;

            if (parsedObject["localizedNames"][cultureCode] != null)
                itemData.LocalizedNames.Add(new ItemData.KeyValueStruct() { Key = cultureCode, Value = parsedObject["localizedNames"][cultureCode].ToString() });
        }

        #endregion
    }
}