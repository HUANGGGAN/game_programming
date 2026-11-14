# Goal

Add attack type to MeleeSO
Handle different attack type in MeleeAttack.cs

# Detail

Default: 0, just as how MeleeAttack.cs perform attack

## Type 1: dash attack

Instead of attacking a sector shape, player will dash toward the facing direction

Use attackRange to decide the dash distance.

As for the attack shape, add attackRadius to MeleeSO, and draw the circle around PlayerShooter.cs. During dash, any enemy inside the boundary will get attacked.

attackRadius will not be used by other attack type

## Type 2: spin attack

Basically type 0 with attack angle = 360, but perform many times. Add attackCount to MeleeSO for attack type 2, means how many attack to perform.

During each attack, count as different damage source, and make the weapon spin in weapon holder(spin 1 round in attackDuration)