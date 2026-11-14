using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletPool : MonoBehaviour
{
    public static BulletPool Instance { get; private set; }

    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private GameObject arcBulletPrefab;
    [SerializeField] private Meteorite meteoritePrefab;
    [SerializeField, Min(0)] private int initialMeteoritePoolSize = 0;
    private readonly List<Meteorite> meteorites = new List<Meteorite>();
    public bool CanSummonMeteorite => meteoritePrefab != null;

    public void SummonMeteoriteBurst(Vector2 target, float scatterRadius, Player attacker = null)
    {
        SummonMeteorite(target, attacker);
        StartCoroutine(SummonExtraMeteorites(target, Mathf.Max(0f, scatterRadius), attacker));
    }

    private IEnumerator SummonExtraMeteorites(Vector2 target, float scatterRadius, Player attacker)
    {
        WaitForSeconds interval = new WaitForSeconds(0.1f);
        for (int i = 0; i < 4; i++)
        {
            yield return interval;
            SummonMeteorite(target + Random.insideUnitCircle * scatterRadius, attacker);
        }
    }

    public Meteorite SummonMeteorite(Vector2 target, Player attacker = null)
    {
        if (meteoritePrefab == null) return null;
        Meteorite meteorite = meteorites.Find(item => item != null && !item.gameObject.activeSelf);
        if (meteorite == null) meteorite = CreateMeteorite();
        meteorite.Launch(target, attacker);
        return meteorite;
    }

    private Meteorite CreateMeteorite()
    {
        Meteorite meteorite = Instantiate(meteoritePrefab, transform);
        meteorite.gameObject.SetActive(false);
        meteorites.Add(meteorite);
        return meteorite;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    [SerializeField] private int poolSize = 10;

    private Dictionary<string, List<Bullet2D>> bulletPools = new Dictionary<string, List<Bullet2D>>();

    private void Awake()
    {
        Instance = this;
        for (int i = 0; i < initialMeteoritePoolSize && meteoritePrefab != null; i++) CreateMeteorite();
    }

    public Bullet2D GetBullet(BulletSO bulletData)
    {
        if (bulletData == null)
        {
            Debug.LogError("Cannot get a bullet without BulletSO data.", this);
            return null;
        }

        string poolKey = string.IsNullOrWhiteSpace(bulletData.bulletId)
            ? bulletData.name
            : bulletData.bulletId;

        if (!bulletPools.TryGetValue(poolKey, out List<Bullet2D> bulletPool))
        {
            bulletPool = new List<Bullet2D>();
            bulletPools.Add(poolKey, bulletPool);

            for (int i = 0; i < poolSize; i++)
            {
                Bullet2D pooledBullet = CreateBullet(bulletData);
                if (pooledBullet == null) return null;
                bulletPool.Add(pooledBullet);
            }
        }

        foreach (Bullet2D bullet in bulletPool)
        {
            if (!bullet.gameObject.activeInHierarchy)
            {
                bullet.Initialize(bulletData);
                return bullet;
            }
        }

        Bullet2D newBullet = CreateBullet(bulletData);
        if (newBullet != null) bulletPool.Add(newBullet);
        return newBullet;
    }

    private Bullet2D CreateBullet(BulletSO bulletData)
    {
        bool isArc = bulletData.bulletId == "1";
        GameObject prefab = isArc ? arcBulletPrefab : bulletPrefab;
        if (prefab == null)
        {
            Debug.LogError(isArc ? "Arc bullet prefab is not assigned on BulletPool." : "Bullet prefab is not assigned on BulletPool.", this);
            return null;
        }

        GameObject obj = Instantiate(prefab, transform);
        Bullet2D bullet = obj.GetComponent<Bullet2D>();
        if (bullet == null || (isArc && bullet is not ArcBullet2D))
        {
            Debug.LogError(isArc ? "Arc bullet prefab must have an ArcBullet2D component." : "Bullet prefab must have a Bullet2D component.", obj);
            Destroy(obj);
            return null;
        }

        bullet.Initialize(bulletData);
        bullet.Deactivate();
        return bullet;
    }
}
