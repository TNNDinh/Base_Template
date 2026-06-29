using System.Collections.Generic;
using Ezg.Feature.Shared;
using Ezg.Tracking;
using Newtonsoft.Json;
using TigerForge;
using Ezg.Feature.Shared.GameData;
using Ezg.Feature.Shared.Systems;

namespace Ezg.Feature.IAP
{
    /// <summary>
    ///     Implementation game-specific cho các seam của module IAP:
    ///     định danh/thống kê người chơi (<see cref="IIapProfile" />) và báo cáo
    ///     analytics/đồng bộ (<see cref="IIapReporter" />).
    ///     Toàn bộ coupling tới PlayerDataManager / GameSystems / Tracking / EventName nằm ở đây.
    /// </summary>
    public class GameIapHost : IIapProfile, IIapReporter
    {
        #region IIapProfile

        public string AccountId => PlayerDataManager.Account.AccountId;

        public bool IsCheatEnabled => GameSystems.isCheat;

        public void RecordPurchase(decimal localizedPrice)
        {
            PlayerDataManager.PlayerShop.dataBase.IAPCount++;
            PlayerDataManager.PlayerShop.dataBase.IAPRevenue += localizedPrice;
            PlayerDataManager.PlayerShop.Save();

            // Refresh user property SAU khi cập nhật stats — giữ đúng thứ tự bản gốc
            // (SetUserProperty đọc dữ liệu doanh thu/đếm mua vừa tăng).
            TrackingService.SetUserProperty();
        }

        #endregion

        #region IIapReporter

        public void OnPurchaseClick(IapPurchaseInfo info)
        {
            FirebaseEvent.purchase_click.Send(new FirebaseEventConfig
            {
                source = info.Source,
                source_detail = info.SourceId,
                product_id = info.ProductId,
                price_usd = ""
            });
        }

        public void OnPurchaseValidated(IapPurchaseInfo info)
        {
            AppFlyerEvent.af_purchase.Send(new AppflyerEventConfig
            {
                player_id = AccountId,
                af_revenue = GetAppsflyerRevenue(info.LocalizedPrice),
                af_content_id = info.ProductId,
                af_currency = info.IsoCurrencyCode
            });
        }

        public void OnConversionData(string conversionJson)
        {
            TrackingService.SetUAProperties(ParseJson(conversionJson));
        }

        public void RequestSync()
        {
            EventManager.EmitEvent(EventName.ForceSyncData);
        }

        #endregion

        #region Private

        private static string GetAppsflyerRevenue(decimal amount)
        {
            return amount.ToString("F20");
        }

        private static Dictionary<string, string> ParseJson(string json)
        {
            try
            {
                return JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            }
            catch
            {
                return null;
            }
        }

        #endregion
    }
}