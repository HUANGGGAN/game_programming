using UnityEngine;

[CreateAssetMenu(fileName = "RandomBullet", menuName = "Weapon/Random Bullet Data")]
public class RandomBulletSO : BulletSO
{
    public BulletSO[] bullets = System.Array.Empty<BulletSO>();
    [Tooltip("Positive relative weights, matched to bullets by index. For example: 70 and 30 give 70% and 30%.")]
    public float[] prob = System.Array.Empty<float>();

    private bool IsValidEntry(int index) => bullets[index] != null && bullets[index] is not RandomBulletSO &&
        prob[index] > 0f && !float.IsNaN(prob[index]) && !float.IsInfinity(prob[index]);

    private double TotalWeight()
    {
        double total = 0;
        if (bullets == null || prob == null)
            return total;
        for (int i = 0; i < System.Math.Min(bullets.Length, prob.Length); i++)
            if (IsValidEntry(i))
                total += prob[i];
        return total;
    }

    public bool HasValidBullets => TotalWeight() > 0;

    public bool TryPickBullet(out BulletSO bullet)
    {
        bullet = null;
        double total = TotalWeight();
        if (total <= 0)
            return false;
        double roll = Random.value * total;
        for (int i = 0; i < System.Math.Min(bullets.Length, prob.Length); i++)
        {
            if (!IsValidEntry(i))
                continue;
            bullet = bullets[i];
            if (roll < prob[i])
                return true;
            roll -= prob[i];
        }
        // Random.value can equal 1; use the last eligible bullet at that endpoint.
        return bullet != null;
    }
}
