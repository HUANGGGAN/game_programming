# game_programming

### Enemy.cs

- Enemy entity, movement, HP, current effection.
- Read EnemySO.cs for attribute

### AttackSource

- BulletSO
- MeleeSO
- EnemySO

### Effection

Defined in EnemySO.cs

- 0: Fire
- 1: Faint
- 2: Bleed

### Attack Type(attackAttribute)

- 0: None
- 1: Fire

### Resistance

- 0: Fire

### Item

- Universal: ItemSO
- Gun: GunSO, need bullet type(BulletSO)
- Charge: ChargedSO
- Melee: MeleeSO
- Wand: WandSO
- Consumable: ConsumableSO

### Wand type
- 0: normal

### Bullet ID, Mode
- 0, 0: normal
- 0, 1: explode on clicked position, direct
- 1, 0: explode on clicked position, parabolic
