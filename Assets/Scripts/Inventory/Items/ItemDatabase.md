# Goal

In ItemDatabase.cs

Implement random selection across categories.

Implement an interface for other script to call

# Detail

## random selection across categories
Call by a list of {int:ID, float:probability}

For example, given [{1, 0.2}, {2, 0.3}, {3, 0.5}]
It randomly returns a category(categories[id]) with given probability, then, randomly select a ItemSO from the category


## filter selection

Given ID and a list of tag, randomly choose a ItemSO from categories[id] that is in the tag list.

## ShopManager.cs
I will assign ItemDatabase to ShopManager.cs

For randomItemGroupID != 0

randomItemGroupID = 1: Request [{0, 0.6}, {1, 0.3}, {2, 0.1}]
randomItemGroupID = 2: Request [{1, 0.6}, {2, 0.3}, {3, 0.1}]
randomItemGroupID = 3: Request [{2, 0.6}, {3, 0.3}, {4, 0.1}]

To avoid hard coded in script, add these field(list of ID, prob, tag list) to SellingObjectSO.cs, these will not be used by randomItemGroupID == 0