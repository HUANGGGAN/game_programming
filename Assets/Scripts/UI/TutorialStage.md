# Goal

Implement tutorial in tutorial level(tutorial scene)

# Detail

There will be some stages in tutorial level, record which stages player has done in TutorialSO
Create a TutorialStagesManager.cs to manage these tutorials(this will be placed in Tutorial Scene, so you need to find Canvas in Main Scene during runtime)

## Structure

In Canvas from Main Scene
```
TopTutorialPanel
    TutorialTitle
    TutorialContent
    TutorialImage
    DoneText // invisible initially
```

## Request

When request TopTutorialPanel, it should move from -300 to 20(stretch mode in Canvas, make it configurable)(and make the speed configurable)

Each request is an ID(stage, from 1, zero is preserved), and make the configurable list in TutorialStagesManager.cs, each element is
- Title Text // String
- Content // String
- TutorialImage // Image, could be empty

So when some object request an ID, pop up with these element shown in TopTutorialPanel until player finish

## Finish

An tutorial stage(ID) can be finished by calling Finish(id) from other objects. 

If player finish the quest from ID, show DoneText and hide other children from TopTutorialPanel, wait for 2 seconds(make it configurable), then move the panel from -20 to -300(stretch mode)

After the coroutine finish, save it in TutorialSO(ID is enough), then check if there is pending request, or wait until other request.

## Tutorial Scene

Once player enter tutorial level or finish any stage, request with smallest ID greater than 0 that player haven't done yet.
If all ID are done, show ID:0, which reminds player.


# ID: Tutorial

Let's start with 3(I will manually add these in TutorialStagesManager.cs)

#### ID:0

On Request: Nothing to do

- Title Text: All done
- Content: You may return to main menu and start a new game.
- TutorialImage: Empty

#### ID:1

On Request: Nothing to do

- Title Text: Movement
- Content: Try moving around with WASD (You can configure key bindings in pause menu)
- TutorialImage: Empty

#### ID:2

On Request: Enable Pickable Item(a new script) that will trigger Finish(2) when picked up by player.

- Title Text: Pickup an Item
- Content: Pickup an item by pressing F
- TutorialImage: Empty