## Goal

Implement boss events

- Boss.cs, the boss logic
- BossManager.cs: handle triggering boss awake and die.

## Details

### BossManager.cs

When moving camera, move it smoothly.
camera follows player by default.

There is a SealDoor object.

- Sleeping State: When player is not in the range (x<-12, y>9), nothing happens
- Active State: When player is in the range (x<-12, y>9), move cinemachine camera to cover the rectangle (-28,27) (-11,10) with a fixed view. Set Boss and SealDoor to active.
- Died State: After boss Die(), set SealDoor to inactive.

### Boss.cs

Implement a simple enemy first, you can refer to Enemy.cs