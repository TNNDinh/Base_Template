using BlackFace.Libraries.Modules.UIModule;

public class ScreenSelectLanguageController : FeatureBaseController, IScreen
{
    private IScreenLogic _logic;

    protected override void Awake()
    {
        base.Awake();
        ScreenLogic.OnClose += CloseMe;
    }

    public IScreenLogic ScreenLogic => _logic ??= GetComponentInChildren<IScreenLogic>();

    public override void LoadData(object data)
    {
        base.LoadData(data);

        ScreenLogic.SetData(data);
    }
}