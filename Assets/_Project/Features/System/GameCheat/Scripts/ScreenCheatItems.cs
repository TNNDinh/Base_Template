using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Ezg.Feature.Shared;
using TigerForge;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Ezg.Feature.Shared.GameData;
using Ezg.Feature.Shared.Systems;

public class ScreenCheatItems : MonoBehaviour
{
    private const float DropdownIconSize = 160f;
    private const float DropdownIconPadding = 16f;
    private const string GeneratedDropdownIconName = "Generated Dropdown Icon";

    [Header("UI References")] public TMP_Dropdown typeDropdown;

    public TMP_InputField searchField;
    public Transform itemContainer;
    public ItemCheat itemPrefab;

    [SerializeField] private Image imageItemSelect;
    [SerializeField] private Text textItemSelect;
    [SerializeField] private Button buttonAdd;
    [SerializeField] private Button buttonGetGenerator;
    [SerializeField] private Button buttonAddAllInGen;

    [Header("Loading UI")] [SerializeField]
    private GameObject loadingPanel;

    [SerializeField] private Text loadingText;
    [SerializeField] private Slider loadingSlider;

    [Header("Pool Settings")] [SerializeField]
    private int initialPoolSize = 50;

    [SerializeField] private int itemsPerFrame = 20;


    [HideInInspector] public ItemSave itemSelect;
    private int activePoolCount;
    private List<CachedItemData> allItemsFlat;

    private MergeEnum.MergeItemTypes currentType;
    private CancellationTokenSource displayCts;
    private bool isInitialized;
    private bool isLoading;

    private Dictionary<MergeEnum.MergeItemTypes, List<CachedItemData>> itemCache;
    private List<ItemCheat> itemPool;

    private CancellationTokenSource loadingCts;
    private List<MergeEnum.MergeItemTypes> typeDropdownValues;

    private void Start()
    {
        InitializeButtons();
        EnsureTypeDropdownImageSlots();
        SetupTypeDropdown();
        typeDropdown.onValueChanged.AddListener(OnTypeChanged);
        searchField.onValueChanged.AddListener(OnSearchChanged);
        SetLoadingUI(false);
    }

    private void OnEnable()
    {
        if (!isInitialized && !isLoading)
        {
            loadingCts = new CancellationTokenSource();
            InitializeCacheAsync(loadingCts.Token).Forget();
        }
    }

    private void OnDisable()
    {
        CancelAllTasks();
    }

    private void OnDestroy()
    {
        CancelAllTasks();
    }

    private void CancelAllTasks()
    {
        loadingCts?.Cancel();
        loadingCts?.Dispose();
        loadingCts = null;

        displayCts?.Cancel();
        displayCts?.Dispose();
        displayCts = null;
    }

    private void InitializeButtons()
    {
        buttonAdd.onClick.RemoveAllListeners();
        buttonAdd.onClick.AddListener(OnAddButtonClick);

        if (buttonGetGenerator != null)
        {
            buttonGetGenerator.onClick.RemoveAllListeners();
            buttonGetGenerator.onClick.AddListener(GetItemGenerator);
        }

        if (buttonAddAllInGen != null)
        {
            buttonAddAllInGen.onClick.RemoveAllListeners();
            buttonAddAllInGen.onClick.AddListener(AddAllItemInGen);
        }
    }

    private void OnAddButtonClick()
    {
        if (itemSelect == null) return;
        // removed: PlayerDataManager.Gameplay (gameplay removed)
        EventManager.EmitEvent(EventName.UpdateResource);
    }

    private void SetLoadingUI(bool show, string message = "Loading...", float progress = 0f)
    {
        if (loadingPanel != null)
            loadingPanel.SetActive(show);

        if (loadingText != null)
            loadingText.text = message;

        if (loadingSlider != null)
            loadingSlider.value = progress;

        typeDropdown.interactable = !show;
        searchField.interactable = !show;
    }

    private async UniTaskVoid InitializeCacheAsync(CancellationToken ct)
    {
        try
        {
            isLoading = true;
            SetLoadingUI(true, "Initializing...");

            itemCache = new Dictionary<MergeEnum.MergeItemTypes, List<CachedItemData>>();
            allItemsFlat = new List<CachedItemData>();

            var types = Enum.GetValues(typeof(MergeEnum.MergeItemTypes))
                .Cast<MergeEnum.MergeItemTypes>()
                .Where(t => t != MergeEnum.MergeItemTypes.None)
                .ToArray();

            var totalTypes = types.Length;
            var processedTypes = 0;

            foreach (var type in types)
            {
                ct.ThrowIfCancellationRequested();

                SetLoadingUI(true, $"Loading {type}...", (float)processedTypes / totalTypes);

                var typeItems = await LoadItemsForTypeAsync(type, ct);

                itemCache[type] = typeItems;
                allItemsFlat.AddRange(typeItems);

                processedTypes++;
            }

            SetLoadingUI(true, "Creating pool...", 0.95f);
            await InitializePoolAsync(ct);
            RefreshTypeDropdownIcons();

            isInitialized = true;
            isLoading = false;

            SetLoadingUI(false);

            if (typeDropdown.options.Count > 0) OnTypeChanged(typeDropdown.value);
        }
        catch (OperationCanceledException)
        {
            SetLoadingUI(false);
        }
        finally
        {
            isLoading = false;
        }
    }

    private async UniTask<List<CachedItemData>> LoadItemsForTypeAsync(MergeEnum.MergeItemTypes type,
        CancellationToken ct)
    {
        // gameplay removed: không còn dữ liệu/sprite item merge nên danh sách item luôn rỗng
        await UniTask.CompletedTask;
        return new List<CachedItemData>();
    }

    private async UniTask InitializePoolAsync(CancellationToken ct)
    {
        itemPool = new List<ItemCheat>(initialPoolSize);

        for (var i = 0; i < initialPoolSize; i++)
        {
            ct.ThrowIfCancellationRequested();

            CreatePoolItem();

            if (i % itemsPerFrame == 0) await UniTask.Yield(ct);
        }
    }

    private ItemCheat CreatePoolItem()
    {
        var item = Instantiate(itemPrefab, itemContainer);
        item.Hide();
        itemPool.Add(item);
        return item;
    }

    private ItemCheat GetPoolItem()
    {
        if (activePoolCount >= itemPool.Count) CreatePoolItem();

        var item = itemPool[activePoolCount];
        activePoolCount++;
        return item;
    }

    private void ResetPool()
    {
        for (var i = 0; i < activePoolCount; i++) itemPool[i].Hide();
        activePoolCount = 0;
    }

    private void SetupTypeDropdown()
    {
        typeDropdown.ClearOptions();
        typeDropdownValues = Enum.GetValues(typeof(MergeEnum.MergeItemTypes))
            .Cast<MergeEnum.MergeItemTypes>()
            .ToList();

        typeDropdown.AddOptions(typeDropdownValues.Select(type => type.ToString()).ToList());
    }

    private void EnsureTypeDropdownImageSlots()
    {
        if (typeDropdown.captionImage == null && typeDropdown.captionText != null)
            typeDropdown.captionImage = CreateDropdownOptionImage(typeDropdown.captionText.rectTransform);

        if (typeDropdown.itemImage == null && typeDropdown.itemText != null)
            typeDropdown.itemImage = CreateDropdownOptionImage(typeDropdown.itemText.rectTransform);
    }

    private Image CreateDropdownOptionImage(RectTransform textRect)
    {
        var parent = textRect.parent;
        var existing = parent.Find(GeneratedDropdownIconName);
        var image = existing != null ? existing.GetComponent<Image>() : null;

        if (image == null)
        {
            var iconObject = new GameObject(GeneratedDropdownIconName, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image));
            iconObject.layer = textRect.gameObject.layer;
            iconObject.transform.SetParent(parent, false);
            iconObject.transform.SetSiblingIndex(textRect.GetSiblingIndex());
            image = iconObject.GetComponent<Image>();
        }

        image.raycastTarget = false;
        image.preserveAspect = true;

        var iconRect = image.rectTransform;
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.sizeDelta = new Vector2(DropdownIconSize, DropdownIconSize);
        iconRect.anchoredPosition = new Vector2(DropdownIconPadding + DropdownIconSize * 0.5f, 0f);

        var offsetMin = textRect.offsetMin;
        offsetMin.x = Mathf.Max(offsetMin.x, DropdownIconPadding * 2f + DropdownIconSize);
        textRect.offsetMin = offsetMin;

        return image;
    }

    private void RefreshTypeDropdownIcons()
    {
        if (typeDropdownValues == null || typeDropdownValues.Count == 0) return;

        var selectedIndex = typeDropdown.value;
        var options = new List<TMP_Dropdown.OptionData>(typeDropdownValues.Count);

        foreach (var type in typeDropdownValues)
            options.Add(new TMP_Dropdown.OptionData(type.ToString(), GetFirstTypeSprite(type), Color.white));

        typeDropdown.ClearOptions();
        typeDropdown.AddOptions(options);
        typeDropdown.SetValueWithoutNotify(Mathf.Clamp(selectedIndex, 0, options.Count - 1));
        typeDropdown.RefreshShownValue();
    }

    private Sprite GetFirstTypeSprite(MergeEnum.MergeItemTypes type)
    {
        if (itemCache == null || !itemCache.TryGetValue(type, out var items)) return null;

        return items.FirstOrDefault(item => item.Sprite != null).Sprite;
    }

    private void OnTypeChanged(int index)
    {
        if (!isInitialized) return;

        if (typeDropdownValues == null || index < 0 || index >= typeDropdownValues.Count)
        {
            Debug.LogError($"Invalid item type dropdown index: {index}");
            return;
        }

        var selectedType = typeDropdownValues[index];
        currentType = selectedType;
        searchField.text = string.Empty;
        DisplayItemsByType(selectedType);
    }

    private void OnSearchChanged(string keyword)
    {
        if (!isInitialized) return;

        keyword = keyword.Trim().ToLower();

        if (string.IsNullOrEmpty(keyword))
        {
            DisplayItemsByType(currentType);
            return;
        }

        SearchAllTypes(keyword);
    }

    private void DisplayItemsByType(MergeEnum.MergeItemTypes type)
    {
        CancelDisplayTask();

        ResetPool();

        if (!itemCache.TryGetValue(type, out var items)) return;

        displayCts = new CancellationTokenSource();
        DisplayItemsAsync(items, displayCts.Token).Forget();
    }

    private void SearchAllTypes(string keyword)
    {
        CancelDisplayTask();

        ResetPool();

        var filteredItems = allItemsFlat
            .Where(item => item.NameLower.Contains(keyword) || item.IdString.Contains(keyword))
            .ToList();

        displayCts = new CancellationTokenSource();
        DisplayItemsAsync(filteredItems, displayCts.Token).Forget();
    }

    private async UniTaskVoid DisplayItemsAsync(List<CachedItemData> items, CancellationToken ct)
    {
        try
        {
            var count = 0;

            foreach (var item in items)
            {
                ct.ThrowIfCancellationRequested();

                DisplayItem(item);
                count++;

                if (count % itemsPerFrame == 0) await UniTask.Yield(ct);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void CancelDisplayTask()
    {
        displayCts?.Cancel();
        displayCts?.Dispose();
        displayCts = null;
    }

    private void DisplayItem(CachedItemData data)
    {
        var uiItem = GetPoolItem();
        uiItem.SetData(data.Sprite, data.NameItem, () => OnItemSelected(data));
        uiItem.Show();
    }

    private void OnItemSelected(CachedItemData data)
    {
        itemSelect = new ItemSave
        {
            idItem = data.Id,
            itemSaveType = data.Type
        };
        imageItemSelect.sprite = data.Sprite;
        textItemSelect.text = data.NameItem;
    }

    public void GetItemGenerator()
    {
        if (!isInitialized) return;

        CancelDisplayTask();

        ResetPool();

        // removed: DataManager.ItemGenerator (gameplay removed)
    }

    public void AddAllItemInGen()
    {
        if (itemSelect == null) return;
        // removed: DataManager.ItemGenerator, DataManager.ItemExpand, PlayerDataManager.Gameplay (gameplay removed)
        GameSystems.ShowSimpleMessage("Generator add disabled (gameplay removed)");
    }

    private struct CachedItemData
    {
        public Sprite Sprite;
        public string NameItem;
        public string NameLower;
        public string IdString;
        public MergeEnum.MergeItemTypes Type;
        public int Id;
    }
}