using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Pooled world-space enemy health bars, damage numbers, and cosmetic coins.</summary>
public class EnemyFeedbackPool : MonoBehaviour
{
    public static EnemyFeedbackPool Instance { get; private set; }
    private static readonly HashSet<EnemyChase> enemies = new HashSet<EnemyChase>();
    [SerializeField] private Canvas worldCanvas;
    [SerializeField] private GameObject healthBarPrefab;
    [SerializeField] private TMP_Text damageNumberPrefab;
    [SerializeField] private Image coinPrefab;
    [SerializeField] private Transform coinDestination;
    [SerializeField, Min(0)] private int initialHealthBars = 30;
    [SerializeField, Min(0)] private int initialDamageNumbers = 30;
    [SerializeField, Min(0)] private int initialCoins = 30;
    [SerializeField] private Vector3 healthBarOffset = new Vector3(0f, 0.8f, 0f);
    [SerializeField] private Vector3 damageNumberOffset = new Vector3(0f, 0.5f, 0f);
    [SerializeField, Min(0.01f)] private float damageNumberDuration = 0.8f;
    [SerializeField, Min(0.01f)] private float coinScatterDuration = 0.8f;
    [SerializeField, Min(0.01f)] private float coinFlightDuration = 0.6f;
    [SerializeField, Min(1)] private int maxCoinsPerDeath = 8;
    [SerializeField] private float jumpSpeed = 2.5f;
    [SerializeField] private float gravity = 8f;
    [SerializeField] private float scatterSpeed = 1.5f;

    private class Bar { public GameObject root; public Image fill; }
    private class Popup
    {
        public TMP_Text text;
        public Vector3 start;
        public Vector3 velocity;
        public Color color;
        public float elapsed;
    }
    private class Coin
    {
        public Image image;
        public Vector3 start;
        public Vector3 velocity;
        public float elapsed;
        public bool flying;
    }
    private readonly Queue<Bar> freeBars = new Queue<Bar>();
    private readonly Queue<TMP_Text> freeNumbers = new Queue<TMP_Text>();
    private readonly Queue<Image> freeCoins = new Queue<Image>();
    private readonly Dictionary<EnemyChase, Bar> bars = new Dictionary<EnemyChase, Bar>();
    private readonly List<Popup> numbers = new List<Popup>();
    private readonly List<Coin> coins = new List<Coin>();
    private Transform PoolRoot => worldCanvas != null ? worldCanvas.transform : transform;

    private void Awake()
    {
        Instance = this;
        if (worldCanvas == null) worldCanvas = GetComponentInParent<Canvas>();
        for (int i = 0; i < initialHealthBars && healthBarPrefab != null; i++) freeBars.Enqueue(CreateBar());
        for (int i = 0; i < initialDamageNumbers && damageNumberPrefab != null; i++) freeNumbers.Enqueue(CreateNumber());
        for (int i = 0; i < initialCoins && coinPrefab != null; i++) freeCoins.Enqueue(CreateCoin());
    }

    private Bar CreateBar()
    {
        GameObject root = Instantiate(healthBarPrefab, PoolRoot);
        Transform fillTransform = root.transform.Find("EnemyHealthBarBackground/EnemyHealthBarFill");
        Image fill = fillTransform != null ? fillTransform.GetComponent<Image>() : null;
        if (fill == null)
            foreach (Image image in root.GetComponentsInChildren<Image>(true))
                if (image.name == "EnemyHealthBarFill") { fill = image; break; }
        if (fill == null) Debug.LogError("Health bar prefab needs an EnemyHealthBarFill Image.", root);
        else
        {
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
        }
        foreach (Graphic graphic in root.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
        root.SetActive(false);
        return new Bar { root = root, fill = fill };
    }

    private TMP_Text CreateNumber()
    {
        TMP_Text text = Instantiate(damageNumberPrefab, PoolRoot);
        text.raycastTarget = false;
        text.gameObject.SetActive(false);
        return text;
    }

    private Image CreateCoin()
    {
        Image image = Instantiate(coinPrefab, PoolRoot);
        image.raycastTarget = false;
        image.gameObject.SetActive(false);
        return image;
    }

    public static void Register(EnemyChase enemy)
    {
        enemies.Add(enemy);
        if (Instance != null && Instance.isActiveAndEnabled) Instance.UpdateBar(enemy);
    }
    public static void Unregister(EnemyChase enemy)
    {
        enemies.Remove(enemy);
        // Use Unity's null check: a managed reference can outlive its native object.
        EnemyFeedbackPool pool = Instance;
        if (pool != null && pool.isActiveAndEnabled) pool.ReleaseBar(enemy);
    }

    public static void Damaged(EnemyChase enemy, float amount)
    {
        if (Instance == null || !Instance.isActiveAndEnabled) return;
        Instance.UpdateBar(enemy);
        Instance.ShowDamage(enemy.transform.position, amount);
    }

    public static void Died(EnemyChase enemy, int money)
    {
        if (Instance != null && Instance.isActiveAndEnabled) Instance.ShowCoins(enemy.transform.position, money);
    }

    private void ReleaseBar(EnemyChase enemy)
    {
        if (!bars.TryGetValue(enemy, out Bar bar)) return;
        bars.Remove(enemy);
        if (bar.root == null) return;
        bar.root.SetActive(false);
        freeBars.Enqueue(bar);
    }

    private void UpdateBar(EnemyChase enemy)
    {
        if (!bars.TryGetValue(enemy, out Bar bar))
        {
            if (healthBarPrefab == null) return;
            bar = freeBars.Count > 0 ? freeBars.Dequeue() : CreateBar();
            bars.Add(enemy, bar);
            bar.root.SetActive(true);
        }
        bar.root.transform.position = enemy.transform.position + healthBarOffset;
        if (bar.fill != null) bar.fill.fillAmount = enemy.MaxHealth > 0f ? Mathf.Clamp01(enemy.CurrentHealth / enemy.MaxHealth) : 0f;
    }

    private void ShowDamage(Vector3 position, float amount)
    {
        if (damageNumberPrefab == null) return;
        TMP_Text text = freeNumbers.Count > 0 ? freeNumbers.Dequeue() : CreateNumber();
        text.text = amount.ToString("0.#");
        text.transform.position = position + damageNumberOffset;
        text.gameObject.SetActive(true);
        Debug.Log($"Showing damage: {amount}");
        numbers.Add(new Popup { text = text, start = text.transform.position, color = text.color,
            velocity = new Vector3(Random.Range(-scatterSpeed, scatterSpeed), jumpSpeed, 0f) });
    }

    private void ShowCoins(Vector3 position, int amount)
    {
        if (coinPrefab == null || coinDestination == null || amount <= 0) return;
        int count = Mathf.Min(amount, Mathf.Max(1, maxCoinsPerDeath));
        for (int i = 0; i < count; i++)
        {
            Image image = freeCoins.Count > 0 ? freeCoins.Dequeue() : CreateCoin();
            image.transform.position = position;
            image.gameObject.SetActive(true);
            coins.Add(new Coin { image = image, start = position,
                velocity = new Vector3(Random.Range(-scatterSpeed, scatterSpeed), jumpSpeed * Random.Range(0.8f, 1.2f), 0f) });
        }
    }

    private Vector3 DestinationPosition()
    {
        Canvas destinationCanvas = coinDestination.GetComponentInParent<Canvas>();
        // Convert HUD targets from their canvas into the world-space canvas plane.
        if (destinationCanvas != null && destinationCanvas.renderMode != RenderMode.WorldSpace && worldCanvas != null)
        {
            Camera sourceCamera = destinationCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : destinationCanvas.worldCamera;
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(sourceCamera, coinDestination.position);
            Camera camera = worldCanvas.worldCamera != null ? worldCanvas.worldCamera : Camera.main;
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle((RectTransform)worldCanvas.transform, screen, camera, out Vector3 point))
                return point;
        }
        return coinDestination.position;
    }

    private void LateUpdate()
    {
        foreach (EnemyChase enemy in enemies)
            if (enemy != null && enemy.isActiveAndEnabled) UpdateBar(enemy);
        float dt = Time.deltaTime;
        for (int i = numbers.Count - 1; i >= 0; i--)
        {
            Popup popup = numbers[i];
            popup.elapsed += dt;
            float t = popup.elapsed;
            popup.text.transform.position = popup.start + popup.velocity * t + Vector3.down * (0.5f * gravity * t * t);
            Color color = popup.color;
            color.a *= 1f - Mathf.Clamp01(t / Mathf.Max(0.01f, damageNumberDuration));
            popup.text.color = color;
            if (t < Mathf.Max(0.01f, damageNumberDuration)) continue;
            popup.text.color = popup.color;
            popup.text.gameObject.SetActive(false);
            freeNumbers.Enqueue(popup.text);
            numbers.RemoveAt(i);
        }
        for (int i = coins.Count - 1; i >= 0; i--)
        {
            Coin coin = coins[i];
            coin.elapsed += dt;
            if (!coin.flying)
            {
                float t = Mathf.Min(coin.elapsed, Mathf.Max(0.01f, coinScatterDuration));
                Vector3 displacement = coin.velocity * t + Vector3.down * (0.5f * gravity * t * t);
                displacement.y = Mathf.Max(0f, displacement.y);
                coin.image.transform.position = coin.start + displacement;
                if (coin.elapsed < Mathf.Max(0.01f, coinScatterDuration)) continue;
                coin.flying = true;
                coin.start = coin.image.transform.position;
                coin.elapsed = 0f;
            }
            float progress = Mathf.Clamp01(coin.elapsed / Mathf.Max(0.01f, coinFlightDuration));
            if (coinDestination != null) coin.image.transform.position = Vector3.Lerp(coin.start, DestinationPosition(), Mathf.SmoothStep(0f, 1f, progress));
            if (progress < 1f && coinDestination != null) continue;
            coin.image.gameObject.SetActive(false);
            freeCoins.Enqueue(coin.image);
            coins.RemoveAt(i);
        }
    }

    private void OnDisable()
    {
        foreach (Bar bar in bars.Values)
        {
            if (bar.root == null) continue;
            bar.root.SetActive(false);
            freeBars.Enqueue(bar);
        }
        bars.Clear();
        foreach (Popup popup in numbers)
        {
            if (popup.text == null) continue;
            popup.text.color = popup.color;
            popup.text.gameObject.SetActive(false);
            freeNumbers.Enqueue(popup.text);
        }
        numbers.Clear();
        foreach (Coin coin in coins)
        {
            if (coin.image == null) continue;
            coin.image.gameObject.SetActive(false);
            freeCoins.Enqueue(coin.image);
        }
        coins.Clear();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        // Instances may live on a separate canvas, outside this object's hierarchy.
        foreach (Bar bar in freeBars) if (bar.root != null) Destroy(bar.root);
        foreach (TMP_Text text in freeNumbers) if (text != null) Destroy(text.gameObject);
        foreach (Image coin in freeCoins) if (coin != null) Destroy(coin.gameObject);
    }
}
