## Target

### Bullet Mode

- Add int bulletMode; in BulletSO.cs, default 0.

- Bullets with bulletMode == 0 are same as usual.

- For bulletMode == 1, if bulletData.isExplosive is true, the bullet should explode at the position where mouse click to launch this bullet(may need to get it from PlayerShooter.cs) if it is still alive.

### Piercing

- Currently BulletSO has bool isPiercing. Change it to int type to detect how many target it can pierce through(for example, 0 will only hit the target, pierce=2 may pierce through 2 enemies and hit the third one), but the bullet still stop at obstacles.