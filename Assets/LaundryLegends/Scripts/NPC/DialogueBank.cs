using System;
using System.Collections.Generic;

namespace LaundryLegends.NPC
{
    /// <summary>Speaker archetypes that own a line pool in <see cref="DialogueBank"/>.</summary>
    public enum NpcArchetype
    {
        /// <summary>Shop staff who works the counter and talks to the player as a colleague.</summary>
        Attendant = 0,

        /// <summary>Regular customer who waits in the seating area, often half on a call.</summary>
        PhoneRegular = 1,

        /// <summary>Walk-in customer dropping laundry off, first visit or occasional.</summary>
        DropOff = 2
    }

    /// <summary>Conversation moments a line can be selected for.</summary>
    public enum DialogueTrigger
    {
        /// <summary>The NPC enters the player's range or starts a visit.</summary>
        Enter = 0,

        /// <summary>Business at the counter: handing laundry over or taking it back.</summary>
        Counter = 1,

        /// <summary>The NPC is waiting in the shop.</summary>
        Waiting = 2,

        /// <summary>The NPC collects a finished order.</summary>
        Pickup = 3,

        /// <summary>No event has fired for a while; low-priority filler.</summary>
        Idle = 4
    }

    /// <summary>
    /// Read-only store of the approved 54 dialogue lines, keyed by speaker
    /// archetype and trigger (decision 0008). Text is the source of truth: a
    /// line plays as a subtitle even when no audio clip exists, and attaching a
    /// clip later does not change this API. Lookups never throw for unknown
    /// keys; they return an empty pool instead.
    /// </summary>
    public static class DialogueBank
    {
        /// <summary>Number of speaker archetypes that own lines.</summary>
        public const int ArchetypeCount = 3;

        /// <summary>Number of triggers each archetype answers.</summary>
        public const int TriggerCount = 5;

        /// <summary>Total number of authored lines in the bank.</summary>
        public const int TotalLineCount = 54;

        private static readonly IReadOnlyList<string>[][] Lines = BuildLines();

        /// <summary>
        /// Returns the immutable line pool for an archetype and trigger. The
        /// result is empty, never <c>null</c>, when the key is unknown.
        /// </summary>
        /// <param name="archetype">Speaker archetype to look up.</param>
        /// <param name="trigger">Conversation moment to look up.</param>
        /// <returns>A read-only list of lines; empty for an unknown key.</returns>
        public static IReadOnlyList<string> GetLines(NpcArchetype archetype, DialogueTrigger trigger)
        {
            int archetypeIndex = (int)archetype;
            int triggerIndex = (int)trigger;
            if (archetypeIndex < 0 || archetypeIndex >= ArchetypeCount)
            {
                return Array.Empty<string>();
            }

            if (triggerIndex < 0 || triggerIndex >= TriggerCount)
            {
                return Array.Empty<string>();
            }

            return Lines[archetypeIndex][triggerIndex];
        }

        /// <summary>Whether the key maps to at least one line.</summary>
        /// <param name="archetype">Speaker archetype to look up.</param>
        /// <param name="trigger">Conversation moment to look up.</param>
        /// <returns><c>true</c> when at least one line exists for the key.</returns>
        public static bool HasLines(NpcArchetype archetype, DialogueTrigger trigger)
        {
            return GetLines(archetype, trigger).Count > 0;
        }

        /// <summary>Number of lines available for a key; zero when the key is unknown.</summary>
        /// <param name="archetype">Speaker archetype to look up.</param>
        /// <param name="trigger">Conversation moment to look up.</param>
        /// <returns>The line count for the key.</returns>
        public static int GetLineCount(NpcArchetype archetype, DialogueTrigger trigger)
        {
            return GetLines(archetype, trigger).Count;
        }

        /// <summary>
        /// Picks one line from the pool for a key. Returns <c>null</c> only when
        /// the key has no lines, so callers can treat a null result as "say
        /// nothing". Pass the same seeded <see cref="System.Random"/> to get the
        /// same picks; a <c>null</c> random always picks the first line.
        /// </summary>
        /// <param name="archetype">Speaker archetype to pick from.</param>
        /// <param name="trigger">Conversation moment to pick from.</param>
        /// <param name="random">Random source for the pick; may be <c>null</c>.</param>
        /// <returns>A non-empty line, or <c>null</c> for an unknown or empty key.</returns>
        public static string Pick(NpcArchetype archetype, DialogueTrigger trigger, System.Random random)
        {
            IReadOnlyList<string> pool = GetLines(archetype, trigger);
            if (pool.Count == 0)
            {
                return null;
            }

            int index = random != null ? random.Next(pool.Count) : 0;
            return pool[index];
        }

        private static IReadOnlyList<string>[][] BuildLines()
        {
            var table = new IReadOnlyList<string>[ArchetypeCount][];

            table[(int)NpcArchetype.Attendant] = new[]
            {
                ReadOnly(
                    "Morning. Rain came in sideways about six, so the floor by the door is slick.",
                    "You're just in time. The seven o'clock crowd already dropped off four bags.",
                    "Machine three is making that noise again. I gave it one more load to prove a point."),
                ReadOnly(
                    "Two loads, one white, one mixed. I put the delicates on the cold cycle.",
                    "This one's a school uniform. They want it back before noon.",
                    "Customer said the stain is cooking oil. I'll pre-treat it and hope.",
                    "End of the year is starting early. Everyone's washing everything at once."),
                ReadOnly(
                    "Give the dryer another ten minutes. The air's too wet today for a short cycle.",
                    "Rainy season is good for business and terrible for drying. Trade-off.",
                    "If the power dips again, that old washer resets. Keep an eye on the panel.",
                    "I folded the towels already. They're on the shelf behind you."),
                ReadOnly(
                    "That's the one for the phone lady. She'll be here after school pickup.",
                    "Checked it twice. No spots, and it still smells like soap, not the machine.",
                    "Bag's tied. If she asks, tell her we used the gentle spin."),
                ReadOnly(
                    "Nyepi next month. Everything shuts, so everyone wants their laundry before.",
                    "My kid's class is doing the end-of-year performance. Three costume changes.",
                    "This humidity. Nothing dries unless you want it to.",
                    "We should get a second folding table before the ceremony rush.")
            };

            table[(int)NpcArchetype.PhoneRegular] = new[]
            {
                ReadOnly(
                    "Sorry — one second. Yes, I'm at the laundry. No, the one by the corner.",
                    "Hi. Just dropping these off. I'll be quick, I'm on my way to work.",
                    "Hold on. Sorry. Two bags today, and the blue shirt needs a cold wash."),
                ReadOnly(
                    "The school asked for the uniform back by Friday. Can we do Thursday?",
                    "This is my husband's work shirt. He says it's fine. It is not fine.",
                    "Whatever you did last time worked. That collar came out white again.",
                    "Rainy season, so everything smells like a wet towel by day two."),
                ReadOnly(
                    "Sorry, say that again? The signal in here is terrible when it rains.",
                    "Yes, I'm still waiting. No, I can't leave it until tomorrow.",
                    "My daughter's school is flooded, so she's home. Which means more laundry.",
                    "I'll sit by the window. Call me when it's done, I'll hear you."),
                ReadOnly(
                    "That's mine. Thank you — the kids will have clean uniforms tomorrow.",
                    "Let me check the bag. Good. Last place lost a sock every single time.",
                    "You're a lifesaver. I'll tell the others at the school gate."),
                ReadOnly(
                    "End of the year, everyone's got something to wash. Weddings, ceremonies, school.",
                    "It's so humid the towels never finish. I've been wearing the same jacket all week.",
                    "The road by the market flooded this morning. Took me twenty minutes to get here.",
                    "I'll bring my mother's things next week. She has opinions about soap.")
            };

            table[(int)NpcArchetype.DropOff] = new[]
            {
                ReadOnly(
                    "Hi, is this where I leave the laundry? Someone said you do it by the kilo.",
                    "Good morning. Rain caught me at the door. Is there somewhere I can put this?",
                    "Hello — my neighbour sent me. She said you're careful with the white shirts."),
                ReadOnly(
                    "Just these two. One is bed sheets, the other is everything else.",
                    "There's a spot on the sleeve. Cooking oil, I think. Can you have a look?",
                    "How long does it usually take? I don't mind waiting if it's not too long.",
                    "I need it by Saturday. There's a ceremony at the temple and everyone's coming."),
                ReadOnly(
                    "It smells good in here. Like clean cotton. Ours just smells like rain.",
                    "I'll wait outside, it's crowded. Or under the awning, at least.",
                    "The kids are off school this week, so the house is loud and the baskets are full.",
                    "Do you take card, or should I find an ATM before you finish?"),
                ReadOnly(
                    "That's faster than I expected. Thank you, really.",
                    "The shirt looks new. I'll bring the rest of the family's next time.",
                    "Can I pay now? I have exact change, I think."),
                ReadOnly(
                    "It's been raining since Tuesday. My whole line is still damp.",
                    "End of the year is expensive. New uniforms, new shoes, and now laundry.",
                    "My son spilled juice on his good shirt. Of course it was the good shirt.",
                    "Everyone says the dry season is easier. I'm still waiting for it.")
            };

            return table;
        }

        private static IReadOnlyList<string> ReadOnly(params string[] lines)
        {
            return Array.AsReadOnly(lines);
        }
    }
}
