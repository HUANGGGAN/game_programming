# Goal

- Handle player hit
- Handle player die
- Add Home region effect

# Detail

## Hit

I will provide a player flash red image in canvas
When player damaged, set the flash red image canvas group's alpha to 0.3, make it smooth.
When not damaged, let the image fade away to alpha=0

## Die

If player's health reaches 0, trigger died and start coroutine

- Disable attack
- Disable movement
- Set flash red image alpha to 0.5
- Set player to red
- Wait for 1 second

After 1 second, respawn the player to home position(I will give you the position)
Set health to maxHealth, MP to max MP
Lose 20% of money

## Home region

When player enter home region.(only trigger when enter)
Disable RegionEnemySpawner. Set HP and MP to full.

Once player leave home region, enable RegionEnemySpawner.