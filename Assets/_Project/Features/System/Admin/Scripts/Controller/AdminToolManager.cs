using System;
using System.Linq;
using Ezg.Core.Extensions;
using Ezg.Core.Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using UnityEngine;
using Ezg.Feature.Shared.Systems;

namespace Ezg.Feature.System.Admin
{
    [FirestoreData]
    public class AdminAccountModel
    {
        [FirestoreProperty] public string username { get; set; }

        [FirestoreProperty] public string pass { get; set; }
    }

    public static class AdminToolManager
    {
        private static FirebaseAuth _firebaseAuth;
        private static FirebaseUser user;
        private static FirebaseFirestore db;
        private static string _adminFirestore = "AdminAccount";
        private static readonly string _adminUsernameSave = "AdminUsername";
        private static readonly string _adminPasswordSave = "AdminPassword";
        private static readonly string _adminKeepSignInSave = "AdminKeepSignin";
        public static string UserIdGetted;
        public static string UserAdmin;

        static AdminToolManager()
        {
            _firebaseAuth = FirebaseAuth.DefaultInstance;
            db = FirebaseFirestore.DefaultInstance;
        }

        public static string GetUsernameSaved()
        {
            return PlayerPrefs.GetString(_adminUsernameSave);
        }

        public static string GetPasswordSaved()
        {
            return PlayerPrefs.GetString(_adminPasswordSave);
        }

        public static bool IsKeepSignin()
        {
            return PlayerPrefs.GetInt(_adminKeepSignInSave) != 0;
        }

        //public static void LoginAccount(string userName, string password, bool keepSignIn, Action successAction)
        //{
        //    if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))
        //    {
        //        GameSystems.Instance.ShowSimpleMessage("Login failed");
        //        return;
        //    }

        //    GameSystems.Instance.ShowWaitingGiftCode(true);
        //    DocumentReference giftRef = db.Collection(_adminFirestore).Document(userName);
        //    giftRef.GetSnapshotAsync().ContinueWithOnMainThread(x =>
        //    {
        //        if (x.Result.Exists)
        //        {
        //            var data = x.Result.ConvertTo<AdminAccountModel>();
        //            if (data.password.Equals(password))
        //            {
        //                if (keepSignIn)
        //                {
        //                    PlayerPrefs.SetString(_adminUsernameSave, userName);
        //                    PlayerPrefs.SetString(_adminPasswordSave, password);
        //                }
        //                else
        //                {
        //                    PlayerPrefs.SetString(_adminUsernameSave, userName);
        //                    PlayerPrefs.DeleteKey(_adminPasswordSave);
        //                }
        //                PlayerPrefs.SetInt(_adminKeepSignInSave, keepSignIn ? 1 : 0);
        //                PlayerPrefs.Save();
        //                successAction?.Invoke();
        //                UserAdmin = userName;
        //            }
        //            else
        //            {
        //                FirebaseFirestoreManager.WaitingStatus = FirebaseFirestoreManager.FirestoreStatus.Faulted;
        //                GameSystems.Instance.ShowSimpleMessage("Login failed");
        //            }
        //        }
        //        else
        //        {
        //            FirebaseFirestoreManager.WaitingStatus = FirebaseFirestoreManager.FirestoreStatus.Faulted;
        //            GameSystems.Instance.ShowSimpleMessage("Login failed");
        //        }
        //        GameSystems.Instance.ShowWaitingGiftCode(false);
        //    });
        //}

        //public static void ChangeAccountPassword(string password, Action successAction)
        //{
        //    GameSystems.Instance.ShowWaitingGiftCode(true);
        //    DocumentReference giftRef = db.Collection(_adminFirestore).Document(UserAdmin);
        //    giftRef.GetSnapshotAsync().ContinueWithOnMainThread(x =>
        //    {
        //        if (x.Result.Exists)
        //        {
        //            var data = x.Result.ConvertTo<AdminAccountModel>();
        //            data.password = password;

        //            giftRef.SetAsync(data.ToDictionary()).ContinueWithOnMainThread(task =>
        //            {
        //                GameSystems.Instance.ShowSimpleMessage("Change password successfully!");
        //                successAction?.Invoke();
        //                if (IsKeepSignin())
        //                {
        //                    PlayerPrefs.SetString(_adminPasswordSave, password);
        //                }
        //            });
        //        }
        //        else
        //        {
        //            FirebaseFirestoreManager.WaitingStatus = FirebaseFirestoreManager.FirestoreStatus.Faulted;
        //            GameSystems.Instance.ShowSimpleMessage("Cannot change password");
        //        }
        //        GameSystems.Instance.ShowWaitingGiftCode(false);
        //    });
        //}

        public static void CheckUserEmail(string userEmail, bool isShowMessage = true, Action successAction = null)
        {
            GameSystems.ShowWaitingScreen(true);

            var function = FirebaseFunctionManager.Function.GetHttpsCallable("checkEmailExists");
            function.CallAsync(userEmail).ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    GameSystems.ShowSimpleMessage("Server error");
                }
                else
                {
                    var a = task.Result.Data;
                    var b = a.ToDictionary<string>();
                    if (isShowMessage && string.IsNullOrEmpty(b.ElementAt(0).Value))
                        GameSystems.ShowSimpleMessage("User" +
                                                      (!string.IsNullOrEmpty(b.ElementAt(0).Value) ? "" : " not") +
                                                      " available");

                    if (!string.IsNullOrEmpty(b.ElementAt(0).Value))
                    {
                        UserIdGetted = b.ElementAt(0).Value;
                        successAction?.Invoke();
                    }
                }

                GameSystems.ShowWaitingScreen(false);
            });
        }

        //public static void SendMail(string userEmail, string title, string content, List<ResourceBase> reward)
        //{
        //    if (string.IsNullOrEmpty(userEmail))
        //    {
        //        GameSystems.Instance.ShowSimpleMessage("Input email please");
        //        return;
        //    }

        //    if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(content))
        //    {
        //        GameSystems.Instance.ShowSimpleMessage("Input title and content please");
        //        return;
        //    }

        //    void SendMailAction()
        //    {
        //        if (string.IsNullOrEmpty(UserIdGetted))
        //        {
        //            GameSystems.Instance.ShowSimpleMessage("User not available");
        //            return;
        //        }

        //        var mailId = Guid.NewGuid().ToString();
        //        DocumentReference docRefRewardMail = db.Collection("MailBox").Document(UserIdGetted).Collection("Mails").Document(mailId);

        //        var mailResult = new PlayerMailBox()
        //        {
        //            id = mailId,
        //            mailType = 0,
        //            readed = false,
        //            subject = title,
        //            content = content,
        //            createdTime = GameSystems.GetCurrentServerTime(),

        //            resId = reward?.Select(x => x.ResourceType == EnumBase.ResourceTypes.Hero ? ((HeroResources)x).HeroId : x.ResourceType == EnumBase.ResourceTypes.Skill ? ((SkillResources)x).SkillId : x.Id.ToString()).ToArray(),
        //            resType = reward?.Select(x => x.ResourceType).ToArray(),
        //            resNumber = reward?.Select(x => x.Quantity).ToArray(),
        //        };

        //        GameSystems.Instance.ShowWaitingGiftCode(true);
        //        docRefRewardMail.SetAsync(mailResult.ToDictionary()).ContinueWithOnMainThread(task =>
        //        {
        //            GameSystems.Instance.ShowSimpleMessage("Send email successfully!");
        //            GameSystems.Instance.ShowWaitingGiftCode(false);
        //        });
        //    }

        //    CheckUserEmail(userEmail, false, SendMailAction);
        //}

        //public static void CreateGiftCode(List<ResourceBase> reward, bool isActive, bool isLimit, int limitTimes)
        //{
        //    if (reward is not { Count: > 0 })
        //    {
        //        GameSystems.Instance.ShowSimpleMessage("Please add resources");
        //        return;
        //    }

        //    var giftCode = Extensions.RandomString(6);
        //    DocumentReference docRef = db.Collection("GiftCode").Document(giftCode);

        //    var giftCodeData = new GiftCodeModel()
        //    {
        //        id = giftCode,
        //        isActive = isActive,
        //        isLimit = isLimit,
        //        limitTimes = limitTimes,
        //        claimedTimes = 0,
        //        createdBy = UserAdmin,

        //        resId = reward?.Select(x => x.ResourceType == EnumBase.ResourceTypes.Hero ? ((HeroResources)x).HeroId : x.ResourceType == EnumBase.ResourceTypes.Skill ? ((SkillResources)x).SkillId : x.Id.ToString()).ToArray(),
        //        resType = reward?.Select(x => x.ResourceType).ToArray(),
        //        resNumber = reward?.Select(x => x.Quantity).ToArray(),
        //    };

        //    GameSystems.Instance.ShowWaitingGiftCode(true);
        //    docRef.SetAsync(giftCodeData.ToDictionary()).ContinueWithOnMainThread(task =>
        //    {
        //        GameSystems.Instance.ShowSimpleMessage("Create gift code successfully: " + giftCode);
        //        GUIUtility.systemCopyBuffer = giftCode;
        //        GameSystems.Instance.ShowWaitingGiftCode(false);
        //    });
        //}
    }
}