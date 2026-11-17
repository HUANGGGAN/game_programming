Implement LavaGolem.cs

The attack is just like other enemies' melee attack(an attack indicator)
But after attack, set up a fire path along the indicator.

My idea is to collect the indicator position and direction, and summon a fire region(you may need pool again)

However, the whole fire region will not set up immediately, I will provide 4 random fire image, the fire should set up from the beginning position to the end(along the indicator, with a configurable length), with each fire having small delay after the previous one.
(We may reuse this summon type in future, given a direction and length, summon component along this path)