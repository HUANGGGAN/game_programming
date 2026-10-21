## Goal

### Limit ammo

In WeaponSO, we consume ammo if consumeAmmo = true;

Change the logic to

if (consumeAmmo = true) && (hasLimitedAmmo = false)
    consume ammo; // decrease ammo count
else if (consumeAmmo = true) && (hasLimitedAmmo = true)
    consume item; // decrease item count instead of ammo


### Bullet Mode=0, bulletID="1"

Create a new script for bullets with bulletID=="1"

If (bulletMode==0 and bulletID=="1"), similar to (bulletMode==1 and bulletID=="0", in Bullet.cs), it should explode if isExplosive == true on the clicked target's position.
However it does not move to there directly, it moves along a parabolic-like arc.
You can see the example below.

```
while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float linearT = elapsed / duration;

            // 1. Move base position linearly from start to target (flat top-down movement)
            Vector3 currentPos = Vector3.Lerp(startPos, endPos, linearT);
            transform.position = currentPos;

            // 2. Calculate parabolic height offset using a standard parabola formula: 4 * h * t * (1 - t)
            float currentHeight = 4f * height * linearT * (1f - linearT);

            // Apply height only to the visual child sprite
            if (visualSprite != null)
            {
                visualSprite.localPosition = new Vector3(0f, currentHeight, 0f);
            }

            yield return null;
        }
```

The parent(will attach the new script) is the bullet, child is the shadow.

Anything didn't mention will be the same as Bullet.cs(bulletID=="0")

