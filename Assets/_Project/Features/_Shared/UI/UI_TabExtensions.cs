using System;
using System.Collections.Generic;
using BlackFace.Libraries.Modules.UIModule;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Ezg.Feature.Shared.Systems;

namespace Ezg.Core.Extensions
{
    [Serializable]
    public class TabListObject
    {
        [SerializeField] [TabGroup("Cấu hình")]
        public List<GameObject> ObjectList;
    }

    public class UI_TabExtensions : MonoBehaviour
    {
        #region Fields

        [SerializeField] [TabGroup("Cấu hình")]
        private CanvasScaler _mainCanvasScale;

        [SerializeField] [TabGroup("Cấu hình")]
        private List<Toggle> _toggleList;

        [SerializeField] [TabGroup("Cấu hình")]
        private List<GameObject> _objectList;

        [SerializeField] [TabGroup("Cấu hình")]
        private bool _useListObjects;

        [SerializeField] [ShowIf("@_useListObjects == true && _useAnimSwap == false")] [TabGroup("Cấu hình")]
        private List<TabListObject> _objectLists;

        [SerializeField] [TabGroup("Cấu hình")]
        private int _indexOnOpen = -1;

        [SerializeField] [TabGroup("Cấu hình")]
        private bool _useAnimSwap;

        private bool _isFirstSetup;
        private List<UnityAction> _onChangeAction;
        private int _selectedIndex;

        #endregion

        #region Initialize

        private void Awake()
        {
            _onChangeAction = new List<UnityAction>();
            var count = _toggleList.Count;
            for (var i = 0; i < count; i++)
            {
                _toggleList[i].onValueChanged.AddListener(OnChange);
                _onChangeAction.Add(null);
            }

            OnChange(true);
        }

        private void OnEnable()
        {
            if (_indexOnOpen != -1) SetTabIndex(_indexOnOpen);
        }

        #endregion

        #region Functions

        public void RegisterOnchangeAction(int index, UnityAction action)
        {
            _onChangeAction[index] = action;
        }

        private void OnChange(bool isOn)
        {
            if (_useAnimSwap && _isFirstSetup)
            {
                OnChangeAnimation(isOn);
            }
            else
            {
                var count = _toggleList.Count;
                for (var i = 0; i < count; i++)
                    if (_useListObjects)
                        _objectLists[i].ObjectList.ForEach(x => x.SetActive(_toggleList[i].isOn));
                    else
                        _objectList[i].SetActive(_toggleList[i].isOn);
            }

            _isFirstSetup = true;

            for (var i = 0; i < _toggleList.Count; i++)
                if (_toggleList[i].isOn)
                {
                    _selectedIndex = i;
                    _onChangeAction[i]?.Invoke();
                    return;
                }
        }

        public void SetTabIndex(int index)
        {
            this.DelayMethod(.01f, () =>
            {
                _toggleList[index].isOn = true;
                _selectedIndex = index;
            });
        }

        private void OnChangeAnimation(bool isOn)
        {
            if (!isOn) return;

            int indexOpen = -1, indexClose = -1;
            var count = _toggleList.Count;
            for (var i = 0; i < count; i++)
            {
                if (_objectList[i].activeSelf) indexClose = i;

                if (_toggleList[i].isOn) indexOpen = i;
            }

            if (indexClose == indexOpen)
            {
                _selectedIndex = indexOpen;
                return;
            }

            if (indexClose == -1)
            {
                _objectList[indexOpen].SetActive(true);
                _selectedIndex = indexOpen;
                _onChangeAction[indexOpen]?.Invoke();
                return;
            }

            UIManager.EnableTouch(false);
            _objectList[indexOpen].transform.localPosition = new Vector2(
                indexOpen < indexClose
                    ? 0 - _mainCanvasScale.referenceResolution.x
                    : _mainCanvasScale.referenceResolution.x, _objectList[indexOpen].transform.localPosition.y);
            _objectList[indexOpen].SetActive(true);
            _objectList[indexOpen].transform.DOLocalMoveX(0, .3f);
            _objectList[indexClose].transform
                .DOLocalMoveX(
                    indexOpen < indexClose
                        ? _mainCanvasScale.referenceResolution.x
                        : 0 - _mainCanvasScale.referenceResolution.x, .3f).OnComplete(() =>
                {
                    _objectList[indexClose].SetActive(false);
                    _objectList[indexClose].transform.DOLocalMoveX(0, 0f);
                    UIManager.EnableTouch(true);
                });
            _selectedIndex = indexOpen;
            _onChangeAction[indexOpen]?.Invoke();
        }

        public List<Toggle> GetToggleList()
        {
            return _toggleList;
        }

        public List<GameObject> GetObjectList()
        {
            return _objectList;
        }

        public void JumpToIndex(int slot)
        {
            _toggleList[slot].isOn = true;
            _selectedIndex = slot;
        }

        public int GetIndexSelected()
        {
            return _selectedIndex;
        }

        #endregion
    }
}