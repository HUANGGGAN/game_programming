# Goal

Update EffectRuntime.md

- Set Tick() to operate 2 times per second
- Effect level raise if same effect applied


## Detail

### Tick speed

Every effectable object call Tick() on each Update(), find a way to make Tick() run exact effect and countdown every 0.5 sec

### Count

Currently if target been applied a same effect, we do
- Check level, a higher level override lower one
- Time override if applied time is longer

Now change it to
- For each level, handle the countdown separately(so target can have same effect but different level at the same time)
- Only run the effect with highest level
- If applied same effect with same level, increase the effect's level by 1 with same remaining time.

For example:

Case 1. Target can have Fire(lv1, 10s) and Fire(lv2, 5s) at the same time, only Fire(lv2) will do damage on the target. After 5 second, only Fire(lv1, 5s) left

Case 2. Target have Fire(lv2, 7s) and is about to be applied with Fire(lv2, 10s), same effect with same level, turn it into Fire(lv(2+1), max(7,10)s)

Case 3. Target have Fire(lv1, 3s) and Fire(lv2, 7s) and is about to be applied with Fire(lv1, 10s), first, 2 Fire(lv1) become Fire(lv2, 10s). Then, 2 Fire(lv2) merges and become Fire(lv3, 10s)