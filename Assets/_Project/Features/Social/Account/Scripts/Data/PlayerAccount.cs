using Ezg.Package.Factory;

namespace Ezg.Feature.Social.Account
{
    public class PlayerAccount : DataPlayerBaseGeneric<PlayerAccountData>
    {
        public string AccountName
        {
            get => dataBase.accountName;
            set
            {
                dataBase.accountName = value;
                Save();
            }
        }

        public string AccountEmail
        {
            get => dataBase.accountEmail;
            set
            {
                dataBase.accountEmail = value;
                Save();
            }
        }

        public string AccountId
        {
            get => dataBase.accountId;
            set
            {
                dataBase.accountId = value;
                Save();
            }
        }

        public PlayerAccountData GetPlayerAccountData()
        {
            return dataBase;
        }

        public bool IsSaveDataToCloud()
        {
            return dataBase.IsSaveDataToCloud;
        }

        public void SetAccountName(string name)
        {
            dataBase.accountName = name;
        }

        public void SetEmail(string email)
        {
            dataBase.accountEmail = email;
        }

        public void SetAccountId(string accountId)
        {
            dataBase.accountId = accountId;
        }

        public string GetAccountName()
        {
            return dataBase.accountName;
        }

        public string GetEmail()
        {
            return dataBase.accountEmail;
        }

        public string GetEmailName()
        {
            return dataBase.accountEmail.Split('@')[0];
        }

        public string GetAccountId()
        {
            return dataBase.accountId;
        }


        public bool ShowedWaringLogin()
        {
            return dataBase.showedWaringLogin;
        }

        public void SetShowedWaringLogin(bool showedWaringLogin)
        {
            dataBase.showedWaringLogin = showedWaringLogin;
        }

        public void SaveDataInCloud()
        {
            dataBase.IsSaveDataToCloud = true;
            Save();
        }
    }
}