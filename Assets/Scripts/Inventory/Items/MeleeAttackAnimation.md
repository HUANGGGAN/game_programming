# Goal

To determine the animation for melee attack

## States

Idle
SwingAttack -> animator.SetTrigger("Swing");
PokeAttack -> animator.SetTrigger("Poke");

we will add more later

## Details

In MeleeSO.cs, add a field to decide which attack state to enter when performing attack
In PlayerShooter.cs, resolve the attack mode to play animation