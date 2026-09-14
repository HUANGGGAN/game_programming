using System.Collections.Generic;
using UnityEngine;

public class BulletPool : MonoBehaviour
{
    public static BulletPool Instance { get; private set; }

    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private int poolSize = 10;

    private List<Bullet2D> bulletPool = new List<Bullet2D>();

    private void Awake()
    {
        Instance = this;

        // Populate initial pool
        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(bulletPrefab, transform);
            Bullet2D bullet = obj.GetComponent<Bullet2D>();
            bullet.Deactivate();
            bulletPool.Add(bullet);
        }
    }

    public Bullet2D GetBullet()
    {
        // Find an inactive bullet to reuse
        foreach (Bullet2D bullet in bulletPool)
        {
            if (!bullet.gameObject.activeInHierarchy)
            {
                return bullet;
            }
        }

        // Expand pool if empty
        GameObject obj = Instantiate(bulletPrefab, transform);
        Bullet2D newBullet = obj.GetComponent<Bullet2D>();
        newBullet.Deactivate();
        bulletPool.Add(newBullet);
        return newBullet;
    }
}