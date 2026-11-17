using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace ChromaAccess.DungeonTrial
{
    /// <summary>
    /// Keyboard-only colour-matching demonstrator.
    ///
    /// A selected game mode owns each valid palette/simulation pairing.
    /// Mixed is intentionally a balanced comparative demo, not a clinical
    /// player simulation. Shapes and outline remain independent aids.
    /// </summary>
    public sealed class DungeonTrialManager : MonoBehaviour
    {
        [SerializeField] private DungeonTrialTarget referenceTarget;
        [SerializeField] private DungeonTrialTarget[] choiceTargets;
        [SerializeField] private TextMesh controlsText;
        [SerializeField] private TextMesh statusText;
        [SerializeField] private Color[] originalPalette;
        [SerializeField] private Color[] deutanPalette;
        [SerializeField] private Color[] protanPalette;
        [SerializeField] private Color[] tritanomalyPalette;
        [SerializeField] private Color[] balancedPalette;
        [SerializeField] private float feedbackSeconds = 0.45f;
        [SerializeField] private DungeonTrialGameMode gameMode = DungeonTrialGameMode.RawPalettesNormalVision;
        [SerializeField] private bool shapesEnabled;
        [SerializeField] private bool outlineEnabled;

        private List<DungeonTrialScheduledRound> schedule;
        private List<DungeonTrialResult> recordedResults;
        private DungeonTrialDefinition currentTrial;
        private int[] choiceColorIndexes;
        private int currentRound;
        private int currentTargetColor;
        private int score;
        private int scheduleSeed;
        private int runNumber;
        private bool acceptsInput;
        private double roundShownAt;
        private string runId;
        private string lastResultText;
        private string completedCsvPath;
        private DateTime runStartedUtc;
        private DungeonTrialGameMode runGameMode;
        private DungeonTrialScheduledRound currentScheduledRound;
        private DungeonTrialPresentation currentPresentation;
        private bool runShapesEnabled;
        private bool runOutlineEnabled;
        private bool selectingGameMode;

        public void Configure(
            DungeonTrialTarget reference,
            DungeonTrialTarget[] choices,
            TextMesh configuredControlsText,
            TextMesh configuredStatusText,
            Color[] configuredOriginalPalette,
            Color[] configuredDeutanPalette,
            Color[] configuredProtanPalette,
            Color[] configuredTritanomalyPalette,
            Color[] configuredBalancedPalette)
        {
            referenceTarget = reference;
            choiceTargets = choices;
            controlsText = configuredControlsText;
            statusText = configuredStatusText;
            originalPalette = configuredOriginalPalette;
            deutanPalette = configuredDeutanPalette;
            protanPalette = configuredProtanPalette;
            tritanomalyPalette = configuredTritanomalyPalette;
            balancedPalette = configuredBalancedPalette;
        }

        private void Start()
        {
            EnsurePaletteDefaults();

            if (!HasValidConfiguration())
            {
                enabled = false;
                return;
            }

            scheduleSeed = CreateScheduleSeed();
            OpenGameModeSelector(false);
        }

        /// <summary>
        /// Called only by the editor scene setup tool. The scene owns the HUD
        /// objects and their formatting; runtime only changes statusText.text.
        /// </summary>
        public void ConfigureHudTexts(TextMesh configuredControlsText, TextMesh configuredStatusText)
        {
            controlsText = configuredControlsText;
            statusText = configuredStatusText;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.M))
            {
                OpenGameModeSelector(true);
                return;
            }

            if (Input.GetKeyDown(KeyCode.F1))
            {
                SelectGameMode(DungeonTrialGameMode.RawPalettesNormalVision);
                return;
            }

            if (Input.GetKeyDown(KeyCode.F2))
            {
                SelectGameMode(DungeonTrialGameMode.RgbThroughCvdMatrices);
                return;
            }

            if (Input.GetKeyDown(KeyCode.F3))
            {
                SelectGameMode(DungeonTrialGameMode.AlternativePalettesThroughCvdMatrices);
                return;
            }

            if (Input.GetKeyDown(KeyCode.F5))
            {
                ToggleShapes();
                return;
            }

            if (Input.GetKeyDown(KeyCode.F6))
            {
                ToggleOutline();
                return;
            }

            if (selectingGameMode)
            {
                if (IsChoiceKeyPressed(KeyCode.Alpha1, KeyCode.Keypad1))
                    SelectGameMode(DungeonTrialGameMode.RawPalettesNormalVision);
                else if (IsChoiceKeyPressed(KeyCode.Alpha2, KeyCode.Keypad2))
                    SelectGameMode(DungeonTrialGameMode.RgbThroughCvdMatrices);
                else if (IsChoiceKeyPressed(KeyCode.Alpha3, KeyCode.Keypad3))
                    SelectGameMode(DungeonTrialGameMode.AlternativePalettesThroughCvdMatrices);
                return;
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                StartNewRun();
                return;
            }

            if (!acceptsInput)
                return;

            if (IsChoiceKeyPressed(KeyCode.Alpha1, KeyCode.Keypad1))
                SelectChoice(0);
            else if (IsChoiceKeyPressed(KeyCode.Alpha2, KeyCode.Keypad2))
                SelectChoice(1);
            else if (IsChoiceKeyPressed(KeyCode.Alpha3, KeyCode.Keypad3))
                SelectChoice(2);
        }

        private static bool IsChoiceKeyPressed(KeyCode numberKey, KeyCode keypadKey)
        {
            return Input.GetKeyDown(numberKey) || Input.GetKeyDown(keypadKey);
        }

        private void SelectGameMode(DungeonTrialGameMode requestedMode)
        {
            gameMode = requestedMode;
            schedule = DungeonTrialGameModeSchedule.Create(gameMode, scheduleSeed);
            if (!DungeonTrialGameModeSchedule.TryValidate(schedule, gameMode, out string reason))
            {
                FailRun("Programul de trialuri este invalid: " + reason);
                return;
            }

            selectingGameMode = false;
            StartNewRun();
            Debug.Log("[DungeonTrial] Game mode: " + requestedMode + ". " + StateLabel());
        }

        private void OpenGameModeSelector(bool discardCurrentRun)
        {
            StopAllCoroutines();
            if (discardCurrentRun)
                ReportDiscardedPartialRun();

            recordedResults = null;
            acceptsInput = false;
            currentTrial = null;
            currentScheduledRound = null;
            currentPresentation = null;
            selectingGameMode = true;
            ApplyPresentationFlags(DungeonTrialPreview.None);
            RefreshModeSelectorText();
        }

        private void ToggleShapes()
        {
            shapesEnabled = !shapesEnabled;
            if (selectingGameMode)
                RefreshModeSelectorText();
            else
                StartNewRun();
            Debug.Log("[DungeonTrial] Shapes toggled. " + StateLabel());
        }

        private void ToggleOutline()
        {
            outlineEnabled = !outlineEnabled;
            if (selectingGameMode)
                RefreshModeSelectorText();
            else
                StartNewRun();
            Debug.Log("[DungeonTrial] Outline toggled. " + StateLabel());
        }

        private void StartNewRun()
        {
            StopAllCoroutines();
            ReportDiscardedPartialRun();

            if (schedule == null)
            {
                FailRun("Programul de trialuri este null.");
                return;
            }

            if (!DungeonTrialGameModeSchedule.TryValidate(schedule, gameMode, out string reason))
            {
                FailRun("Programul de trialuri este invalid: " + reason);
                return;
            }

            runGameMode = gameMode;
            runShapesEnabled = shapesEnabled;
            runOutlineEnabled = outlineEnabled;

            runStartedUtc = DateTime.UtcNow;
            runNumber++;
            runId = runStartedUtc.ToString("yyyyMMdd_HHmmss_fff", CultureInfo.InvariantCulture)
                + "_seed" + scheduleSeed.ToString(CultureInfo.InvariantCulture)
                + "_run" + runNumber.ToString("D2", CultureInfo.InvariantCulture);
            recordedResults = new List<DungeonTrialResult>(schedule.Count);
            currentRound = 0;
            score = 0;
            currentTrial = null;
            currentScheduledRound = null;
            currentPresentation = null;
            choiceColorIndexes = null;
            acceptsInput = false;
            lastResultText = null;
            completedCsvPath = null;

            Debug.Log("[DungeonTrial] Started run " + runId + " with " + schedule.Count
                + " trials. " + StateLabel());
            BeginRound();
        }

        private void ReportDiscardedPartialRun()
        {
            if (recordedResults == null || recordedResults.Count == 0 || schedule == null)
                return;

            if (recordedResults.Count < schedule.Count)
            {
                Debug.Log("[DungeonTrial] Discarded incomplete run " + runId + " ("
                    + recordedResults.Count + "/" + schedule.Count + " answers).");
            }
        }

        private void BeginRound()
        {
            if (currentRound >= schedule.Count)
            {
                FinishRun();
                return;
            }

            currentScheduledRound = schedule[currentRound];
            currentTrial = currentScheduledRound.Trial;
            if (!TryValidateRound(currentTrial, out string reason))
            {
                FailRun(reason);
                return;
            }

            currentPresentation = currentScheduledRound.Presentation;
            Color[] activePalette = GetActivePalette(currentPresentation.Palette);
            currentTargetColor = currentTrial.TargetColorIndex;
            ApplyPresentationFlags(currentPresentation.Preview);
            choiceColorIndexes = new int[choiceTargets.Length];

            for (int choiceIndex = 0; choiceIndex < choiceTargets.Length; choiceIndex++)
            {
                int colorIndex = currentTrial.GetChoiceColorIndex(choiceIndex);
                choiceColorIndexes[choiceIndex] = colorIndex;
                choiceTargets[choiceIndex].ResetSelectionFeedback();
                choiceTargets[choiceIndex].SetColor(activePalette[colorIndex]);
                choiceTargets[choiceIndex].SetCue(colorIndex, runShapesEnabled);
                choiceTargets[choiceIndex].SetOutline(runOutlineEnabled);
            }

            referenceTarget.ResetSelectionFeedback();
            referenceTarget.SetColor(activePalette[currentTargetColor]);
            referenceTarget.SetCue(currentTargetColor, runShapesEnabled);
            referenceTarget.SetOutline(runOutlineEnabled);

            acceptsInput = false;
            RefreshStatusText("Se afișează runda…");
            StartCoroutine(AcceptInputAfterFirstDisplayedFrame());
        }

        private IEnumerator AcceptInputAfterFirstDisplayedFrame()
        {
            yield return new WaitForEndOfFrame();
            roundShownAt = Time.realtimeSinceStartupAsDouble;
            acceptsInput = true;
            RefreshStatusText(null);
        }

        private void ApplyPresentationFlags(DungeonTrialPreview requestedPreview)
        {
            referenceTarget.SetPreview(requestedPreview);
            foreach (DungeonTrialTarget choice in choiceTargets)
                choice.SetPreview(requestedPreview);
        }

        private void SelectChoice(int choiceIndex)
        {
            if (!acceptsInput || choiceColorIndexes == null || choiceIndex < 0 || choiceIndex >= choiceTargets.Length)
                return;

            acceptsInput = false;
            int selectedColorIndex = choiceColorIndexes[choiceIndex];
            int correctChoiceIndex = currentTrial.GetCorrectChoiceIndex();
            bool correct = choiceIndex == correctChoiceIndex;
            double responseTimeMilliseconds = Math.Max(
                0d,
                (Time.realtimeSinceStartupAsDouble - roundShownAt) * 1000d);

            DungeonTrialResult result = new DungeonTrialResult
            {
                SessionId = runId,
                SessionSeed = scheduleSeed,
                TrialNumber = currentRound + 1,
                GameMode = runGameMode,
                RoundCondition = DungeonTrialGameModeSchedule.ConditionKey(currentPresentation),
                ConditionLabel = BuildConditionLabel(
                    runGameMode,
                    DungeonTrialGameModeSchedule.ConditionKey(currentPresentation),
                    currentPresentation.Palette,
                    currentPresentation.Preview,
                    runShapesEnabled,
                    runOutlineEnabled),
                Palette = currentPresentation.Palette,
                PreviewModel = currentPresentation.Preview,
                ShapesEnabled = runShapesEnabled,
                OutlineEnabled = runOutlineEnabled,
                TargetColorIndex = currentTargetColor,
                CorrectPosition = correctChoiceIndex + 1,
                Choice1ColorIndex = choiceColorIndexes[0],
                Choice2ColorIndex = choiceColorIndexes[1],
                Choice3ColorIndex = choiceColorIndexes[2],
                SelectedPosition = choiceIndex + 1,
                SelectedColorIndex = selectedColorIndex,
                Correct = correct,
                ResponseTimeMilliseconds = responseTimeMilliseconds,
                RunStartedUtc = runStartedUtc,
                TimestampUtc = DateTime.UtcNow
            };

            if (!TryValidateResult(result, out string reason))
            {
                FailRun(reason);
                return;
            }

            recordedResults.Add(result);
            if (correct)
                score++;

            choiceTargets[choiceIndex].ShowSelectionFeedback(correct);
            lastResultText = (correct ? "CORECT" : "GREȘIT")
                + " · " + responseTimeMilliseconds.ToString("0", CultureInfo.InvariantCulture) + " ms";
            RefreshStatusText(null);
            Debug.Log("[DungeonTrial] Trial " + result.TrialNumber + ": key " + result.SelectedPosition
                + ", " + (correct ? "correct" : "wrong") + ", "
                + result.ResponseTimeMilliseconds.ToString("0.000", CultureInfo.InvariantCulture) + " ms.");
            StartCoroutine(AdvanceAfterFeedback());
        }

        private IEnumerator AdvanceAfterFeedback()
        {
            yield return new WaitForSeconds(feedbackSeconds);
            currentRound++;
            BeginRound();
        }

        private void FinishRun()
        {
            acceptsInput = false;
            if (!TryValidateCompletedRun(out string reason))
            {
                FailRun(reason);
                return;
            }

            if (!TryExportResults(out string path, out reason))
            {
                FailRun("Nu s-a putut salva CSV-ul: " + reason);
                return;
            }

            completedCsvPath = path;
            lastResultText = "FINAL: CSV salvat";
            RefreshStatusText(null);
            Debug.Log("[DungeonTrial] Finished " + runId + ". Score " + score + "/"
                + schedule.Count + ". CSV: " + completedCsvPath);
        }

        private bool TryValidateRound(DungeonTrialDefinition trial, out string reason)
        {
            if (trial == null || !trial.IsValid())
            {
                reason = "Trialul curent nu este o permutare validă a celor trei roluri de culoare.";
                return false;
            }

            int correctChoiceIndex = trial.GetCorrectChoiceIndex();
            if (correctChoiceIndex < 0 || correctChoiceIndex >= choiceTargets.Length)
            {
                reason = "Trialul curent nu are o poziție corectă validă.";
                return false;
            }

            reason = null;
            return true;
        }

        private bool TryValidateResult(DungeonTrialResult result, out string reason)
        {
            if (result.SelectedPosition < 1 || result.SelectedPosition > choiceColorIndexes.Length)
            {
                reason = "Poziția selectată nu este validă.";
                return false;
            }

            if (result.TrialNumber != currentRound + 1)
            {
                reason = "Numărul trialului înregistrat nu corespunde rundei active.";
                return false;
            }

            if (result.CorrectPosition != currentTrial.GetCorrectChoiceIndex() + 1)
            {
                reason = "Poziția corectă înregistrată nu corespunde programului trialului.";
                return false;
            }

            if (result.SelectedColorIndex != choiceColorIndexes[result.SelectedPosition - 1])
            {
                reason = "Culoarea selectată nu corespunde poziției selectate.";
                return false;
            }

            if (result.Correct != (result.SelectedPosition == result.CorrectPosition))
            {
                reason = "Corectitudinea răspunsului nu corespunde poziției corecte.";
                return false;
            }

            if (result.ResponseTimeMilliseconds < 0d)
            {
                reason = "Timpul de răspuns nu poate fi negativ.";
                return false;
            }

            if (currentScheduledRound == null || currentPresentation == null
                || result.GameMode != runGameMode
                || result.RoundCondition != DungeonTrialGameModeSchedule.ConditionKey(currentPresentation)
                || result.Palette != currentPresentation.Palette
                || result.PreviewModel != currentPresentation.Preview)
            {
                reason = "Rezultatul nu corespunde modului vizual programat pentru rundă.";
                return false;
            }

            reason = null;
            return true;
        }

        private bool TryValidateCompletedRun(out string reason)
        {
            if (recordedResults == null || recordedResults.Count != schedule.Count)
            {
                reason = "O sesiune completă trebuie să conțină exact " + schedule.Count + " răspunsuri.";
                return false;
            }

            for (int index = 0; index < recordedResults.Count; index++)
            {
                DungeonTrialResult result = recordedResults[index];
                if (result == null)
                {
                    reason = "CSV-ul ar conține un rezultat null.";
                    return false;
                }

                if (result.SessionId != runId || result.SessionSeed != scheduleSeed)
                {
                    reason = "CSV-ul ar conține răspunsuri dintr-o altă sesiune.";
                    return false;
                }

                if (result.TrialNumber != index + 1 || result.ResponseTimeMilliseconds < 0d)
                {
                    reason = "CSV-ul ar conține un index de trial sau un timp invalid.";
                    return false;
                }

                DungeonTrialScheduledRound expectedRound = schedule[index];
                DungeonTrialPresentation expectedPresentation = expectedRound.Presentation;
                string expectedCondition = BuildConditionLabel(
                    runGameMode,
                    DungeonTrialGameModeSchedule.ConditionKey(expectedPresentation),
                    expectedPresentation.Palette,
                    expectedPresentation.Preview,
                    runShapesEnabled,
                    runOutlineEnabled);
                if (result.GameMode != runGameMode
                    || result.RoundCondition != DungeonTrialGameModeSchedule.ConditionKey(expectedPresentation)
                    || result.ConditionLabel != expectedCondition
                    || result.Palette != expectedPresentation.Palette
                    || result.PreviewModel != expectedPresentation.Preview
                    || result.ShapesEnabled != runShapesEnabled
                    || result.OutlineEnabled != runOutlineEnabled)
                {
                    reason = "CSV-ul nu corespunde condiției programate pentru fiecare rundă.";
                    return false;
                }
            }

            reason = null;
            return true;
        }

        private bool TryExportResults(out string path, out string reason)
        {
            path = null;
            reason = null;

            try
            {
                string directory = GetResultsDirectory();
                Directory.CreateDirectory(directory);
                path = Path.Combine(directory, "dungeon_trial_" + runId + ".csv");

                StringBuilder csv = new StringBuilder();
                csv.AppendLine(DungeonTrialCsv.Header);
                foreach (DungeonTrialResult result in recordedResults)
                    csv.AppendLine(DungeonTrialCsv.Format(result));

                File.WriteAllText(path, csv.ToString(), new UTF8Encoding(false));
                return true;
            }
            catch (Exception exception)
            {
                reason = exception.Message;
                return false;
            }
        }

        private static string GetResultsDirectory()
        {
#if UNITY_EDITOR
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(projectRoot, "docs", "results");
#else
            return Path.Combine(Application.persistentDataPath, "ChromaAccessResults");
#endif
        }

        private void RefreshStatusText(string temporaryMessage)
        {
            if (selectingGameMode)
            {
                RefreshModeSelectorText();
                return;
            }

            if (statusText == null || schedule == null || currentScheduledRound == null || currentPresentation == null)
                return;

            string roundOrFinal = completedCsvPath == null
                ? "RUNDA " + Math.Min(currentRound + 1, schedule.Count) + "/" + schedule.Count
                    + " · SCOR " + score + "/" + schedule.Count
                : "FINAL " + score + "/" + schedule.Count
                    + " · CSV: " + Path.GetFileName(completedCsvPath);
            string message = temporaryMessage ?? lastResultText ?? roundOrFinal;

            statusText.text =
                "MOD ACTIV: " + GameModeLabel(runGameMode)
                + "\nRUNDĂ: " + RoundDescription(currentPresentation)
                + "\nALEGE FIOLA: [1] STÂNGA   [2] CENTRU   [3] DREAPTA"
                + "\nAJUTOARE: [F5] FORME " + OnOff(runShapesEnabled) + "   [F6] CONTUR " + OnOff(runOutlineEnabled)
                + "\n[M] SCHIMBĂ MODUL   [R] REIA RUNUL"
                + "\n" + roundOrFinal + (message == roundOrFinal ? string.Empty : " · " + message);
        }

        private void RefreshModeSelectorText()
        {
            if (statusText == null)
                return;

            statusText.text =
                "ALEGE PERSPECTIVA DE JOC\n"
                + "[1] PALETE RAW — RGB + PALETE ALTERNATIVE, FĂRĂ FILTRU\n"
                + "[2] RGB PRIN MATRICE — RGB VĂZUT PRIN CELE 3 AFECȚIUNI\n"
                + "[3] PALETE ALTERNATIVE PRIN MATRICE — TOATE CELE 9 COMBINAȚII\n"
                + "MODUL 3 ARE 27 RUNDE: 3 PALETE × 3 MATRICE × 3 ȚINTE\n"
                + "DUPĂ ALEGERE: [1] [2] [3] ALEG FIOLA\n"
                + "AJUTOARE: [F5] FORME " + OnOff(shapesEnabled) + "   [F6] CONTUR " + OnOff(outlineEnabled);
        }

        private void EnsurePaletteDefaults()
        {
            originalPalette = EnsurePalette(originalPalette, DungeonTrialPalette.Original);
            deutanPalette = EnsurePalette(deutanPalette, DungeonTrialPalette.DeutanPalette);
            protanPalette = EnsurePalette(protanPalette, DungeonTrialPalette.ProtanPalette);
            tritanomalyPalette = EnsurePalette(tritanomalyPalette, DungeonTrialPalette.TritanomalyPalette);
            balancedPalette = EnsurePalette(balancedPalette, DungeonTrialPalette.BalancedPalette);
        }

        private static Color[] EnsurePalette(Color[] configuredPalette, DungeonTrialPalette fallback)
        {
            return configuredPalette != null && configuredPalette.Length == DungeonTrialSchedule.ColorCount
                ? configuredPalette
                : DungeonTrialPaletteLibrary.Create(fallback);
        }

        private Color[] GetActivePalette(DungeonTrialPalette palette)
        {
            switch (palette)
            {
                case DungeonTrialPalette.Original:
                    return originalPalette;
                case DungeonTrialPalette.DeutanPalette:
                    return deutanPalette;
                case DungeonTrialPalette.ProtanPalette:
                    return protanPalette;
                case DungeonTrialPalette.TritanomalyPalette:
                    return tritanomalyPalette;
                case DungeonTrialPalette.BalancedPalette:
                    return balancedPalette;
                default:
                    throw new ArgumentOutOfRangeException(nameof(palette));
            }
        }

        private void FailRun(string message)
        {
            StopAllCoroutines();
            acceptsInput = false;
            lastResultText = "EROARE: " + message;
            RefreshStatusText(null);
            Debug.LogError("[DungeonTrial] " + message);
        }

        private bool HasValidConfiguration()
        {
            bool isValid = referenceTarget != null
                && choiceTargets != null
                && choiceTargets.Length == DungeonTrialSchedule.ColorCount
                && originalPalette != null
                && originalPalette.Length == DungeonTrialSchedule.ColorCount
                && deutanPalette != null
                && deutanPalette.Length == DungeonTrialSchedule.ColorCount
                && protanPalette != null
                && protanPalette.Length == DungeonTrialSchedule.ColorCount
                && tritanomalyPalette != null
                && tritanomalyPalette.Length == DungeonTrialSchedule.ColorCount
                && balancedPalette != null
                && balancedPalette.Length == DungeonTrialSchedule.ColorCount;

            if (!isValid)
                Debug.LogError("[DungeonTrial] The scene has an incomplete trial configuration.");

            return isValid;
        }

        private static int CreateScheduleSeed()
        {
            long ticks = DateTime.UtcNow.Ticks;
            int seed = unchecked((int)(ticks ^ (ticks >> 32)));
            return seed == int.MinValue ? 0 : Math.Abs(seed);
        }

        private static string BuildConditionLabel(
            DungeonTrialGameMode gameMode,
            string roundCondition,
            DungeonTrialPalette palette,
            DungeonTrialPreview preview,
            bool shapes,
            bool outline)
        {
            return "game_mode=" + GameModeKey(gameMode)
                + ";round_condition=" + roundCondition
                + ";palette=" + PaletteKey(palette)
                + ";preview=" + PreviewKey(preview)
                + ";shapes=" + (shapes ? "1" : "0")
                + ";outline=" + (outline ? "1" : "0");
        }

        private string StateLabel()
        {
            return "game_mode=" + GameModeKey(gameMode)
                + ";shapes=" + (shapesEnabled ? "1" : "0")
                + ";outline=" + (outlineEnabled ? "1" : "0");
        }

        private static string GameModeKey(DungeonTrialGameMode mode)
        {
            switch (mode)
            {
                case DungeonTrialGameMode.RawPalettesNormalVision: return "raw_palettes_normal_vision";
                case DungeonTrialGameMode.RgbThroughCvdMatrices: return "rgb_through_cvd_matrices";
                case DungeonTrialGameMode.AlternativePalettesThroughCvdMatrices: return "alternative_palettes_through_cvd_matrices";
                default: throw new ArgumentOutOfRangeException(nameof(mode));
            }
        }

        private static string GameModeLabel(DungeonTrialGameMode mode)
        {
            switch (mode)
            {
                case DungeonTrialGameMode.RawPalettesNormalVision: return "PALETE RAW · VEDERE NORMALĂ";
                case DungeonTrialGameMode.RgbThroughCvdMatrices: return "RGB PRIN MATRICE DE VEDERE";
                case DungeonTrialGameMode.AlternativePalettesThroughCvdMatrices: return "PALETE ALTERNATIVE PRIN MATRICE";
                default: throw new ArgumentOutOfRangeException(nameof(mode));
            }
        }

        private static string RoundDescription(DungeonTrialPresentation presentation)
        {
            return "PALETĂ " + PaletteLabel(presentation.Palette)
                + " · FILTRU " + PreviewLabel(presentation.Preview);
        }

        private static string PaletteKey(DungeonTrialPalette palette)
        {
            switch (palette)
            {
                case DungeonTrialPalette.Original: return "original";
                case DungeonTrialPalette.DeutanPalette: return "deutan_candidate";
                case DungeonTrialPalette.ProtanPalette: return "protan_candidate";
                case DungeonTrialPalette.TritanomalyPalette: return "tritanomaly_candidate";
                case DungeonTrialPalette.BalancedPalette: return "balanced_candidate";
                default: throw new ArgumentOutOfRangeException(nameof(palette));
            }
        }

        private static string PreviewKey(DungeonTrialPreview preview)
        {
            switch (preview)
            {
                case DungeonTrialPreview.None: return "none";
                case DungeonTrialPreview.Deuteranopia: return "deuteranopia";
                case DungeonTrialPreview.Protanopia: return "protanopia";
                case DungeonTrialPreview.TritanomalyApproximation: return "tritanomaly_approximation";
                default: throw new ArgumentOutOfRangeException(nameof(preview));
            }
        }

        private static string PaletteLabel(DungeonTrialPalette palette)
        {
            switch (palette)
            {
                case DungeonTrialPalette.Original: return "ORIGINALĂ";
                case DungeonTrialPalette.DeutanPalette: return "DEUTAN (EXP.)";
                case DungeonTrialPalette.ProtanPalette: return "PROTAN (EXP.)";
                case DungeonTrialPalette.TritanomalyPalette: return "TRITAN (EXP.)";
                case DungeonTrialPalette.BalancedPalette: return "BALANCED (EXP.)";
                default: throw new ArgumentOutOfRangeException(nameof(palette));
            }
        }

        private static string PreviewLabel(DungeonTrialPreview preview)
        {
            switch (preview)
            {
                case DungeonTrialPreview.None: return "OPRIT";
                case DungeonTrialPreview.Deuteranopia: return "DEUTAN";
                case DungeonTrialPreview.Protanopia: return "PROTAN";
                case DungeonTrialPreview.TritanomalyApproximation: return "TRITANOMALIE (APROX.)";
                default: throw new ArgumentOutOfRangeException(nameof(preview));
            }
        }

        private static string OnOff(bool value)
        {
            return value ? "ON" : "OFF";
        }
    }
}
