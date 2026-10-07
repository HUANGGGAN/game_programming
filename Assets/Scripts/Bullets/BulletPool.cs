using System.Collections.Generic;
using UnityEngine;

public class BulletPool : MonoBehaviour
{
    public static BulletPool Instance { get; private set; }

    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private int poolSize = 10;

    private Dictionary<string, List<Bullet2D>> bulletPools = new Dictionary<string, List<Bullet2D>>();

    private void Awake()
    {
        Instance = this;
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
        if (bulletPrefab == null)
        {
            Debug.LogError("Bullet prefab is not assigned on BulletPool.", this);
            return null;
        }

        GameObject obj = Instantiate(bulletPrefab, transform);
        Bullet2D bullet = obj.GetComponent<Bullet2D>();
        if (bullet == null)
        {
            Debug.LogError("Bullet prefab must have a Bullet2D component.", obj);
            Destroy(obj);
            return null;
        }

        bullet.Initialize(bulletData);
        bullet.Deactivate();
        return bullet;
    }
}