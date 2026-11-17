# Goal

- Create a new type of random item selection method in ItemDatabaseSO.cs
- Implement Enemy drop

# Detail

## Random Item selection

Given a list of {ItemSO, prob} and a list of {ID, prob, taglist}

return no more than one item from ItemSO list or ItemSO from one of the categories using taglist
note that the sum of the probability should fall between 0 and 1, that is, it could return nothing.

For example, given

[{item1, 0.1}, {item2, 0.3}]
[{ID:3, 0.5, {Fire, Frost}}]

0.1+0.3+0.5=0.9, so there is 
10% of chance return nothing
10% return item1
30% return item2
50% return one of the item from category 3 with tag either Fire or Frost

## Enemy Drop

In EnemySO.cs, add two list mentioned above do decide the item to drop

### Dropping process

- Create an interactable object DroppedItem.cs

The drop animation should look like coin drop(See EnemyFeedbackPool.cs), and player can pick up by pressing F(See Pickable.cs)

You need to create a pool for this.

When the item is far away from player(you can use manhatton distance), disable it.

There is max pool size, if reach max size, reuse the furthest item(not delete it)

After enemy is dead, drop the item if the random selection returns