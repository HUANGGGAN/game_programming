## Target Files

- WaveManager.cs
- IEnemySpawner.cs
- EnemySO.cs
- Enemy.cs
- EnemyPoolSpawner.cs

## Goal

Implement a wave manager with difficulty setting.

## Details

EnemyPoolSpawner.cs handles enemy pool and registration, WaveManager.cs request enemy spawn.

The request contains
- center: normally the player
- min_radius/max_radius: the distance between enemy spawn point and center
- total_point: Total enemies to spawn, notice each type of enemy has different point.(4 points can summon 2 2-point enemies)
- current_difficulty: to let Enemy pool decide which enemy to spawn.

### EnemyPoolSpawner.cs

Enemy Registration, each type of enemy will have a configurable EnemyPoint and EnemyDifficulty

- float enemy_point
- float enemy_difficulty
(Remember to update EnemySO.cs and Enemy.cs)

when WaveManager.cs request to spawn enemy, randomly summon enemies with enemy_difficulty <= current_difficulty
and the sum of enemies' enemy_point <= total_point.

e.g. EnemyA:{point:1, difficulty:1}, EnemyB:{point:2, difficulty:2}
request{total_point:3, current_difficulty:1.5} will summon 3 EnemyA
request{total_point:5, current_difficulty:3} will summon (5 EnemyA, or 1 EnemyB 3 EnemyA, or 2EnemyB or 1EnemyA)

When spawning, check navmesh, ensure the center(player) is reachable from the spawnpoint

### WaveManager.cs

Set base difficulty to 1, the difficulty will increase as time pass. At 5 minite end, the difficulty should be 10 and reach maximum.

- float baseDifficulty = 1;
- float spawnPeriod; (the period of wave manager request spawning)

