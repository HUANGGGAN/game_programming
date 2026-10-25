# Goal

Distinguish how enemy dies.

## Detail

Currently enemy could die(Die()) from 2 reason

- Take Damage
- Despawn if too far

Do the changes

### Despawn

No need to do dying process, just need to clean up some coroutine or resources.
Write a clean Despawn() for this instead of calling Die()

### Take Damage

You can always assume the source is from player(whether bullet, melee, or effect damage)
This will trigger Die()

#### Direct Damage

If enemy take damage directly
- Bullet
- Explosion
- Melee
Do the flash red coroutine normally

#### Effect Damage

If enemy take damage from effect(fire, bleed), don't do the flash red coroutine(Reminder, it may be other)

### Die()
If enemy Die()(not despawn)
Add int money = 1; field in EnemySO, and add money to player `AddMoney(int amount)`(In PlayerData)