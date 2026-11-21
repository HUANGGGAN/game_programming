# Structure

Now

```
SceneLoader/
    LoadingCanvas/
        EventSystem
        LoadingPanel/
            Progress Bar
            LoadingText
MenuCanvas/
    MenuPanel/
        StartButton/
            Text
Camera
```

# Goal

Add level selection and a main menu button handler

# Detail

## Level selection

For now there is a StartButton that starts the main game, with all objects needed in Scene:Main
Then loads the Map_0_0.

I want to add a tutorial level, that is, something in Main will not be loaded, we can categorize them as

- Load across levels, both tutorial(Not Tutorial.cs) and main game. For example, Player.cs, Enemy Pool, Tutorial.cs
- Load only in main game, such as RegionEnemySpawner.cs
- Load only in Tutorial, such as the colliders or specific chests for the levels

#### Goal

I will split those things above into different scenes, for those that will be loaded across levels, load them whichever the level player choose(Main Game or Tutorial)

I will add a TutorialButton under MenuPanel, it should load the scenes.

## Main Menu Button

I will place a button under pause panel, it should switch the active scene to StartingMenu
If it will cause lagging(memory spike), we should consider switch to loading page first.