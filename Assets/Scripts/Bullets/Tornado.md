# Goal

Write a Tornado.cs that pulls Enemy.cs to the center

Confugurable variables
- radius: 5
- duration: 4
- force: 2.5
- damage: 5
- damage_interval: 0.5

During the `duration`, pull all the enemies in the `radius` with `force` and give `damage` every `damage_interval` seconds.

For WandSO attackMode == 2

public bool CanHandle(WeaponSO weapon) => weapon is WandSO wand && wand.attackMode == 2;

Summon a tornado where player clicked.
I will provide 2 images, switch the tornado's icon between them every `damage_interval`