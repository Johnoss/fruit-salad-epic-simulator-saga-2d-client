using System.Collections.Generic;
using System.Linq;
using FruitGoal;
using Tile;
using UnityEngine;

namespace Level
{
    /// <summary>
    /// Every level in the game, in order. One hundred of them.
    ///
    /// In code rather than in an asset. If this ever needs designer-facing tuning, make it a
    /// ScriptableObject holding the same list: <see cref="LevelController"/> only ever asks for an
    /// ordered list of definitions.
    ///
    /// Four dials, and they interact:
    /// - Fruit count is the main one. More kinds means fewer matches on screen, so the hunt for any
    ///   one fruit gets longer. Three is breezy, eight is genuinely hard.
    /// - Board size scales everything at once - more tiles, longer chains, more room to work.
    /// - Objective counts set the length. They are what turns a comfortable board into a long one.
    /// - Starting Starfruits take the edge off, and are spent the moment they are used.
    ///
    /// Shape is free: cells are square and capped so a board is never taller than it is wide, so a
    /// narrow strip or a letterbox just gets smaller cells rather than running off the screen.
    ///
    /// The curve runs from one objective on a 5x5 to three heavy ones on a 9x9, but it is not a
    /// straight line. Roughly every fifth level breaks the pattern on purpose - a two-column strip,
    /// a two-fruit sweep, a tiny board holding every fruit there is, a breather after a marathon -
    /// because a hundred variations on 7x7 is not a hundred levels. Named ones are commented; the
    /// rest are the steady climb between them.
    /// </summary>
    public static class LevelSet
    {
        private const TileType Apple = TileType.Apple;
        private const TileType Melon = TileType.Melon;
        private const TileType Coconut = TileType.Coconut;
        private const TileType Orange = TileType.Orange;
        private const TileType Kiwi = TileType.Kiwi;
        private const TileType Strawberry = TileType.Strawberry;
        private const TileType Avocado = TileType.Avocado;
        private const TileType Lemon = TileType.Lemon;
        private const TileType Star = TileType.Starfruit;

        /// Every fruit the board can spawn. Starfruit is not here - it is opt-in per level.
        private static readonly TileType[] All =
            { Apple, Melon, Coconut, Orange, Kiwi, Strawberry, Avocado, Lemon };

        // Named palettes, so a level can change its whole character in one word. Reusing a palette
        // a dozen levels later reads as a callback rather than as a repeat.
        private static readonly TileType[] Classic = { Apple, Melon, Coconut, Orange, Kiwi, Strawberry };
        private static readonly TileType[] Citrus = { Orange, Lemon, Apple, Melon, Kiwi };
        private static readonly TileType[] Tropical = { Coconut, Kiwi, Melon, Strawberry, Avocado };
        private static readonly TileType[] Garden = { Apple, Avocado, Lemon, Kiwi, Strawberry };
        private static readonly TileType[] Orchard = { Apple, Strawberry, Lemon, Coconut, Avocado };

        public static readonly IReadOnlyList<LevelDefinition> Levels = new[]
        {
            // ------------------------------------------------------------------ 1-10: learning it

            // 1. The hook. Three fruit on a small board - matches are everywhere and one objective
            //    is over in under a minute.
            new LevelDefinition(Board(5, 5), new[] { Apple, Melon, Kiwi },
                Goal(Apple, 8)),

            // 2. Same board, one more fruit, and a second objective to chase at the same time.
            new LevelDefinition(Board(5, 5), new[] { Apple, Melon, Kiwi, Orange },
                Goal(Kiwi, 10),
                Goal(Orange, 8)),

            // 3. Bigger board, and an entirely different palette - the fruit set changing is as
            //    much of a reset as the size is.
            new LevelDefinition(Board(6, 6), new[] { Apple, Strawberry, Lemon, Coconut },
                Goal(Strawberry, 12),
                Goal(Lemon, 10)),

            // 4. Five fruit and the first three-objective level.
            new LevelDefinition(Board(6, 6), new[] { Apple, Melon, Orange, Kiwi, Avocado },
                Goal(Avocado, 10),
                Goal(Melon, 12),
                Goal(Apple, 10)),

            // 5. SPECIAL - Orchard Strip. Three columns and eight rows. Only three fruit, because
            //    with two neighbours instead of eight a strip is already restrictive enough.
            new LevelDefinition(Board(8, 3), new[] { Apple, Lemon, Avocado },
                Goal(Apple, 14),
                Goal(Avocado, 12)),

            // 6. First wildcards on the table. Two, dealt into the middle, to be spent or saved.
            new LevelDefinition(Board(6, 6), new[] { Melon, Coconut, Orange, Strawberry, Lemon }, 2,
                Goal(Coconut, 12),
                Goal(Strawberry, 12)),

            // 7. Full-size board, six fruit. The baseline the rest of the set varies from.
            new LevelDefinition(Board(7, 7), Classic,
                Goal(Melon, 14),
                Goal(Kiwi, 12),
                Goal(Coconut, 10)),

            // 8. SPECIAL - Wide Open. Four rows, eight columns. Chains run sideways and the whole
            //    board is visible at a glance, so the counts can be steep without dragging.
            new LevelDefinition(Board(4, 8), new[] { Apple, Orange, Lemon, Avocado },
                Goal(Lemon, 18),
                Goal(Orange, 18)),

            // 9. The citrus-and-stone half of the set, with one wildcard to break a jam.
            new LevelDefinition(Board(7, 7), new[] { Orange, Lemon, Avocado, Coconut, Strawberry, Kiwi }, 1,
                Goal(Orange, 14),
                Goal(Avocado, 12),
                Goal(Kiwi, 12)),

            // 10. SPECIAL - Two of a Kind. Sixty-four tiles, two fruit. Every chain is enormous and
            //     the counts are absurd on purpose - this one is about the sweep, not the search.
            new LevelDefinition(Board(8, 8), new[] { Strawberry, Coconut },
                Goal(Strawberry, 40),
                Goal(Coconut, 40)),

            // ------------------------------------------------------- 11-20: the full set arrives

            // 11. Back to normal, and a palette that has not appeared yet.
            new LevelDefinition(Board(7, 7), new[] { Apple, Melon, Avocado, Lemon, Kiwi, Coconut },
                Goal(Avocado, 14),
                Goal(Lemon, 14),
                Goal(Melon, 12)),

            // 12. Seven fruit for the first time. Two wildcards to take the edge off.
            new LevelDefinition(Board(6, 7), Without(Avocado), 2,
                Goal(Strawberry, 14),
                Goal(Apple, 12),
                Goal(Lemon, 12)),

            // 13. SPECIAL - Starfruit Grove. The wildcard spawns as an ordinary tile here, and is
            //     itself an objective. Listed once against three of everything else, so it turns up
            //     roughly one tile in thirteen - common enough to plan around, rare enough to want.
            //     Collecting one still spends it, so the goal and the temptation pull against each
            //     other.
            new LevelDefinition(Board(6, 6), Weighted(3, new[] { Apple, Melon, Kiwi, Orange }, Star),
                Goal(Star, 6),
                Goal(Apple, 14)),

            // 14. Seven fruit, no help.
            new LevelDefinition(Board(7, 7), Without(Apple),
                Goal(Coconut, 14),
                Goal(Avocado, 14),
                Goal(Strawberry, 12)),

            // 15. SPECIAL - The Long Haul. A big board and only five fruit, so matches are easy and
            //     the difficulty is entirely the distance. Three wildcards for the tedium.
            new LevelDefinition(Board(8, 8), new[] { Apple, Melon, Orange, Kiwi, Coconut }, 3,
                Goal(Apple, 25),
                Goal(Orange, 25),
                Goal(Kiwi, 25)),

            // 16. Every fruit in the game, on a full board. The set is complete from here.
            new LevelDefinition(Board(7, 7), All,
                Goal(Kiwi, 14),
                Goal(Lemon, 12),
                Goal(Melon, 12)),

            // 17. SPECIAL - Needle in a Haystack. One objective, eight fruit. Roughly six lemons on
            //     the board at a time, so most of the level is clearing everything else to bring
            //     them together. One wildcard, because finding nothing for a while is the point and
            //     being stuck is not.
            new LevelDefinition(Board(7, 7), All, 1,
                Goal(Lemon, 24)),

            // 18. Widest board in the set so far, seven fruit, two wildcards.
            new LevelDefinition(Board(7, 8), Without(Melon), 2,
                Goal(Avocado, 16),
                Goal(Coconut, 14),
                Goal(Orange, 14)),

            // 19. SPECIAL - Pocket Square. Twenty-five tiles and all eight fruit, so roughly three
            //     of each. Matches are scarce and the board reshuffles itself often. The counts are
            //     small because everything about finding them is not.
            new LevelDefinition(Board(5, 5), All, 1,
                Goal(Strawberry, 6),
                Goal(Avocado, 6),
                Goal(Coconut, 6)),

            // 20. Everything at once: biggest board, every fruit, the heaviest objectives so far,
            //     and three wildcards to spend on them.
            new LevelDefinition(Board(8, 8), All, 3,
                Goal(Melon, 18),
                Goal(Strawberry, 18),
                Goal(Lemon, 18)),

            // ---------------------------------------------------------- 21-30: settling into it

            new LevelDefinition(Board(7, 7), Tropical,
                Goal(Coconut, 16),
                Goal(Avocado, 14),
                Goal(Melon, 14)),

            new LevelDefinition(Board(6, 7), Without(Coconut, Kiwi), 1,
                Goal(Lemon, 16),
                Goal(Apple, 14)),

            // 23. SPECIAL - Tall Order. Nine rows, four columns. Everything falls a long way, so a
            //     cleared column rewrites more of the board than anywhere else in the set.
            new LevelDefinition(Board(9, 4), new[] { Melon, Kiwi, Orange, Coconut },
                Goal(Melon, 18),
                Goal(Kiwi, 16)),

            new LevelDefinition(Board(7, 7), Garden, 1,
                Goal(Avocado, 16),
                Goal(Strawberry, 16),
                Goal(Kiwi, 14)),

            new LevelDefinition(Board(8, 8), Classic, 2,
                Goal(Apple, 20),
                Goal(Coconut, 18),
                Goal(Orange, 16)),

            // 26. SPECIAL - Melon Patch. Three fruit, one objective, one enormous number. No hunting
            //     at all - just a board that keeps handing you melons until you are sick of them.
            new LevelDefinition(Board(7, 7), new[] { Melon, Coconut, Kiwi },
                Goal(Melon, 45)),

            new LevelDefinition(Board(7, 7), Without(Lemon),
                Goal(Strawberry, 16),
                Goal(Avocado, 14),
                Goal(Melon, 14)),

            new LevelDefinition(Board(6, 8), Citrus, 1,
                Goal(Lemon, 20),
                Goal(Orange, 18),
                Goal(Apple, 16)),

            new LevelDefinition(Board(7, 7), All, 2,
                Goal(Coconut, 16),
                Goal(Lemon, 14),
                Goal(Kiwi, 14)),

            // 30. SPECIAL - Double Trouble. The two-fruit sweep again, different pair, bigger board.
            //     Chains run the whole width if you let them.
            new LevelDefinition(Board(8, 8), new[] { Lemon, Avocado },
                Goal(Lemon, 45),
                Goal(Avocado, 45)),

            // ------------------------------------------------------------ 31-40: no more warm-up

            new LevelDefinition(Board(7, 8), Without(Apple), 1,
                Goal(Orange, 18),
                Goal(Coconut, 16),
                Goal(Lemon, 16)),

            new LevelDefinition(Board(7, 7), Orchard,
                Goal(Avocado, 18),
                Goal(Strawberry, 18),
                Goal(Coconut, 16)),

            // 33. SPECIAL - Wide Load. Four rows, nine columns. The flattest board there is.
            new LevelDefinition(Board(4, 9), new[] { Apple, Melon, Kiwi, Coconut, Strawberry },
                Goal(Strawberry, 22),
                Goal(Apple, 20)),

            new LevelDefinition(Board(8, 8), Without(Strawberry), 2,
                Goal(Kiwi, 20),
                Goal(Avocado, 18),
                Goal(Melon, 18)),

            new LevelDefinition(Board(7, 7), All,
                Goal(Apple, 16),
                Goal(Orange, 16),
                Goal(Avocado, 14)),

            new LevelDefinition(Board(6, 6), Tropical, 1,
                Goal(Kiwi, 18),
                Goal(Coconut, 16)),

            // 37. SPECIAL - Citrus Squeeze. Every objective is citrus and the two fruit that are
            //     not - melon and kiwi - are pure obstruction, so most of the board is in the way.
            new LevelDefinition(Board(7, 7), Citrus,
                Goal(Lemon, 24),
                Goal(Orange, 24),
                Goal(Apple, 16)),

            new LevelDefinition(Board(8, 7), Without(Kiwi), 2,
                Goal(Apple, 20),
                Goal(Lemon, 18),
                Goal(Strawberry, 18)),

            new LevelDefinition(Board(7, 7), All, 1,
                Goal(Melon, 18),
                Goal(Coconut, 16),
                Goal(Strawberry, 16)),

            // 40. SPECIAL - Starfruit Thicket. Wildcards in the spawn pool again, twice as common as
            //     in the Grove, and the objective is twice the size. Every one you collect is one
            //     you cannot spend.
            new LevelDefinition(Board(7, 7), Weighted(3, new[] { Apple, Melon, Coconut, Kiwi, Orange }, Star, Star),
                Goal(Star, 12),
                Goal(Coconut, 16)),

            // ------------------------------------------------------------- 41-50: to the halfway

            new LevelDefinition(Board(8, 8), Garden, 2,
                Goal(Avocado, 22),
                Goal(Lemon, 20),
                Goal(Kiwi, 20)),

            new LevelDefinition(Board(7, 7), Without(Melon, Coconut),
                Goal(Strawberry, 18),
                Goal(Orange, 18),
                Goal(Apple, 16)),

            // 43. SPECIAL - The Pillar. Three columns, nine rows, and the tallest thing in the game.
            //     Six fruit is already a lot for a board this narrow.
            new LevelDefinition(Board(9, 3), Classic, 1,
                Goal(Apple, 16),
                Goal(Melon, 14)),

            new LevelDefinition(Board(8, 8), All, 2,
                Goal(Lemon, 20),
                Goal(Kiwi, 20),
                Goal(Coconut, 18)),

            new LevelDefinition(Board(7, 8), Orchard,
                Goal(Coconut, 22),
                Goal(Apple, 20),
                Goal(Lemon, 20)),

            new LevelDefinition(Board(7, 7), Without(Orange), 1,
                Goal(Avocado, 20),
                Goal(Melon, 18),
                Goal(Strawberry, 18)),

            // 47. SPECIAL - Quick Snack. Sixteen tiles, three fruit, tiny numbers. A deliberate
            //     ninety seconds off after six heavy levels.
            new LevelDefinition(Board(4, 4), new[] { Apple, Orange, Kiwi },
                Goal(Apple, 10),
                Goal(Kiwi, 10)),

            new LevelDefinition(Board(8, 8), Classic, 1,
                Goal(Melon, 24),
                Goal(Orange, 22),
                Goal(Kiwi, 22)),

            new LevelDefinition(Board(7, 7), All,
                Goal(Strawberry, 18),
                Goal(Avocado, 18),
                Goal(Apple, 16)),

            // 50. SPECIAL - Halfway Feast. Everything the game has, four wildcards, and the biggest
            //     numbers so far. A landmark rather than a wall.
            new LevelDefinition(Board(8, 8), All, 4,
                Goal(Coconut, 25),
                Goal(Lemon, 25),
                Goal(Melon, 25)),

            // --------------------------------------------------------- 51-60: the second half

            new LevelDefinition(Board(7, 8), Without(Avocado), 1,
                Goal(Kiwi, 22),
                Goal(Coconut, 20),
                Goal(Strawberry, 20)),

            new LevelDefinition(Board(8, 8), Tropical,
                Goal(Melon, 26),
                Goal(Avocado, 24),
                Goal(Coconut, 22)),

            // 53. SPECIAL - Pinstripe. Two columns. Every tile has at most five neighbours and the
            //     board is one long ladder. Three fruit, because anything more would be cruel.
            new LevelDefinition(Board(8, 2), new[] { Strawberry, Lemon, Kiwi },
                Goal(Strawberry, 14),
                Goal(Lemon, 12)),

            new LevelDefinition(Board(7, 7), All, 1,
                Goal(Orange, 20),
                Goal(Avocado, 18),
                Goal(Melon, 18)),

            new LevelDefinition(Board(8, 8), Without(Apple, Lemon), 2,
                Goal(Coconut, 24),
                Goal(Kiwi, 22),
                Goal(Melon, 22)),

            new LevelDefinition(Board(6, 7), Garden,
                Goal(Lemon, 20),
                Goal(Strawberry, 18),
                Goal(Avocado, 18)),

            // 57. SPECIAL - Sour Note. Two fruit dominate the pool three to one, and both objectives
            //     are on the third. The board is mostly noise.
            new LevelDefinition(Board(7, 7), Weighted(3, new[] { Lemon, Avocado }, Strawberry, Kiwi, Coconut),
                Goal(Strawberry, 18),
                Goal(Coconut, 16)),

            new LevelDefinition(Board(8, 8), All, 2,
                Goal(Apple, 22),
                Goal(Lemon, 22),
                Goal(Kiwi, 20)),

            new LevelDefinition(Board(7, 7), Citrus, 1,
                Goal(Orange, 24),
                Goal(Melon, 22),
                Goal(Apple, 20)),

            // 60. SPECIAL - Two of a Kind III. The full board, two fruit, and the largest numbers in
            //     the game to this point. Pure sweep.
            new LevelDefinition(Board(9, 9), new[] { Apple, Kiwi },
                Goal(Apple, 55),
                Goal(Kiwi, 55)),

            // -------------------------------------------------------------- 61-70: getting mean

            new LevelDefinition(Board(8, 8), Without(Coconut), 1,
                Goal(Avocado, 24),
                Goal(Strawberry, 22),
                Goal(Lemon, 22)),

            new LevelDefinition(Board(7, 8), All, 2,
                Goal(Melon, 22),
                Goal(Orange, 20),
                Goal(Kiwi, 20)),

            // 63. SPECIAL - Needle II. One objective, every fruit, and half as many again as the
            //     first time. No wildcards.
            new LevelDefinition(Board(7, 7), All,
                Goal(Avocado, 30)),

            new LevelDefinition(Board(8, 8), Orchard, 1,
                Goal(Apple, 26),
                Goal(Coconut, 24),
                Goal(Avocado, 24)),

            new LevelDefinition(Board(7, 7), Without(Strawberry, Melon),
                Goal(Lemon, 22),
                Goal(Kiwi, 20),
                Goal(Orange, 20)),

            new LevelDefinition(Board(8, 8), All, 3,
                Goal(Coconut, 24),
                Goal(Strawberry, 24),
                Goal(Apple, 22)),

            // 67. SPECIAL - Scattergun. Five rows, nine columns, all eight fruit. The board is wide
            //     enough that the fruit you want is always somewhere else.
            new LevelDefinition(Board(5, 9), All, 1,
                Goal(Kiwi, 24),
                Goal(Lemon, 22),
                Goal(Melon, 22)),

            new LevelDefinition(Board(8, 8), Tropical, 1,
                Goal(Strawberry, 28),
                Goal(Coconut, 26),
                Goal(Kiwi, 24)),

            new LevelDefinition(Board(7, 7), Without(Kiwi, Apple),
                Goal(Avocado, 22),
                Goal(Melon, 20),
                Goal(Lemon, 20)),

            // 70. SPECIAL - Starfruit Orchard. The last and heaviest of the wildcard-pool levels.
            //     Twenty of them to collect, and everything else on the board wants them too.
            new LevelDefinition(Board(8, 8), Weighted(3, new[] { Apple, Melon, Coconut, Kiwi, Orange, Lemon }, Star, Star),
                Goal(Star, 20),
                Goal(Apple, 22),
                Goal(Lemon, 20)),

            // ------------------------------------------------------------ 71-80: the long climb

            new LevelDefinition(Board(8, 8), All, 2,
                Goal(Lemon, 26),
                Goal(Avocado, 24),
                Goal(Coconut, 24)),

            new LevelDefinition(Board(8, 9), Classic, 1,
                Goal(Melon, 30),
                Goal(Apple, 28),
                Goal(Orange, 26)),

            // 73. SPECIAL - Compact Chaos. Pocket Square again, harder: same twenty-five tiles and
            //     all eight fruit, but the numbers are doubled and there is no wildcard.
            new LevelDefinition(Board(5, 5), All,
                Goal(Kiwi, 12),
                Goal(Lemon, 12),
                Goal(Melon, 12)),

            new LevelDefinition(Board(8, 8), Without(Melon), 2,
                Goal(Strawberry, 28),
                Goal(Orange, 26),
                Goal(Avocado, 24)),

            new LevelDefinition(Board(7, 8), All, 1,
                Goal(Coconut, 24),
                Goal(Apple, 24),
                Goal(Kiwi, 22)),

            new LevelDefinition(Board(9, 9), Citrus, 2,
                Goal(Lemon, 32),
                Goal(Orange, 30),
                Goal(Melon, 28)),

            // 77. SPECIAL - The Gauntlet. Seven fruit, a full board, three objectives at thirty
            //     apiece and nothing to help with them.
            new LevelDefinition(Board(8, 8), Without(Apple),
                Goal(Coconut, 30),
                Goal(Lemon, 30),
                Goal(Avocado, 30)),

            new LevelDefinition(Board(8, 8), Garden, 2,
                Goal(Avocado, 28),
                Goal(Strawberry, 26),
                Goal(Kiwi, 26)),

            new LevelDefinition(Board(7, 7), All, 1,
                Goal(Melon, 24),
                Goal(Lemon, 22),
                Goal(Strawberry, 22)),

            // 80. SPECIAL - Endurance. The largest board, five fruit so matches are never the
            //     problem, and thirty-five of each. This one is measured in minutes.
            new LevelDefinition(Board(9, 9), new[] { Apple, Melon, Coconut, Orange, Kiwi }, 3,
                Goal(Apple, 35),
                Goal(Melon, 35),
                Goal(Coconut, 35)),

            // ----------------------------------------------------------- 81-90: the last stretch

            new LevelDefinition(Board(8, 8), All, 2,
                Goal(Orange, 28),
                Goal(Avocado, 26),
                Goal(Coconut, 26)),

            new LevelDefinition(Board(8, 9), Without(Coconut), 1,
                Goal(Kiwi, 30),
                Goal(Strawberry, 28),
                Goal(Lemon, 28)),

            // 83. SPECIAL - Thread the Needle. Three columns and nine rows, six fruit. The narrowest
            //     board in the game carrying more variety than it comfortably can.
            new LevelDefinition(Board(9, 3), Without(Avocado, Lemon), 2,
                Goal(Strawberry, 18),
                Goal(Coconut, 16)),

            new LevelDefinition(Board(8, 8), Tropical, 1,
                Goal(Avocado, 32),
                Goal(Melon, 30),
                Goal(Kiwi, 28)),

            new LevelDefinition(Board(9, 9), All, 3,
                Goal(Lemon, 30),
                Goal(Apple, 28),
                Goal(Melon, 28)),

            new LevelDefinition(Board(7, 8), Without(Lemon, Kiwi),
                Goal(Avocado, 26),
                Goal(Coconut, 24),
                Goal(Strawberry, 24)),

            // 87. SPECIAL - Starfruit Storm. Four wildcards to start and nothing else unusual - a
            //     board that opens already half solved, and objectives that assume you noticed.
            new LevelDefinition(Board(8, 8), All, 4,
                Goal(Kiwi, 32),
                Goal(Orange, 30),
                Goal(Avocado, 30)),

            new LevelDefinition(Board(9, 9), Orchard, 2,
                Goal(Apple, 34),
                Goal(Lemon, 32),
                Goal(Coconut, 30)),

            new LevelDefinition(Board(8, 8), All, 1,
                Goal(Melon, 28),
                Goal(Strawberry, 28),
                Goal(Kiwi, 26)),

            // 90. SPECIAL - Ninety. Eight rows, nine columns, every fruit, and no wildcards at all.
            new LevelDefinition(Board(8, 9), All,
                Goal(Coconut, 32),
                Goal(Avocado, 30),
                Goal(Orange, 30)),

            // --------------------------------------------------------------- 91-100: the finish

            new LevelDefinition(Board(9, 9), All, 3,
                Goal(Lemon, 32),
                Goal(Kiwi, 32),
                Goal(Apple, 30)),

            new LevelDefinition(Board(8, 8), Without(Strawberry), 1,
                Goal(Avocado, 32),
                Goal(Melon, 30),
                Goal(Coconut, 30)),

            // 93. SPECIAL - Last Stand. Thirty-six tiles and all eight fruit. The smallest board in
            //     the endgame, and the one where nothing ever seems to match.
            new LevelDefinition(Board(6, 6), All, 1,
                Goal(Strawberry, 16),
                Goal(Lemon, 16),
                Goal(Avocado, 16)),

            new LevelDefinition(Board(9, 9), Classic, 2,
                Goal(Apple, 36),
                Goal(Kiwi, 34),
                Goal(Melon, 32)),

            new LevelDefinition(Board(8, 9), All, 2,
                Goal(Orange, 32),
                Goal(Coconut, 32),
                Goal(Strawberry, 30)),

            // 96. SPECIAL - Marathon. Eighty-one tiles, every fruit, and the longest three
            //     objectives in the game outside the finale.
            new LevelDefinition(Board(9, 9), All, 3,
                Goal(Melon, 38),
                Goal(Lemon, 36),
                Goal(Kiwi, 36)),

            new LevelDefinition(Board(8, 8), Without(Orange), 1,
                Goal(Avocado, 32),
                Goal(Strawberry, 30),
                Goal(Apple, 30)),

            // 98. SPECIAL - Narrow Escape. One last strip, this time with everything on it. A
            //     deliberate change of shape right before the end.
            new LevelDefinition(Board(9, 4), Without(Melon), 2,
                Goal(Coconut, 24),
                Goal(Lemon, 22),
                Goal(Kiwi, 22)),

            new LevelDefinition(Board(9, 9), All, 2,
                Goal(Apple, 36),
                Goal(Orange, 36),
                Goal(Avocado, 34)),

            // 100. SPECIAL - The Whole Salad. Every fruit, the largest board, four wildcards, and
            //      forty of each. This is the end of the game, and the panel says so from here on.
            new LevelDefinition(Board(9, 9), All, 4,
                Goal(Melon, 40),
                Goal(Strawberry, 40),
                Goal(Lemon, 40)),
        };

        /// Rows first, then columns - the same order Vector2Int takes them in.
        private static Vector2Int Board(int rows, int columns)
        {
            return new Vector2Int(rows, columns);
        }

        private static FruitObjectiveDefinition Goal(TileType fruit, int requiredCount)
        {
            return new FruitObjectiveDefinition(fruit, requiredCount);
        }

        /// Every fruit except the ones named. The seven-fruit levels are all one fruit short of the
        /// full set, and saying which one is missing reads better than listing the other seven.
        private static TileType[] Without(params TileType[] excluded)
        {
            return All.Where(fruit => !excluded.Contains(fruit)).ToArray();
        }

        /// <summary>
        /// A pool where <paramref name="common"/> fruit appear <paramref name="weight"/> times each
        /// and <paramref name="rare"/> fruit appear once. The board draws from the pool uniformly,
        /// so repetition is the only weighting there is - this just makes the intent readable.
        /// </summary>
        private static TileType[] Weighted(int weight, IReadOnlyList<TileType> common, params TileType[] rare)
        {
            var pool = new List<TileType>(common.Count * weight + rare.Length);

            foreach (var fruit in common)
            {
                for (var repeat = 0; repeat < weight; repeat++)
                {
                    pool.Add(fruit);
                }
            }

            pool.AddRange(rare);

            return pool.ToArray();
        }
    }
}
