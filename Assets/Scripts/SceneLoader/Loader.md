# Goal

Separate the objects into different scenes and dynamically load them if needed.
We will build a minimal one first.

# Detail

For testing now, we will only have 3 scenes.
But some objects may be loaded across different scenes, such as the loader itself.

# Scene: Starting Menu

Only a canvas/panel/ with a button inside, the button should try loading Main Scene.

# Scene: Main

All objects that should be in memory during game for all time, such as the player, the managers, and so on.

# Scene: Map_0_0

The whole map(now, we will split the map to chunk later, I haven't finish it),

# Other

I want a loading page when waiting for different scenes. But I'm not sure which scene should I place the page.