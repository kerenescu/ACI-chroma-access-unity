using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace ChromaAccess.DungeonTrial
{
    /// <summary>
    /// Palette families used by the experiment and the runtime controller.
    /// </summary>
    public enum DungeonTrialPalette
    {
        Original = 0,
        DeutanPalette = 1,
        ProtanPalette = 2,
        TritanomalyPalette = 3,
        BalancedPalette = 4
    }

    /// <summary>
    /// Runtime previews use the same fixed-severity Machado matrices as the
    /// project-level simulation. Tritanomaly is intentionally not called
    /// tritanopia: the available project model is only an approximation.
    /// </summary>
    public enum DungeonTrialPreview
    {
        None = 0,
        Deuteranopia = 1,
        Protanopia = 2,
        TritanomalyApproximation = 3
    }

    /// <summary>
    /// The three experiment perspectives requested for the demonstrator.
    /// The concrete palette and simulation used by a round are kept separate
    /// in DungeonTrialPresentation and are logged individually.
    /// </summary>
    public enum DungeonTrialGameMode
    {
        RawPalettesNormalVision = 0,
        RgbThroughCvdMatrices = 1,
        AlternativePalettesThroughCvdMatrices = 2
    }

    public sealed class DungeonTrialPresentation
    {
        public readonly DungeonTrialPalette Palette;
        public readonly DungeonTrialPreview Preview;

        public DungeonTrialPresentation(DungeonTrialPalette palette, DungeonTrialPreview preview)
        {
            Palette = palette;
            Preview = preview;
        }
    }

    /// <summary>
    /// Palette candidates copied from Assets/Settings/PaletteLab.json.
    /// They are controlled-scene numerical candidates, not universal player
    /// corrections. Each call returns a fresh array for safe scene ownership.
    /// </summary>
    public static class DungeonTrialPaletteLibrary
    {
        public static Color[] Create(DungeonTrialPalette palette)
        {
            switch (palette)
            {
                case DungeonTrialPalette.Original:
                    return Palette(0xC8, 0x4C, 0x4C, 0x4C, 0xC8, 0x4C, 0x4C, 0x4C, 0xC8);
                case DungeonTrialPalette.DeutanPalette:
                    return Palette(0xBB, 0x55, 0x00, 0xEE, 0x88, 0xFF, 0x4C, 0x4C, 0xC8);
                case DungeonTrialPalette.ProtanPalette:
                    return Palette(0x88, 0x77, 0x11, 0x55, 0xBB, 0xEE, 0x4C, 0x4C, 0xC8);
                case DungeonTrialPalette.TritanomalyPalette:
                    return Palette(0xC8, 0x4C, 0x4C, 0x00, 0xBB, 0xFF, 0x4C, 0x4C, 0xC8);
                case DungeonTrialPalette.BalancedPalette:
                    return Palette(0xCC, 0x44, 0x11, 0x55, 0xBB, 0xEE, 0x4C, 0x4C, 0xC8);
                default:
                    throw new ArgumentOutOfRangeException(nameof(palette));
            }
        }

        private static Color[] Palette(
            byte r1, byte g1, byte b1,
            byte r2, byte g2, byte b2,
            byte r3, byte g3, byte b3)
        {
            return new[]
            {
                Rgb(r1, g1, b1),
                Rgb(r2, g2, b2),
                Rgb(r3, g3, b3)
            };
        }

        private static Color Rgb(byte red, byte green, byte blue)
        {
            return new Color32(red, green, blue, byte.MaxValue);
        }
    }

    /// <summary>
    /// One colour-matching trial. The target and the three candidate roles are
    /// stored as indices (0, 1, 2), so the same schedule works with either
    /// palette without changing its answer key.
    /// </summary>
    public sealed class DungeonTrialDefinition
    {
        private readonly int[] choiceColorIndexes;

        public int TargetColorIndex { get; private set; }

        public DungeonTrialDefinition(int targetColorIndex, int[] configuredChoiceColorIndexes)
        {
            if (configuredChoiceColorIndexes == null)
                throw new ArgumentNullException(nameof(configuredChoiceColorIndexes));

            TargetColorIndex = targetColorIndex;
            choiceColorIndexes = (int[])configuredChoiceColorIndexes.Clone();

            if (!IsValid())
                throw new ArgumentException("A trial must contain each of the three colour roles exactly once.");
        }

        public int GetChoiceColorIndex(int choiceIndex)
        {
            return choiceColorIndexes[choiceIndex];
        }

        public int GetCorrectChoiceIndex()
        {
            for (int choiceIndex = 0; choiceIndex < choiceColorIndexes.Length; choiceIndex++)
            {
                if (choiceColorIndexes[choiceIndex] == TargetColorIndex)
                    return choiceIndex;
            }

            return -1;
        }

        public string Signature()
        {
            return TargetColorIndex + ":" + choiceColorIndexes[0] + choiceColorIndexes[1] + choiceColorIndexes[2];
        }

        public bool IsValid()
        {
            if (TargetColorIndex < 0 || TargetColorIndex >= DungeonTrialSchedule.ColorCount)
                return false;

            if (choiceColorIndexes == null || choiceColorIndexes.Length != DungeonTrialSchedule.ColorCount)
                return false;

            bool[] seen = new bool[DungeonTrialSchedule.ColorCount];
            for (int choiceIndex = 0; choiceIndex < choiceColorIndexes.Length; choiceIndex++)
            {
                int colorIndex = choiceColorIndexes[choiceIndex];
                if (colorIndex < 0 || colorIndex >= DungeonTrialSchedule.ColorCount || seen[colorIndex])
                    return false;

                seen[colorIndex] = true;
            }

            return GetCorrectChoiceIndex() >= 0;
        }
    }

    /// <summary>
    /// Builds a balanced set of 18 trials: every target role, every candidate
    /// position, and every permutation is represented. Only their order is
    /// randomized, which makes the session reproducible from its logged seed.
    /// </summary>
    public static class DungeonTrialSchedule
    {
        public const int ColorCount = 3;
        public const int TrialCount = 18;

        private static readonly int[][] CandidatePermutations =
        {
            new[] { 0, 1, 2 },
            new[] { 0, 2, 1 },
            new[] { 1, 0, 2 },
            new[] { 1, 2, 0 },
            new[] { 2, 0, 1 },
            new[] { 2, 1, 0 }
        };

        public static List<DungeonTrialDefinition> CreateBalanced(int seed)
        {
            List<DungeonTrialDefinition> schedule = new List<DungeonTrialDefinition>(TrialCount);

            foreach (int[] permutation in CandidatePermutations)
            {
                for (int targetColorIndex = 0; targetColorIndex < ColorCount; targetColorIndex++)
                    schedule.Add(new DungeonTrialDefinition(targetColorIndex, permutation));
            }

            System.Random random = new System.Random(seed);
            for (int index = schedule.Count - 1; index > 0; index--)
            {
                int otherIndex = random.Next(index + 1);
                DungeonTrialDefinition temporary = schedule[index];
                schedule[index] = schedule[otherIndex];
                schedule[otherIndex] = temporary;
            }

            return schedule;
        }

        public static bool TryValidate(IList<DungeonTrialDefinition> schedule, out string reason)
        {
            if (schedule == null)
            {
                reason = "The schedule is null.";
                return false;
            }

            if (schedule.Count != TrialCount)
            {
                reason = "Expected " + TrialCount + " trials, got " + schedule.Count + ".";
                return false;
            }

            int[] targetCounts = new int[ColorCount];
            int[] correctPositionCounts = new int[ColorCount];
            HashSet<string> signatures = new HashSet<string>();

            foreach (DungeonTrialDefinition trial in schedule)
            {
                if (trial == null || !trial.IsValid())
                {
                    reason = "A trial has an invalid target or candidate permutation.";
                    return false;
                }

                if (!signatures.Add(trial.Signature()))
                {
                    reason = "The schedule contains a duplicate trial: " + trial.Signature() + ".";
                    return false;
                }

                targetCounts[trial.TargetColorIndex]++;
                correctPositionCounts[trial.GetCorrectChoiceIndex()]++;
            }

            int expectedCount = TrialCount / ColorCount;
            for (int index = 0; index < ColorCount; index++)
            {
                if (targetCounts[index] != expectedCount)
                {
                    reason = "Target role " + index + " appears " + targetCounts[index] + " times instead of " + expectedCount + ".";
                    return false;
                }

                if (correctPositionCounts[index] != expectedCount)
                {
                    reason = "Correct position " + (index + 1) + " appears " + correctPositionCounts[index] + " times instead of " + expectedCount + ".";
                    return false;
                }
            }

            reason = null;
            return true;
        }
    }

    /// <summary>
    /// Couples one answer-key trial to its exact source palette and simulated
    /// vision. It prevents the three high-level modes from losing their
    /// per-round experimental metadata.
    /// </summary>
    public sealed class DungeonTrialScheduledRound
    {
        public readonly DungeonTrialDefinition Trial;
        public readonly DungeonTrialPresentation Presentation;

        public DungeonTrialScheduledRound(DungeonTrialDefinition trial, DungeonTrialPresentation presentation)
        {
            Trial = trial ?? throw new ArgumentNullException(nameof(trial));
            Presentation = presentation ?? throw new ArgumentNullException(nameof(presentation));
        }
    }

    public static class DungeonTrialGameModeSchedule
    {
        public const int RawPalettesTrialCount = 24;
        public const int RgbMatricesTrialCount = 24;
        public const int AlternativeCrossTrialCount = 27;

        private static readonly DungeonTrialPalette[] AlternativePalettes =
        {
            DungeonTrialPalette.DeutanPalette,
            DungeonTrialPalette.ProtanPalette,
            DungeonTrialPalette.TritanomalyPalette
        };

        private static readonly DungeonTrialPreview[] CvdPreviews =
        {
            DungeonTrialPreview.Deuteranopia,
            DungeonTrialPreview.Protanopia,
            DungeonTrialPreview.TritanomalyApproximation
        };

        private static readonly int[][] SixPermutations =
        {
            new[] { 0, 1, 2 }, new[] { 0, 2, 1 }, new[] { 1, 0, 2 },
            new[] { 1, 2, 0 }, new[] { 2, 0, 1 }, new[] { 2, 1, 0 }
        };

        // Each role and correct choice position appears twice in a six-round block.
        private static readonly int[] SixTargets = { 0, 1, 0, 2, 1, 2 };
        private static readonly int[] IdentityPermutation = { 0, 1, 2 };

        public static int GetRunTrialCount(DungeonTrialGameMode mode)
        {
            switch (mode)
            {
                case DungeonTrialGameMode.RawPalettesNormalVision: return RawPalettesTrialCount;
                case DungeonTrialGameMode.RgbThroughCvdMatrices: return RgbMatricesTrialCount;
                case DungeonTrialGameMode.AlternativePalettesThroughCvdMatrices: return AlternativeCrossTrialCount;
                default: throw new ArgumentOutOfRangeException(nameof(mode));
            }
        }

        public static string ConditionKey(DungeonTrialPresentation presentation)
        {
            if (presentation == null)
                throw new ArgumentNullException(nameof(presentation));

            return presentation.Palette + "__" + presentation.Preview;
        }

        public static List<DungeonTrialScheduledRound> Create(DungeonTrialGameMode selectedMode, int seed)
        {
            switch (selectedMode)
            {
                case DungeonTrialGameMode.RawPalettesNormalVision:
                    return CreateRawPalettes(seed);
                case DungeonTrialGameMode.RgbThroughCvdMatrices:
                    return CreateRgbThroughMatrices(seed);
                case DungeonTrialGameMode.AlternativePalettesThroughCvdMatrices:
                    return CreateAlternativeCrossProduct(seed);
                default:
                    throw new ArgumentOutOfRangeException(nameof(selectedMode));
            }
        }

        public static bool TryValidate(IList<DungeonTrialScheduledRound> schedule, DungeonTrialGameMode selectedMode, out string reason)
        {
            if (schedule == null)
            {
                reason = "The scheduled run is null.";
                return false;
            }

            int expectedCount = GetRunTrialCount(selectedMode);
            if (schedule.Count != expectedCount)
            {
                reason = "Expected " + expectedCount + " rounds, got " + schedule.Count + ".";
                return false;
            }

            Dictionary<string, int> conditionCounts = new Dictionary<string, int>();
            Dictionary<string, int[]> conditionTargetCounts = new Dictionary<string, int[]>();
            Dictionary<string, int[]> conditionPositionCounts = new Dictionary<string, int[]>();
            int[] totalTargetCounts = new int[DungeonTrialSchedule.ColorCount];
            int[] totalPositionCounts = new int[DungeonTrialSchedule.ColorCount];

            foreach (DungeonTrialScheduledRound round in schedule)
            {
                if (round == null || round.Trial == null || !round.Trial.IsValid() || round.Presentation == null)
                {
                    reason = "A scheduled round is invalid.";
                    return false;
                }

                string conditionKey = ConditionKey(round.Presentation);
                if (!conditionCounts.ContainsKey(conditionKey))
                {
                    conditionCounts.Add(conditionKey, 0);
                    conditionTargetCounts.Add(conditionKey, new int[DungeonTrialSchedule.ColorCount]);
                    conditionPositionCounts.Add(conditionKey, new int[DungeonTrialSchedule.ColorCount]);
                }

                conditionCounts[conditionKey]++;
                conditionTargetCounts[conditionKey][round.Trial.TargetColorIndex]++;
                conditionPositionCounts[conditionKey][round.Trial.GetCorrectChoiceIndex()]++;
                totalTargetCounts[round.Trial.TargetColorIndex]++;
                totalPositionCounts[round.Trial.GetCorrectChoiceIndex()]++;
            }

            int expectedTotalPerRole = expectedCount / DungeonTrialSchedule.ColorCount;
            for (int index = 0; index < DungeonTrialSchedule.ColorCount; index++)
            {
                if (totalTargetCounts[index] != expectedTotalPerRole || totalPositionCounts[index] != expectedTotalPerRole)
                {
                    reason = "The run is not balanced across target roles and positions.";
                    return false;
                }
            }

            switch (selectedMode)
            {
                case DungeonTrialGameMode.RawPalettesNormalVision:
                    return ValidateRawPalettes(conditionCounts, conditionTargetCounts, conditionPositionCounts, out reason);
                case DungeonTrialGameMode.RgbThroughCvdMatrices:
                    return ValidateRgbMatrices(conditionCounts, out reason);
                case DungeonTrialGameMode.AlternativePalettesThroughCvdMatrices:
                    return ValidateAlternativeCrossProduct(conditionCounts, conditionTargetCounts, conditionPositionCounts, out reason);
                default:
                    reason = "Unknown game mode.";
                    return false;
            }
        }

        private static List<DungeonTrialScheduledRound> CreateRawPalettes(int seed)
        {
            List<DungeonTrialScheduledRound> schedule = new List<DungeonTrialScheduledRound>(RawPalettesTrialCount);
            AddSixTrialBlock(schedule, new DungeonTrialPresentation(DungeonTrialPalette.Original, DungeonTrialPreview.None));
            foreach (DungeonTrialPalette palette in AlternativePalettes)
                AddSixTrialBlock(schedule, new DungeonTrialPresentation(palette, DungeonTrialPreview.None));
            Shuffle(schedule, seed);
            return schedule;
        }

        private static List<DungeonTrialScheduledRound> CreateRgbThroughMatrices(int seed)
        {
            List<DungeonTrialDefinition> stimuli = CreateTwentyFourStimuli(seed);
            List<DungeonTrialPresentation> presentations = new List<DungeonTrialPresentation>(RgbMatricesTrialCount);
            foreach (DungeonTrialPreview preview in CvdPreviews)
            {
                for (int index = 0; index < 8; index++)
                    presentations.Add(new DungeonTrialPresentation(DungeonTrialPalette.Original, preview));
            }

            Shuffle(presentations, seed ^ 0x5247424D);
            List<DungeonTrialScheduledRound> schedule = new List<DungeonTrialScheduledRound>(RgbMatricesTrialCount);
            for (int index = 0; index < stimuli.Count; index++)
                schedule.Add(new DungeonTrialScheduledRound(stimuli[index], presentations[index]));
            return schedule;
        }

        private static List<DungeonTrialScheduledRound> CreateAlternativeCrossProduct(int seed)
        {
            List<DungeonTrialScheduledRound> schedule = new List<DungeonTrialScheduledRound>(AlternativeCrossTrialCount);
            foreach (DungeonTrialPalette palette in AlternativePalettes)
            {
                foreach (DungeonTrialPreview preview in CvdPreviews)
                {
                    DungeonTrialPresentation presentation = new DungeonTrialPresentation(palette, preview);
                    for (int target = 0; target < DungeonTrialSchedule.ColorCount; target++)
                        schedule.Add(new DungeonTrialScheduledRound(new DungeonTrialDefinition(target, IdentityPermutation), presentation));
                }
            }

            Shuffle(schedule, seed);
            return schedule;
        }

        private static List<DungeonTrialDefinition> CreateTwentyFourStimuli(int seed)
        {
            List<DungeonTrialDefinition> trials = DungeonTrialSchedule.CreateBalanced(seed);
            for (int index = 0; index < SixPermutations.Length; index++)
                trials.Add(new DungeonTrialDefinition(SixTargets[index], SixPermutations[index]));
            Shuffle(trials, seed ^ 0x5354494D);
            return trials;
        }

        private static void AddSixTrialBlock(List<DungeonTrialScheduledRound> schedule, DungeonTrialPresentation presentation)
        {
            for (int index = 0; index < SixPermutations.Length; index++)
                schedule.Add(new DungeonTrialScheduledRound(new DungeonTrialDefinition(SixTargets[index], SixPermutations[index]), presentation));
        }

        private static bool ValidateRawPalettes(
            Dictionary<string, int> counts,
            Dictionary<string, int[]> targetCounts,
            Dictionary<string, int[]> positionCounts,
            out string reason)
        {
            DungeonTrialPalette[] rawPalettes =
            {
                DungeonTrialPalette.Original,
                DungeonTrialPalette.DeutanPalette,
                DungeonTrialPalette.ProtanPalette,
                DungeonTrialPalette.TritanomalyPalette
            };

            foreach (DungeonTrialPalette palette in rawPalettes)
            {
                string key = ConditionKey(new DungeonTrialPresentation(palette, DungeonTrialPreview.None));
                if (!counts.ContainsKey(key) || counts[key] != 6)
                {
                    reason = "Raw mode must contain six rounds for " + palette + ".";
                    return false;
                }

                for (int index = 0; index < DungeonTrialSchedule.ColorCount; index++)
                {
                    if (targetCounts[key][index] != 2 || positionCounts[key][index] != 2)
                    {
                        reason = "Raw mode is not balanced inside " + palette + ".";
                        return false;
                    }
                }
            }

            reason = null;
            return counts.Count == rawPalettes.Length;
        }

        private static bool ValidateRgbMatrices(Dictionary<string, int> counts, out string reason)
        {
            foreach (DungeonTrialPreview preview in CvdPreviews)
            {
                string key = ConditionKey(new DungeonTrialPresentation(DungeonTrialPalette.Original, preview));
                if (!counts.ContainsKey(key) || counts[key] != 8)
                {
                    reason = "RGB-through-matrices mode must contain eight rounds for " + preview + ".";
                    return false;
                }
            }

            reason = null;
            return counts.Count == CvdPreviews.Length;
        }

        private static bool ValidateAlternativeCrossProduct(
            Dictionary<string, int> counts,
            Dictionary<string, int[]> targetCounts,
            Dictionary<string, int[]> positionCounts,
            out string reason)
        {
            foreach (DungeonTrialPalette palette in AlternativePalettes)
            {
                foreach (DungeonTrialPreview preview in CvdPreviews)
                {
                    string key = ConditionKey(new DungeonTrialPresentation(palette, preview));
                    if (!counts.ContainsKey(key) || counts[key] != 3)
                    {
                        reason = "Alternative cross mode must contain three rounds for " + key + ".";
                        return false;
                    }

                    for (int index = 0; index < DungeonTrialSchedule.ColorCount; index++)
                    {
                        if (targetCounts[key][index] != 1 || positionCounts[key][index] != 1)
                        {
                            reason = "Alternative cross mode is not balanced inside " + key + ".";
                            return false;
                        }
                    }
                }
            }

            reason = null;
            return counts.Count == AlternativePalettes.Length * CvdPreviews.Length;
        }

        private static void Shuffle<T>(List<T> items, int seed)
        {
            System.Random random = new System.Random(seed);
            for (int index = items.Count - 1; index > 0; index--)
            {
                int otherIndex = random.Next(index + 1);
                T temporary = items[index];
                items[index] = items[otherIndex];
                items[otherIndex] = temporary;
            }
        }
    }

    
}
