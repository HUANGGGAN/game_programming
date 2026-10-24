# Bullet Particle System

## Goal

Make Bullet.cs request particles from ParticlePool.cs

## Details

- Add int ParticleID to BulletSO. 0 means no particle effect.
- In Bullet.cs, request the particle to play before disabled