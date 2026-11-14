# Goal

Make a script for meteorite, registered in pool, with initial size = 0
Separate Explode logic from Bullet.cs

# Detail

I will create a asset from WandSO with attackMode 1, it should summon 1 meteorite fall on clicked position, and 4 other meteorite on random places

WandSO can only perform attackMode 0 now, attackMode 1 is for meteorite
(SO WandSO can have no BulletSO, check in PlayerShooter.cs)

## Meteorite.cs

Don't need collider, they only need to perform animation and trigger the explosion at the end of animation

Need Animation controller, I will create the animation clip by myself.

When summon, appear at the upper left of the target from the edge of screen. Then move toward the target(45 degree)


### Animation clip

When reach target, switch to ExplosionStage(animator.SetTrigger("Explode");), then set explosion at that place.
Return to the pool after explosion animation.

## Explosion

Separate the explosion logic from Bullet.cs to new script, as many object later need to request explosion.