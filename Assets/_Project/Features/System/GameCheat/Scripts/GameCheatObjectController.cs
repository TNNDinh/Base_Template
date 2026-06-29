using TigerForge;
using UnityEngine;
using Ezg.Feature.Shared.Systems;

namespace Ezg.Feature.System.GameCheat
{
    /// <summary>
    ///     Bật tắt cheat sẽ bật tắt object này
    /// </summary>
    public class GameCheatObjectController : MonoBehaviour
    {
        //public bool IsBattleCheatUI;
        public bool IsOnlyEditor;

        private CanvasGroup _canvasGroup;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        private void Start()
        {
            EventManager.StartListening(nameof(EventName.CheatChanged), ShowObject);
            EventManager.StartListening(nameof(EventName.BattleCheatUpdateUI), ShowObject);
            ShowObject();
            CheckCheat();
        }

        private void OnEnable()
        {
            EventManager.StartListening(EventName.CheatHideUIUA, HideUI);
            EventManager.StartListening(EventName.CheatShowUIUA, ShowUI);
        }

        private void OnDisable()
        {
            EventManager.StopListening(EventName.CheatShowUIUA, ShowUI);
            EventManager.StopListening(EventName.CheatHideUIUA, HideUI);
        }

        private void ShowObject()
        {
            gameObject.SetActive(GameSystems.isCheat);

            if (IsOnlyEditor)
            {
#if UNITY_EDITOR
                gameObject.SetActive(GameSystems.isCheat);
#else
                gameObject.SetActive(false);
#endif
            }
        }


        private void CheckCheat()
        {
            if (GameCheatManager.isHideCheatUI)
                HideUI();
            else
                ShowUI();
        }

        private void HideUI()
        {
            _canvasGroup.alpha = 0f;
        }

        private void ShowUI()
        {
            _canvasGroup.alpha = 1f;
        }
    }
}