using UnityEngine;

[CreateAssetMenu(fileName = "PsdToUnityConfig", menuName = "Configs/PsdToUnityConfig")]
public class PsdToUnityConfigCollection : ScriptableObject
{
    public string spriteFolderPath = "Assets/Sprites/";
    public string currencyFolderPath = "Assets/_Project/Visual/Resources/Images/Currencies/";

    public string buttonPrefabPath =
        "Assets/_Project/Visual/ArtAsset/UI/Prefab/Button_Template/button_template_CTA_txt.prefab";

    public string textPrefabPath = "Assets/_Project/Visual/ArtAsset/UI/Prefab/Text_Template/text_body.prefab";
    public string buttonImagePrefix = "btn";
    public string buttonTextPrefix = "txt";
    public string buttonImageChildName = "btn";
    public string buttonTextChildName = "text_button_CTA";
}