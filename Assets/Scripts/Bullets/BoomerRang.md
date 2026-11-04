# Goal

In ArcBullet.cs, add another bullet mode in it.

## Detail

ArcBullet.cs(bullet.id == 1) now handles bullet with bullet.mode == 0, add a new one with bullet.mode == 1

Mode 1:

The bullet has constant speed, but with an arc trail.
When launch, the bullet will follow an arc to the point where player click, then fly back to player(an arc also)

Notice that player can move during the bullet flying, the bullet should update its return path.

Also, make the bullet keep spinning during flying.

Make those parameters configurable