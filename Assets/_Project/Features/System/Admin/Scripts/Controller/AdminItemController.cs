using Assets.Scripts._2.BUS.Features.Item;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ezg.Feature.System.Admin
{
    public class AdminItemController : ItemElementController
    {
        [SerializeField] [TabGroup("Cấu hình tính năng")]
        private Button _deleteItem;

        public void SetRemoveItem(UnityAction action)
        {
            _deleteItem.onClick.RemoveAllListeners();
            _deleteItem.onClick.AddListener(action);
        }
    }
}