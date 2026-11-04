# Goal

Update EffectRuntime.cs

- Add new effects
- Update old effects

# Details

Old:
- 1: Fire
- 2: Faint
- 3: Bleed

New:
- 5: Freeze
- 6: Shock
- 7: HP stealing
- 8: MP stealing

## 1: Fire
When apply fire successfully, clear out freeze effect.

## 5: Freeze

When apply freeze sucessfully, clear out fire effect.

Damage:
Same as fire and bleed

Speed:
Slow down target's speed, when holding level:x freeze, multiply speed by $\frac{2}{2+x}$
Level:4 above will stop the target totally.

## 6: Shock

Damage:
Same as fire

Speed:
multiply speed by $\frac{3}{3+\text{level}}$
Won't stop target totally

## 7,8: HP,MP stealing

This won't apply on target directly(the duration will be 0)
But when successfully apply, add HP/MP to attacker by $\text{level}*5$