# BulletSO and EnemyBulletSO

- add bool preserveAngle. When shoot, don't rotate the bullet(default false)

# Region Enemy Spawner

When spawning, make sure the spawn point is between (-150, -150) and (150, 150)

# IceGround

For FrozenEnemies.cs link a object (IceGround), when FrozenEnemies.cs dies, enable IceGround

# EffectPool.cs

Instead of ID -> Image
use

ID+a list of level -> Image

# IceStatue.cs

When deactivating, show the progress bar(just like Chest.cs)

# TreeBoss.cs

Make sure the Vines is rendered under Tree Boss

# Player.cs

Player facing(Front, Back) change the logic

Last Movement is up -> Back
Last Movement is down -> Front

Last Attack is facing down(cursor's y-position relative to player) -> Front
Last Attack is facing up -> Back

Attack has higher priority

