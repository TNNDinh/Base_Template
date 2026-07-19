using System.Reflection;
using Ezg.Feature.Gameplay.Battle;
using Ezg.Feature.Social.Account;
using Ezg.Package.Factory;

namespace Ezg.Feature.Shared.GameData
{
    public static class PlayerDataManager
    {
        private static PlayerAccount _account;

        private static PlayerBattleHero _battleHero;

        private static PlayerCampaign _campaign;

        private static PlayerLoginActivity _loginActivity;

        private static PlayerSetting _setting;

        private static PlayerResource _resource;

        private static PlayerUnlockFeature _unlockFeature;

        private static PlayerSilverWeeklyPassData _silverWeeklyPassData;

        private static PlayerGoldWeeklyPassData _goldWeeklyPassData;

        private static PlayerPackDurationData _packDurationData;

        private static PlayerRating _playerRating;

        private static PlayerArenaProgress _arenaProgress;

        public static PlayerAccount Account
        {
            get { return _account ??= DataPlayer.GetModule<PlayerAccount>(); }
            set => _account = value;
        }

        /// <summary>Hero battle: roster tướng đã unlock + đội hình active (persist).</summary>
        public static PlayerBattleHero BattleHero
        {
            get { return _battleHero ??= DataPlayer.GetModule<PlayerBattleHero>(); }
            set => _battleHero = value;
        }

        public static PlayerCampaign Campaign
        {
            get { return _campaign ??= DataPlayer.GetModule<PlayerCampaign>(); }
            set => _campaign = value;
        }

        public static PlayerLoginActivity LoginActivity
        {
            get { return _loginActivity ??= DataPlayer.GetModule<PlayerLoginActivity>(); }
            set => _loginActivity = value;
        }

        public static PlayerSetting Settings
        {
            get { return _setting ??= DataPlayer.GetModule<PlayerSetting>(); }
            set => _setting = value;
        }

        public static PlayerResource PlayerResource
        {
            get { return _resource ??= DataPlayer.GetModule<PlayerResource>(); }
            set => _resource = value;
        }

        public static PlayerUnlockFeature UnlockFeature
        {
            get { return _unlockFeature ??= DataPlayer.GetModule<PlayerUnlockFeature>(); }
            set => _unlockFeature = value;
        }

        public static PlayerSilverWeeklyPassData SilverWeeklyPassData
        {
            get
            {
                _silverWeeklyPassData ??= DataPlayer.GetModule<PlayerSilverWeeklyPassData>();
                return _silverWeeklyPassData;
            }
            set => _silverWeeklyPassData = value;
        }

        public static PlayerGoldWeeklyPassData GoldWeeklyPassData
        {
            get
            {
                _goldWeeklyPassData ??= DataPlayer.GetModule<PlayerGoldWeeklyPassData>();
                return _goldWeeklyPassData;
            }
            set => _goldWeeklyPassData = value;
        }

        public static PlayerPackDurationData PlayerPackDurationData
        {
            get
            {
                _packDurationData ??= DataPlayer.GetModule<PlayerPackDurationData>();
                return _packDurationData;
            }
            set => _packDurationData = value;
        }

        public static PlayerRating PlayerRating
        {
            get
            {
                _playerRating ??= DataPlayer.GetModule<PlayerRating>();
                return _playerRating;
            }
            set => _playerRating = value;
        }

        /// <summary>Tiến trình nâng cấp arena: cấp vũ khí + gold nâng cấp (persist).</summary>
        public static PlayerArenaProgress ArenaProgress
        {
            get { return _arenaProgress ??= DataPlayer.GetModule<PlayerArenaProgress>(); }
            set => _arenaProgress = value;
        }

        public static void ClearCachedModules()
        {
            const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            foreach (var field in typeof(PlayerDataManager).GetFields(flags))
            {
                if (!typeof(DataPlayerBase).IsAssignableFrom(field.FieldType)) continue;

                field.SetValue(null, null);
            }
        }

        #region Shop

        public static PlayerShop _shop;

        public static PlayerShop PlayerShop
        {
            get
            {
                if (_shop == null) _shop = DataPlayer.GetModule<PlayerShop>();

                return _shop;
            }
            set => _shop = value;
        }

        #endregion
    }
}
