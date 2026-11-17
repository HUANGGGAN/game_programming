# Goal

Spawn enemies based on
- Region
- Distance
- Difficulty

# Detail

In EnemySO.cs
- [Min(0.01f)] public float enemy_point = 1f;
- [Min(0f)] public float enemy_difficulty = 1f;

We have already implemented WaveManager:
Given fixed points and current enemy_difficulty, summon enemies.

Now we need to change a little bit, implement another script RegionEnemySpawner.cs
I will provide different regions(currently sprite renderer only, this is not same as Region.cs), the regions can overlap
- For given regions, only specified enemies can be spawn(but they can move to other region by the agent)
- We will still use enemy_point for summoning enemies
- Ignore enemy_difficulty for now.
- Enemies should spawn on navmesh surface(remember the mask)


I will provide a home position, when generating enemies, multiply the enemies' HP and attack
$1 + ||\text{player position} - \text{home position}||_2 \times \text{difficulty_coefficient}$
and round to integer

Make difficulty_coefficient configurable in RegionEnemySpawner.cs

# Spawning

Different from WaveManager.cs, total spawn point won't increase by time, instead, by distance

total point = base total point * (1 + distance*point_coefficient)

For other
[SerializeField, Min(0.01f)] private float spawnPeriod = 3f;
[SerializeField, Min(0f)] private float totalPoint = 1f;
[SerializeField, Min(0f)] private float minRadius = 10f;
[SerializeField, Min(0f)] private float maxRadius = 15f;

Keep it same.

# Region

Now the regions are just sprite renderer, find a way to decide whether a point is inside it.
No need to be precise, optimize for the performance is more important.