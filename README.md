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
- Melee: MeleeSO
- Wand: WandSO
- Consumable: ConsumableSO
