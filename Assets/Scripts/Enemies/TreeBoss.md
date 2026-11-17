## Goal

Implement boss events

- TreeBoss.cs, the boss logic
- BossManager.cs: handle triggering boss awake and die.

## Details

### BossManager.cs

When moving camera, move it smoothly.
camera follows player by default.

Given
- SealDoor object
- BossTrigger object
- Boss Panel

TreeBoss is disabled before player trigger.

Once Player first enter BossTrigger object(with a collider), start boss routine
- Enable SealDoor
- Move cinemachine camera to cover from x=-150 to x=-120 with a fixed view first
- Set the boss to active. For the first 2 second(make it configurable), let the boss fade in(alpha from 0 to 1), and shake the camera.

After that, boss fight begins.

When boss is dead, start dead coroutine

- Set the boss to red and fade away, while fading away, shake the boss(not the camera)
After that
- Restore the camera
- Disable SealDoor


### TreeBoss.cs

Quite different from Enemy.cs, but we will implement details later.
You only need to implement these for now
- The Boss won't be knockback
- It take damage just like enemies, but won't stop the attack.
- Neither Melee or Ranged attack, we will do the attack module later.