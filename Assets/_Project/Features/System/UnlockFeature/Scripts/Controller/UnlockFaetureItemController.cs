using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Ezg.Feature.System.UnlockFeature
{
    public class UnlockFaetureItemController : MonoBehaviour
    {
        [SerializeField] [TabGroup("Cấu hình")]
        private Image _icon;

        [SerializeField] [TabGroup("Cấu hình")]
        private Text _name;

        //public void InitData(UnlockFeatureProperty type)
        //{
        //    //_icon.sprite = type.feature.GetIconFeature();
        //    _name.text = GameSystems.Localize(type.feature.ToString().ToLower());
        //}
    }
}