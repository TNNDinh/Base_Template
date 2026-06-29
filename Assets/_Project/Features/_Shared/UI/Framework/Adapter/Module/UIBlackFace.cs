using System;
using BlackFace.Libraries.Modules.UIModule;
using Cysharp.Threading.Tasks;
using Ezg.Core.Utils;
using Ezg.Feature.Shared.Systems;
using Ezg.Feature.Shared.Config;

public class UIBlackFace : IUIFramework
{
    public void Show(string key, params object[] custom)
    {
        var data = custom.Length > 0 ? custom[0] : null;
        var grp = custom.Length > 1 ? (UIManager.UIGroupName)custom[1] : UIManager.UIGroupName.Modal_Container;
        UIManager.Instance.Show(CoreUtils.ParseEnum<GameEnums.Features>(key), grp, data: data).Forget();
    }

    public void Close(string key)
    {
        UIManager.Instance.CloseFeature(CoreUtils.ParseEnum<GameEnums.Features>(key));
    }

    public void Close(string key, Action onComplete)
    {
        UIManager.Instance.CloseFeature(CoreUtils.ParseEnum<GameEnums.Features>(key));
    }

    public void CloseLastUI()
    {
        UIManager.Instance.CloseLastestUI();
    }
}