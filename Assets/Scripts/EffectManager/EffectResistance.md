In EffectRuntime.cs, add resistance for each ID, for simple implementation, we will use negative ID

We will use ID:1(Fire) as example, ID:-1 will be Fire Resistance

For example ID:-1 will block ID:1 with same or lower level
Applying ID:-1 will clear ID:1 with same or lower level

If ID:1(Fire) has higher level than ID:-1, they can coexist

# PlayerConsumable.cs

Let ID:4 be ID:-1 30 sec, ID:5 be ID:-5 30 sec