## Goal

Implement boss logic.

## Details

Moving State:
- Normal: Move to player just like Enemy.cs
- Charging: disable knockback, set speed to 0, disable flashRedRoutine, set color to yellow.
- Dashing: Locate player's position, move the the position with DashSpeed, don't update the target position during dashing. During dashing, set damage to DashDamage, set knockback to DashKnockback

Shield State:
- None: Can be hit by bullet.
- hasShield(1-MaxShieldCount): Can only be hit by Melee attack, each attack decrease one shield.
- get one shield every GainShieldTime

Skill:
Dash: 
1. Get into Charging state for 3 seconds
2. Done Charging, locate player's position, move to that position with DashSpeed, repeat this process 3 times.
3. Return to normal state.

## Configurable Variables

- Normal Speed
- DashSpeed
- Dash skill CD time
- DashDamage
- DashKnockback
- MaxShieldCount
- GainShieldTime

## EnemySO.cs

Boss will still attach an EnemySO.cs

## Shield

When Shield count changes, output to debug log for now, we will change it later.