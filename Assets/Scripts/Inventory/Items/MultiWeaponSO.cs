using UnityEngine;

[CreateAssetMenu(fileName = "NewMultiWeapon", menuName = "Weapon/Multi Weapon")]
public class MultiWeaponSO : WeaponSO
{
    public WeaponSO[] weapons;
}
