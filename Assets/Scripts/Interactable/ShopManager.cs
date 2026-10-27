using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

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
    [SerializeField] private TMP_InputField quantityInput;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text statusText;

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
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
        if (buttonSlotGrid != null && buttonPrefab != null && SellingObjects != null)
        {
            foreach (SellingObjectSO offer in SellingObjects)
            {
                if (offer == null)
                    continue;
                Button button = Instantiate(buttonPrefab, buttonSlotGrid, false);
                if (offer.objectIcon == null && offer.@object != null && offer.@object.icon != null)
                    SetImage(button.transform, "ItemFrame/ItemImage", offer.@object.icon);
                else
                    SetImage(button.transform, "ItemFrame/ItemImage", offer.objectIcon);
                SetText(button.transform, "ItemText", offer.objectName);
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
        if (shopPanel != null)
            shopPanel.SetActive(state == GameState.Merchant);
    }

    public void SelectTab(int tab)
    {
        for (int i = 0; i < offers.Count; i++)
            offerButtons[i].SetActive(tab == 0 || offers[i].type == tab);
    }

    public void Buy(SellingObjectSO offer)
    {
        if (offer == null)
            return;
        if (descriptionText != null)
            descriptionText.text = offer.description;
        int quantity = 1;
        if (!offer.buyOneAtOnce && quantityInput != null && !string.IsNullOrWhiteSpace(quantityInput.text) &&
            (!int.TryParse(quantityInput.text, out quantity) || quantity <= 0))
        {
            if (statusText != null)
                statusText.text = "Enter a positive quantity.";
            return;
        }
        bool success = TryBuy(offer, quantity);
        if (statusText != null)
            statusText.text = success ? "Purchased." : "Purchase unavailable. Check your currency and inventory space.";
    }

    public bool TryBuy(SellingObjectSO offer, int quantity = 1)
    {
        if (StateManager.Instance.CurrentState != GameState.Merchant || offer == null ||
            quantity <= 0 || offer.price < 0 || offer.randomItemGroupID != 0)
            return false;
        if (offer.buyOneAtOnce)
            quantity = 1;
        long totalPrice = (long)offer.price * quantity;
        if (totalPrice > int.MaxValue)
            return false;
        ResolveInventory();
        if (inventoryData == null || (offer.currency == null &&
            (playerData == null || playerData.money < totalPrice)))
            return false;

        if (!inventoryData.TryExchangeItems(offer.currency, offer.currency != null ? (int)totalPrice : 0,
            offer.@object, quantity))
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
