using System;
using LaundryLegends.NPC;
using LaundryLegends.UI;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LaundryLegends.Tests.EditMode
{
    public sealed class DialogueBankTests
    {
        [Test]
        public void EveryArchetypeAnswersEveryTriggerWithAtLeastThreeLines()
        {
            foreach (NpcArchetype archetype in Enum.GetValues(typeof(NpcArchetype)))
            {
                foreach (DialogueTrigger trigger in Enum.GetValues(typeof(DialogueTrigger)))
                {
                    Assert.GreaterOrEqual(
                        DialogueBank.GetLineCount(archetype, trigger),
                        3,
                        $"{archetype}/{trigger} needs at least three lines.");
                }
            }
        }

        [Test]
        public void BankContainsExactlyFiftyFourLines()
        {
            int total = 0;
            foreach (NpcArchetype archetype in Enum.GetValues(typeof(NpcArchetype)))
            {
                int archetypeTotal = 0;
                foreach (DialogueTrigger trigger in Enum.GetValues(typeof(DialogueTrigger)))
                {
                    int count = DialogueBank.GetLineCount(archetype, trigger);
                    archetypeTotal += count;
                    total += count;
                }

                Assert.AreEqual(18, archetypeTotal, $"{archetype} should own exactly eighteen lines.");
            }

            Assert.AreEqual(54, total, "Decision 0008 authorises fifty-four lines.");
            Assert.AreEqual(DialogueBank.TotalLineCount, total);
        }

        [Test]
        public void NoLineIsEmptyOrWhitespace()
        {
            foreach (NpcArchetype archetype in Enum.GetValues(typeof(NpcArchetype)))
            {
                foreach (DialogueTrigger trigger in Enum.GetValues(typeof(DialogueTrigger)))
                {
                    foreach (string line in DialogueBank.GetLines(archetype, trigger))
                    {
                        Assert.IsFalse(
                            string.IsNullOrWhiteSpace(line),
                            $"{archetype}/{trigger} contains an empty line.");
                    }
                }
            }
        }

        [Test]
        public void PickNeverReturnsNullForAValidKey()
        {
            var random = new System.Random(20261031);
            foreach (NpcArchetype archetype in Enum.GetValues(typeof(NpcArchetype)))
            {
                foreach (DialogueTrigger trigger in Enum.GetValues(typeof(DialogueTrigger)))
                {
                    for (int i = 0; i < 25; i++)
                    {
                        string line = DialogueBank.Pick(archetype, trigger, random);
                        Assert.IsNotNull(line, $"{archetype}/{trigger} must always yield a line.");
                        Assert.IsFalse(string.IsNullOrWhiteSpace(line), $"{archetype}/{trigger} yielded blank text.");
                    }
                }
            }
        }

        [Test]
        public void PickIsDeterministicForAFixedSeed()
        {
            var first = new System.Random(4242);
            var second = new System.Random(4242);

            for (int i = 0; i < 40; i++)
            {
                Assert.AreEqual(
                    DialogueBank.Pick(NpcArchetype.PhoneRegular, DialogueTrigger.Waiting, first),
                    DialogueBank.Pick(NpcArchetype.PhoneRegular, DialogueTrigger.Waiting, second),
                    "The same seed must produce the same picks.");
            }
        }

        [Test]
        public void PickWithoutARandomSourceUsesTheFirstLine()
        {
            Assert.AreEqual(
                DialogueBank.GetLines(NpcArchetype.Attendant, DialogueTrigger.Enter)[0],
                DialogueBank.Pick(NpcArchetype.Attendant, DialogueTrigger.Enter, null));
        }

        [Test]
        public void UnknownKeysReturnNoLinesAndNoPick()
        {
            Assert.IsEmpty(DialogueBank.GetLines((NpcArchetype)99, DialogueTrigger.Enter));
            Assert.IsEmpty(DialogueBank.GetLines((NpcArchetype)(-1), DialogueTrigger.Enter));
            Assert.IsEmpty(DialogueBank.GetLines(NpcArchetype.Attendant, (DialogueTrigger)99));
            Assert.IsEmpty(DialogueBank.GetLines(NpcArchetype.Attendant, (DialogueTrigger)(-1)));
            Assert.IsFalse(DialogueBank.HasLines((NpcArchetype)99, (DialogueTrigger)99));
            Assert.AreEqual(0, DialogueBank.GetLineCount((NpcArchetype)99, DialogueTrigger.Idle));
            Assert.IsNull(DialogueBank.Pick((NpcArchetype)99, DialogueTrigger.Idle, new System.Random(1)));
            Assert.IsNull(DialogueBank.Pick(NpcArchetype.Attendant, (DialogueTrigger)99, null));
        }

        [Test]
        public void NpcDialogueCanSpeakABankLine()
        {
            var npcObject = new GameObject("DialogueBankTestNpc");
            try
            {
                NpcDialogue dialogue = npcObject.AddComponent<NpcDialogue>();
                dialogue.ConfigureForBank(NpcArchetype.DropOff, DialogueTrigger.Pickup);

                string line = dialogue.SpeakFromBank(DialogueTrigger.Pickup);

                Assert.IsNotNull(line);
                Assert.AreEqual(line, dialogue.CurrentLine);
                Assert.IsTrue(dialogue.IsTalking);
                CollectionAssert.Contains(
                    DialogueBank.GetLines(NpcArchetype.DropOff, DialogueTrigger.Pickup),
                    line);
            }
            finally
            {
                Object.DestroyImmediate(npcObject);
            }
        }

        [Test]
        public void ExplicitConfigureKeepsTheSceneSetupToolContract()
        {
            var npcObject = new GameObject("DialogueExplicitTestNpc");
            try
            {
                NpcDialogue dialogue = npcObject.AddComponent<NpcDialogue>();
                dialogue.ConfigureForBank(NpcArchetype.Attendant, DialogueTrigger.Enter);
                dialogue.Configure("First line.", "Second line.");

                Assert.IsFalse(dialogue.IsUsingDialogueBank);
                Assert.AreEqual(2, dialogue.ExplicitLineCount);

                dialogue.Say("Second line.");
                Assert.AreEqual("Second line.", dialogue.CurrentLine);
                Assert.IsTrue(dialogue.IsTalking);
            }
            finally
            {
                Object.DestroyImmediate(npcObject);
            }
        }

        [Test]
        public void SubtitleDurationStaysWithinReadableBounds()
        {
            Assert.AreEqual(SubtitleView.MinimumSeconds, SubtitleView.EstimateDuration(null), 0.001f);
            Assert.AreEqual(SubtitleView.MinimumSeconds, SubtitleView.EstimateDuration("   "), 0.001f);
            Assert.AreEqual(SubtitleView.MinimumSeconds, SubtitleView.EstimateDuration("Hi."), 0.001f);
            Assert.AreEqual(
                SubtitleView.MaximumSeconds,
                SubtitleView.EstimateDuration(new string('a', 400)),
                0.001f);
        }

        [Test]
        public void SubtitleAppearsForATextLineWithNoClip()
        {
            var viewObject = new GameObject("SubtitleViewTest");
            try
            {
                SubtitleView view = viewObject.AddComponent<SubtitleView>();
                view.Show("No clip is assigned to this line.");

                Assert.IsTrue(view.IsVisible);
                Assert.AreEqual("No clip is assigned to this line.", view.CurrentSubtitle);
                Assert.Greater(view.TimeRemaining, 0f);

                view.Hide();
                Assert.IsFalse(view.IsVisible);
                Assert.IsEmpty(view.CurrentSubtitle);
            }
            finally
            {
                Object.DestroyImmediate(viewObject);
            }
        }
    }
}
