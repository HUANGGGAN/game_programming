# Enemy Movement Refine

## Goal

- Refine the knockback system

## Details

Currently we apply a velocity on enemy when knockback, change it into

- a knockback force, enemies should have a rigidbody2d, and receive force when hit.
- the movement toward player(agent) should stop during knockback period(make the period configurable)
- reset coroutine, after the knockback period, force reset the enemy's velocity to 0(or the moving speed toward player)