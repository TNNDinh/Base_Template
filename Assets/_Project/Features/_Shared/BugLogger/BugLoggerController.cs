using System;
using System.Collections.Generic;
using BlackFace.Libraries.Modules.UIModule;
using Cysharp.Threading.Tasks;
using Easygoing.Features.Shared.Tracking;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using Ezg.Feature.Shared.Config;
using Ezg.Feature.Shared.Systems;

namespace Assets._Game._4.CORE.Modules.BugLogger
{
    public class BugLoggerController : FeatureBaseController
    {
        #region Fields

        [SerializeField] [TabGroup("Simple")] [Title("Feedback Container")]
        private GameObject _feedbackObject;

        [SerializeField] [TabGroup("Simple")] [Title("Title Input")]
        private InputField _titleInput;

        [SerializeField] [TabGroup("Simple")] [Title("Content Input")]
        private InputField _contentInput;

        [SerializeField] [TabGroup("Simple")] [Title("Submit Button")]
        private Button _submitButton;

        [SerializeField] [TabGroup("Detail")] [Title("Feature Input")]
        private InputField _featureInput;

        [SerializeField] [TabGroup("Detail")] [Title("Issue Title Input")]
        private InputField _issueTitleInput;

        [SerializeField] [TabGroup("Detail")] [Title("Actual Result Input")]
        private InputField _actualResultInput;

        [SerializeField] [TabGroup("Detail")] [Title("Expected Result Input")]
        private InputField _expectedResultInput;

        [SerializeField] [TabGroup("Detail")] [Title("Steps Input")]
        private InputField _stepsToReproduceInput;

        [SerializeField] [TabGroup("Detail")] [Title("Reproduce Rate Dropdown")]
        private Dropdown _reproduceRateDropdown;

        [SerializeField] [TabGroup("Detail")] [Title("Reproduce Rate Dropdown")]
        private Dropdown _reproduceCategoryDropdown;

        [SerializeField] [TabGroup("Detail")] [Title("Reproduce Rate Dropdown")]
        private Dropdown _reproduceRateDropdown2;

        [SerializeField] [TabGroup("Detail")] [Title("Tag Toggle Prefab")]
        private Toggle _tagTogglePrefab;

        [SerializeField] [TabGroup("Detail")] [Title("Tag Toggle Container")]
        private RectTransform _tagToggleContainer;

        private readonly List<Toggle> _activeTagToggles = new();

        [SerializeField] [TabGroup("Detail")] [Title("Submit Detail Button")]
        private Button _submitDetailButton;

        [SerializeField] [TabGroup("Shared")] [Title("Tag User Dropdown")]
        private Dropdown _tagUserDropdown;

        private GameEnums.Features _currentFeature;
        private byte[] _screenshotBytes;

        private const string NONE_OPTION = "Tag ai?";

        #endregion

        #region Initialize

        protected override void Awake()
        {
            _currentFeature = UIManager.Instance.GetLastFeature();

            base.Awake();
        }

        protected override void Start()
        {
            base.Start();

            // Simple Mode
            _submitButton.onClick.AddListener(OnSubmitSimple);
            _titleInput.text = _currentFeature is GameEnums.Features.Battle ? "Gameplay" : _currentFeature.ToString();

            // Detail Mode
            _submitDetailButton.onClick.AddListener(OnSubmitDetail);
            InitDetailMode();

            LoadEmployeeDropdown().Forget();
            LoadForumTagsAsync().Forget();

            Time.timeScale = 0f;
        }

        #endregion

        #region Public Methods

        public override void LoadData(object data)
        {
            base.LoadData(data);
            _screenshotBytes = (byte[])data;
        }

        /// <summary>
        ///     Override CloseMe để xóa screenshot khỏi bộ nhớ
        /// </summary>
        public override void CloseMe(Action completeAction = null)
        {
            // Xóa screenshot data khỏi bộ nhớ
            _screenshotBytes = null;

            Time.timeScale = 1f;
            base.CloseMe(completeAction);
        }

        #endregion

        #region Private Methods

        private async UniTaskVoid LoadForumTagsAsync()
        {
            if (_tagTogglePrefab == null || _tagToggleContainer == null) return;

            var tags = await DiscordTracking.GetAvailableTagsAsync();

            // Clear previous toggles
            foreach (var t in _activeTagToggles)
                if (t != null)
                    Destroy(t.gameObject);
            _activeTagToggles.Clear();

            for (var i = 0; i < tags.Length; i++)
            {
                var toggle = Instantiate(_tagTogglePrefab, _tagToggleContainer);
                var label = toggle.GetComponentInChildren<Text>();
                if (label != null) label.text = tags[i].Name;
                toggle.SetIsOnWithoutNotify(i == 0); // first tag selected by default
                toggle.onValueChanged.AddListener(isOn => OnTagToggleValueChanged(toggle, isOn));
                _activeTagToggles.Add(toggle);
            }
        }

        private async UniTaskVoid LoadEmployeeDropdown()
        {
            if (_tagUserDropdown == null) return;

            var employees = await EmployeeService.GetEmployeesAsync();
            _tagUserDropdown.ClearOptions();

            var options = new List<Dropdown.OptionData> { new(NONE_OPTION) };
            if (employees != null)
                foreach (var emp in employees)
                    options.Add(new Dropdown.OptionData(emp.name));

            _tagUserDropdown.AddOptions(options);
            _tagUserDropdown.value = 0;
        }

        private void InitDetailMode()
        {
            _featureInput.text = _currentFeature is GameEnums.Features.Battle ? "Gameplay" : _currentFeature.ToString();

            if (_reproduceCategoryDropdown != null)
            {
                _reproduceCategoryDropdown.ClearOptions();
                var categoryOptions = new List<Dropdown.OptionData>();
                foreach (var category in Enum.GetValues(typeof(BugCategories)))
                    categoryOptions.Add(new Dropdown.OptionData(category.ToString()));
                _reproduceCategoryDropdown.AddOptions(categoryOptions);
            }

            // Reproduce Rate
            _reproduceRateDropdown.ClearOptions();
            _reproduceRateDropdown2.ClearOptions();
            var rateOptions = new List<Dropdown.OptionData>();
            for (var i = 0; i < 10; i++) rateOptions.Add(new Dropdown.OptionData(i.ToString()));
            _reproduceRateDropdown.AddOptions(rateOptions);
            _reproduceRateDropdown2.AddOptions(rateOptions);
        }

        private string FormatBugMessageSimple(GameEnums.Features feature, string title, string content)
        {
            var featureName = feature == GameEnums.Features.Battle ? "Gameplay" : feature.ToString();
            var reportType =
                _feedbackObject.activeSelf
                    ? nameof(LoggerTypes.Feedback)
                    : nameof(LoggerTypes.Bug); // Thường là Feedback ở mode simple

            var tagUserStr = GetTagUserString();

            return $"### 📝 {reportType} Report\n" +
                   $"-# **DeviceId:** {SystemInfo.deviceUniqueIdentifier}\n" +
                   $"-# **Device:** {SystemInfo.deviceName}\n" +
                   $"-# **Model:** {SystemInfo.deviceModel}\n" +
                   $"-# **OS:** {SystemInfo.operatingSystem}\n" +
                   $"-# **Version:** {Application.version}\n" +
                   "-# ----------------------------------------------------\n" +
                   $"**Feature:** {featureName}\n" +
                   $"**Title:** {title}\n" +
                   $"**Content:** {content}\n" +
                   tagUserStr;
        }

        private string FormatBugMessageDetail()
        {
            var version = Application.version;
            var feature = _featureInput.text;
            var issueTitle = _issueTitleInput.text;

            var actual = _actualResultInput.text;
            var expected = _expectedResultInput.text;
            var steps = _stepsToReproduceInput.text;

            var rate = _reproduceRateDropdown.options[_reproduceRateDropdown.value].text + "/" +
                       _reproduceRateDropdown2.options[_reproduceRateDropdown2.value].text;


            var platform =
#if UNITY_EDITOR
                "Editor";
#elif UNITY_ANDROID || PLATFORM_ANDROID
                "Android";
#elif PLATFORM_IOS || UNITY_IOS
                "iOS";
#else
                "Other";
#endif

            var tagUserStr = GetTagUserString();

            return "### 🐛 BUG Report (Detailed)\n" +
                   $"**【Actual Result】**\n{actual}\n\n" +
                   $"**【Expected Result】**\n{expected}\n\n" +
                   $"**【Steps to Reproduce】**\n{steps}\n\n" +
                   "-# ----------------------------------------------------\n" +
                   "[Remarks]\n" +
                   $"-# **Version:** {version}\n" +
                   $"-# **Reproduce Rate:** {rate}\n" +
                   $"-# **Platform:** {platform}\n" +
                   $"-# **DeviceId:** {SystemInfo.deviceUniqueIdentifier}\n" +
                   $"-# **OS:** {SystemInfo.operatingSystem}\n" +
                   tagUserStr;
        }

        private string[] GetSelectedForumTagIds()
        {
            if (_activeTagToggles.Count == 0) return null;

            var tags = DiscordTracking.GetCachedTags();
            if (tags == null) return null;

            var selected = new List<string>();
            for (var i = 0; i < _activeTagToggles.Count && i < tags.Length; i++)
                if (_activeTagToggles[i] != null && _activeTagToggles[i].isOn)
                    selected.Add(tags[i].Id);
            return selected.Count > 0 ? selected.ToArray() : null;
        }

        private string GetTagUserString()
        {
            if (_tagUserDropdown != null && _tagUserDropdown.value > 0)
            {
                var cachedEmployees = EmployeeService.GetCachedEmployees();
                if (cachedEmployees != null && _tagUserDropdown.value <= cachedEmployees.Count)
                {
                    var selectedEmployee = cachedEmployees[_tagUserDropdown.value - 1];
                    return $"**Tag:** <@{selectedEmployee.id}>\n";
                }
            }

            return "";
        }

        private void SendToDiscord(string message)
        {
            if (_screenshotBytes != null && _screenshotBytes.Length > 0)
                DiscordTracking.TrackingWithScreenShot(message, _screenshotBytes);
            else
                DiscordTracking.Tracking(message);
        }

        #endregion

        #region Event Handlers

        private const int MAX_SELECTED_TAGS = 5;
        private const int MAX_THREAD_TITLE_LENGTH = 100;
        private const int MAX_MESSAGE_CONTENT_LENGTH = 2000;

        private void OnSubmitSimple()
        {
            var title = _titleInput.text;
            var content = _contentInput.text;

            if (string.IsNullOrEmpty(content))
            {
                GameSystems.ShowSimpleMessage("Input content please...!");
                return;
            }

            var message = FormatBugMessageSimple(_currentFeature, title, content);
            SendToDiscord(message);

            // Reset input sau khi gửi
            _titleInput.text = string.Empty;
            _contentInput.text = string.Empty;

            CloseMe();
        }

        private void OnSubmitDetail()
        {
            if (string.IsNullOrEmpty(_issueTitleInput.text) || string.IsNullOrEmpty(_actualResultInput.text))
            {
                GameSystems.ShowSimpleMessage("Please fill Issue Title and Actual Result!");
                return;
            }

            var appVersionLabel = Application.isEditor ? "Editor" : Application.version;
            var title =
                $"[{appVersionLabel}]-[{_reproduceCategoryDropdown.options[_reproduceCategoryDropdown.value].text}]-[{_featureInput.text}] {_issueTitleInput.text}";

            // Validate thread title: Discord requires 1-100 characters
            if (title.Length > MAX_THREAD_TITLE_LENGTH)
            {
                GameSystems.ShowSimpleMessage(
                    $"Thread title too long!\nMax {MAX_THREAD_TITLE_LENGTH} chars.\nCurrent: {title.Length}");
                return;
            }

            var message = FormatBugMessageDetail();

            // Validate message content: Discord requires <= 2000 characters
            if (message.Length > MAX_MESSAGE_CONTENT_LENGTH)
            {
                GameSystems.ShowSimpleMessage(
                    $"Message too long!\nMax {MAX_MESSAGE_CONTENT_LENGTH} chars.\nCurrent: {message.Length}");
                return;
            }

            DiscordTracking.TrackingCreateThreadWithScreenshot(title, message, _screenshotBytes,
                GetSelectedForumTagIds());

            // Reset
            _issueTitleInput.text = string.Empty;
            _actualResultInput.text = string.Empty;
            _expectedResultInput.text = string.Empty;
            _stepsToReproduceInput.text = string.Empty;

            CloseMe();
        }

        private void OnTagToggleValueChanged(Toggle changedToggle, bool isOn)
        {
            if (!isOn || changedToggle == null) return;

            if (GetSelectedTagCount() <= MAX_SELECTED_TAGS) return;

            changedToggle.SetIsOnWithoutNotify(false);
            GameSystems.ShowSimpleMessage($"You can select up to {MAX_SELECTED_TAGS} tags only!");
        }

        private int GetSelectedTagCount()
        {
            var count = 0;
            for (var i = 0; i < _activeTagToggles.Count; i++)
                if (_activeTagToggles[i] != null && _activeTagToggles[i].isOn)
                    count++;
            return count;
        }

        #endregion
    }

    public enum LoggerTypes
    {
        Bug,
        Feedback
    }

    public enum BugCategories
    {
        Visual,
        Logic,
        GD
    }

    public enum ReproduceRate
    {
        Always_100,
        Often_75,
        Sometimes_50,
        Rarely_25,
        Random
    }
}