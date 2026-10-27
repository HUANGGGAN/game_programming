# EffectManager

## Goal

Handle effect icon render.
Move the effect logic code from EnemySO.cs to new script(create a new one)
Create a new script to handle effect icons
Make configurable effect icon.

## Detail


### Effect Slot Grid
I'll create a slot grid(empty initially) and icon prefabs
```
EffectIconSlotGrid
```
For each effect player holds, make corresponding icon visible inside.
Since only player need to care about the icons while normal enemies won't, you may write another script for player.
But some entities(like bosses) may need this feature later, don't hard code in Player.cs

### Effect Icon Register

Make a cofigurable list holds
[{int ID, sprite:effect_icon}, ...]
to let EffectManager know which icon to shows up when

### Effect Icon Prefab

I will make a prefab
```
EffectIconFrame
EffectIconFrame/EffectIconImage -> set to the registered effect_icon
```
Effect Manager should place the prefab under Effect Slot Grid

### Remove Effect Interface

There is only Apply() interface now, implement Remove interface that removes the effect and disable the icon. As effect can be removed by many ways(Tick, by Potion, ...)