using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class ShopManager : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private SellingObjectSO[] SellingObjects;
    [SerializeField] private InventorySO inventoryData;
    [SerializeField] private PlayerData playerData;
    [SerializeField] private InventoryUIController inventoryUIController;

    [Header("Shop UI")]
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private Button closeButton;
    [SerializeField] private Transform buttonSlotGrid;
    [SerializeField] private Button buttonPrefab;
    [SerializeField] private Transform tabSlotGrid;
    [SerializeField] private TMP_Text statusText;

    [Header("Purchase UI")]
    [SerializeField] private GameObject purchasePanel;
    [SerializeField] private Button gainItemPanel;
    [SerializeField, Min(0f)] private float fadeDuration = 0.25f;
    [SerializeField, Range(0f, 1f)] private float purchaseShopAlpha = 0.5f;
    [SerializeField, Range(0f, 1f)] private float normalShopAlpha = 1f;

    private CanvasGroup shopGroup;
    private CanvasGroup purchaseGroup;
    private CanvasGroup gainGroup;
    private Button purchaseCloseButton;
    private Button purchaseButton;
    private Button decreaseButton;
    private Button increaseButton;
    private GameObject counterPanel;
    private SellingObjectSO selectedOffer;
    private int purchase_quantity = 1;
    private int ownedCurrency;
    private bool gainItemVisible;
    public int PurchaseQuantity => purchase_quantity;

    private readonly List<GameObject> offerButtons = new List<GameObject>();
    private readonly List<SellingObjectSO> offers = new List<SellingObjectSO>();
    private readonly List<Button> tabButtons = new List<Button>();
    private readonly List<UnityAction> tabActions = new List<UnityAction>();
    private StateManager stateManager;
    private bool initialized;

    private void Awake()
    {
        Initialize();
        ApplyState(stateManager.CurrentState);
    }

    private void Initialize()
    {
        if (initialized)
            return;
        initialized = true;
        ResolveInventory();
        shopGroup = EnsureGroup(shopPanel);
        purchaseGroup = EnsureGroup(purchasePanel);
        gainGroup = EnsureGroup(gainItemPanel != null ? gainItemPanel.gameObject : null);
        if (purchasePanel != null)
        {
            purchaseCloseButton = FindComponent<Button>(purchasePanel.transform, "PurchaseCloseButton");
            purchaseButton = FindComponent<Button>(purchasePanel.transform, "PurchaseButton");
            decreaseButton = FindComponent<Button>(purchasePanel.transform, "CounterPanel/DecreaseButton");
            increaseButton = FindComponent<Button>(purchasePanel.transform, "CounterPanel/IncreaseButton");
            Transform counter = purchasePanel.transform.Find("CounterPanel");
            counterPanel = counter != null ? counter.gameObject : null;
        }
        purchaseCloseButton?.onClick.AddListener(CancelPurchase);
        purchaseButton?.onClick.AddListener(ConfirmPurchase);
        decreaseButton?.onClick.AddListener(DecreaseQuantity);
        increaseButton?.onClick.AddListener(IncreaseQuantity);
        gainItemPanel?.onClick.AddListener(CloseGainItem);
        purchasePanel?.SetActive(false);
        if (gainItemPanel != null)
            gainItemPanel.gameObject.SetActive(false);
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
        if (buttonSlotGrid != null && buttonPrefab != null && SellingObjects != null)
        {
            foreach (SellingObjectSO offer in SellingObjects)
            {
                if (offer == null)
                    continue;
                Button button = Instantiate(buttonPrefab, buttonSlotGrid, false);
                if (offer.objectIcon == null && offer.@object?.item != null && offer.@object.item.icon != null)
                    SetImage(button.transform, "ItemFrame/ItemImage", offer.@object.item.icon);
                else
                    SetImage(button.transform, "ItemFrame/ItemImage", offer.objectIcon);
                SetText(button.transform, "ItemText", offer.objectName);
                SetText(button.transform, "ItemFrame/ItemQuantity", $"\u00d7 {offer.@object?.count ?? 1}");
                if (offer.currencyIcon == null && offer.currency != null && offer.currency.icon != null)
                    SetImage(button.transform, "PricePanel/PriceFrame/PriceImage", offer.currency.icon);
                else
                    SetImage(button.transform, "PricePanel/PriceFrame/PriceImage", offer.currencyIcon);
                SetText(button.transform, "PricePanel/PriceText", offer.price.ToString());
                button.onClick.AddListener(() => Buy(offer));
                button.gameObject.SetActive(true);
                offerButtons.Add(button.gameObject);
                offers.Add(offer);
            }
        }
        if (tabSlotGrid != null)
        {
            Button[] buttons = tabSlotGrid.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                int tab = i;
                UnityAction action = () => SelectTab(tab);
                buttons[i].onClick.AddListener(action);
                tabButtons.Add(buttons[i]);
                tabActions.Add(action);
            }
        }
        // Keep listening while the shop panel is inactive, including when this
        // controller is attached to the panel itself.
        stateManager = StateManager.Instance;
        stateManager.StateChanged += ApplyState;
    }

    private void OnEnable()
    {
        Initialize();
        SelectTab(0);
        ApplyState(stateManager.CurrentState);
    }

    private void OnDisable()
    {
        if (stateManager != null)
            stateManager.CloseState(GameState.Merchant);
    }

    private void OnDestroy()
    {
        if (stateManager != null)
            stateManager.StateChanged -= ApplyState;
        if (closeButton != null)
            closeButton.onClick.RemoveListener(Close);
        purchaseCloseButton?.onClick.RemoveListener(CancelPurchase);
        purchaseButton?.onClick.RemoveListener(ConfirmPurchase);
        decreaseButton?.onClick.RemoveListener(DecreaseQuantity);
        increaseButton?.onClick.RemoveListener(IncreaseQuantity);
        gainItemPanel?.onClick.RemoveListener(CloseGainItem);
        for (int i = 0; i < tabButtons.Count; i++)
            if (tabButtons[i] != null)
                tabButtons[i].onClick.RemoveListener(tabActions[i]);
        foreach (GameObject button in offerButtons)
            if (button != null)
                Destroy(button);
    }

    private void ResolveInventory()
    {
        if (inventoryUIController == null)
            inventoryUIController = FindFirstObjectByType<InventoryUIController>(FindObjectsInactive.Include);
        if (inventoryData == null && inventoryUIController != null)
            inventoryData = inventoryUIController.InventoryData;
    }

    public bool Open(Player player = null)
    {
        Initialize();
        if (shopPanel == null || StateManager.Instance.CurrentState != GameState.Playing)
            return false;
        if (player != null)
            playerData = player.Data;
        if (statusText != null)
            statusText.text = string.Empty;
        Interactable.ClearSelection();
        return StateManager.Instance.RequestState(GameState.Merchant);
    }

    public void Close()
    {
        StateManager.Instance.CloseState(GameState.Merchant);
    }

    private void ApplyState(GameState state)
    {
        bool purchasing = state == GameState.Purchase;
        bool shopping = state == GameState.Merchant || purchasing;
        if (state == GameState.Playing)
        {
            gainItemVisible = false;
            selectedOffer = null;
        }
        if (shopGroup != null)
        {
            shopGroup.alpha = purchasing ? purchaseShopAlpha : normalShopAlpha;
            shopGroup.interactable = state == GameState.Merchant && !gainItemVisible;
            shopGroup.blocksRaycasts = shopGroup.interactable;
        }
        if (shopPanel != null)
            shopPanel.SetActive(shopping);
        SetOverlayVisible(purchasePanel, purchaseGroup, purchasing);
        if (gainItemPanel != null)
            SetOverlayVisible(gainItemPanel.gameObject, gainGroup, state == GameState.Merchant && gainItemVisible);
    }

    public void SelectTab(int tab)
    {
        for (int i = 0; i < offers.Count; i++)
            offerButtons[i].SetActive(tab == 0 || offers[i].type == tab);
    }

    public void Buy(SellingObjectSO offer)
    {
        if (offer == null || purchasePanel == null || gainItemPanel == null || gainItemVisible ||
            StateManager.Instance.CurrentState != GameState.Merchant)
            return;
        ResolveInventory();
        selectedOffer = offer;
        purchase_quantity = 1;
        ownedCurrency = offer.currency != null
            ? (inventoryData != null ? inventoryData.GetItemCount(offer.currency) : 0)
            : (playerData != null ? playerData.money : 0);
        Transform root = purchasePanel.transform;
        SetImage(root, "ItemFrame/ItemImage", offer.objectIcon != null ? offer.objectIcon : offer.@object?.item?.icon);
        SetText(root, "ItemText", offer.objectName);
        SetText(root, "DescriptionText", offer.description);
        Sprite currencyIcon = offer.currencyIcon != null ? offer.currencyIcon : offer.currency?.icon;
        SetImage(root, "PlayerOwnPanel/PriceFrame/PriceImage", currencyIcon);
        SetImage(root, "PurchaseButton/PriceFrame/PriceImage", currencyIcon);
        SetText(root, "PlayerOwnPanel/PriceFrame/PriceText", ownedCurrency.ToString());
        counterPanel?.SetActive(!offer.buyOneAtOnce);
        if (statusText != null)
            statusText.text = string.Empty;
        UpdatePurchaseUI();
        StateManager.Instance.RequestState(GameState.Purchase);
    }

    public void IncreaseQuantity() => ChangeQuantity(1);
    public void DecreaseQuantity() => ChangeQuantity(-1);

    private void ChangeQuantity(int change)
    {
        if (StateManager.Instance.CurrentState != GameState.Purchase || selectedOffer == null || selectedOffer.buyOneAtOnce)
            return;
        purchase_quantity = (int)System.Math.Max(1L, System.Math.Min(int.MaxValue, (long)purchase_quantity + change));
        UpdatePurchaseUI();
    }

    private void UpdatePurchaseUI()
    {
        Transform root = purchasePanel.transform;
        long itemCount = (long)(selectedOffer.@object?.count ?? 1) * purchase_quantity;
        SetText(root, "ItemFrame/ItemQuantity", $"\u00d7 {itemCount}");
        SetText(root, "CounterPanel/QuantityText", purchase_quantity.ToString());
        long cost = (long)selectedOffer.price * purchase_quantity;
        TMP_Text price = FindComponent<TMP_Text>(root, "PurchaseButton/PriceFrame/PriceText");
        if (price != null)
        {
            price.text = cost.ToString();
            price.color = selectedOffer.price < 0 || cost > ownedCurrency ? Color.red : Color.black;
        }
        if (decreaseButton != null)
            decreaseButton.interactable = purchase_quantity > 1;
        if (increaseButton != null)
            increaseButton.interactable = purchase_quantity < int.MaxValue;
    }

    public void CancelPurchase()
    {
        StateManager.Instance.CloseState(GameState.Purchase);
    }

    public void ConfirmPurchase()
    {
        if (StateManager.Instance.CurrentState != GameState.Purchase || selectedOffer == null)
            return;
        if (!TryBuy(selectedOffer, purchase_quantity))
        {
            if (statusText != null)
                statusText.text = "Purchase unavailable. Check your currency and inventory space.";
            return;
        }
        gainItemVisible = true;
        StateManager.Instance.CloseState(GameState.Purchase);
    }

    public void CloseGainItem()
    {
        gainItemVisible = false;
        ApplyState(StateManager.Instance.CurrentState);
    }

    private void Update()
    {
        Fade(purchaseGroup);
        Fade(gainGroup);
        if (StateManager.Instance.CurrentState != GameState.Purchase || Keyboard.current == null)
            return;
        Keyboard keyboard = Keyboard.current;
        if (keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame)
            IncreaseQuantity();
        if (keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame)
            DecreaseQuantity();
        if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
            ConfirmPurchase();
    }

    private void Fade(CanvasGroup group)
    {
        if (group != null && group.gameObject.activeInHierarchy)
            group.alpha = fadeDuration <= 0f ? 1f : Mathf.MoveTowards(group.alpha, 1f, Time.unscaledDeltaTime / fadeDuration);
    }

    private void SetOverlayVisible(GameObject panel, CanvasGroup group, bool visible)
    {
        if (panel == null)
            return;
        if (visible && !panel.activeSelf && group != null)
            group.alpha = fadeDuration <= 0f ? 1f : 0f;
        panel.SetActive(visible);
    }

    private static CanvasGroup EnsureGroup(GameObject panel)
    {
        if (panel == null)
            return null;
        CanvasGroup group = panel.GetComponent<CanvasGroup>();
        return group != null ? group : panel.AddComponent<CanvasGroup>();
    }

    private static T FindComponent<T>(Transform root, string path) where T : Component
    {
        Transform child = root.Find(path);
        return child != null ? child.GetComponent<T>() : null;
    }

    public bool TryBuy(SellingObjectSO offer, int quantity = 1)
    {
        if ((StateManager.Instance.CurrentState != GameState.Merchant && StateManager.Instance.CurrentState != GameState.Purchase) || offer == null ||
            quantity <= 0 || offer.price < 0 || offer.randomItemGroupID != 0)
            return false;
        if (offer.buyOneAtOnce)
            quantity = 1;
        long totalPrice = (long)offer.price * quantity;
        long totalCount = (long)(offer.@object?.count ?? 1) * quantity;
        int ammoCount = offer.@object?.ammoCount ?? 0;
        if (totalPrice > int.MaxValue || totalCount <= 0 || totalCount > int.MaxValue || ammoCount < 0)
            return false;
        ResolveInventory();
        if (inventoryData == null || (offer.currency == null &&
            (playerData == null || playerData.money < totalPrice)))
            return false;

        if (!inventoryData.TryExchangeItems(offer.currency, offer.currency != null ? (int)totalPrice : 0,
            offer.@object?.item, (int)totalCount, ammoCount))
            return false;
        if (offer.currency == null && totalPrice > 0)
            playerData.AddMoney(-(int)totalPrice);
        inventoryUIController?.RefreshUI();
        return true;
    }

    private static void SetImage(Transform root, string path, Sprite sprite)
    {
        Transform child = root.Find(path);
        Image image = child != null ? child.GetComponent<Image>() : null;
        if (image != null)
            image.sprite = sprite;
    }

    private static void SetText(Transform root, string path, string text)
    {
        Transform child = root.Find(path);
        TMP_Text label = child != null ? child.GetComponent<TMP_Text>() : null;
        if (label != null)
            label.text = text;
    }
}
