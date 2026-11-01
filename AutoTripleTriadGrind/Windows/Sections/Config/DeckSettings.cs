using System;
using AutoTripleTriadGrind.Core.Localization;
using AutoTripleTriadGrind.Windows.Components;

namespace AutoTripleTriadGrind.Windows.Sections.Config;

internal static class DeckSettings
{
    private const int DeckSlots = 5;

    private static readonly SettingsControls.Choices.Choice[] sources = new SettingsControls.Choices.Choice[2]
    {
        new SettingsControls.Choices.Choice(L.Deck.SourceOptimized, L.Deck.SourceOptimizedDetail),
        new SettingsControls.Choices.Choice(L.Deck.SourceOwn, L.Deck.SourceOwnDetail)
    };

    private static readonly string[] slotLabels = BuildSlotLabels();

    public static void Draw(Configuration configuration)
    {
        DrawSourceGroup(configuration);
        if (configuration.DeckSource == DeckSource.Optimized)
        {
            DrawOptimizerGroup(configuration);
        }
    }

    private static void DrawSourceGroup(Configuration configuration)
    {
        using (SettingsGroup.Begin(Loc.T(L.Deck.GroupSource)))
        {
            SettingsRow.Draw(Loc.T(L.Deck.Source), Loc.T(L.Deck.SourceHelp), SettingsControls.RowComboWidth, () =>
            {
                SettingsControls.Choices.DrawCombo("##tta_deck_source", sources, (int)configuration.DeckSource, (int index) =>
                {
                    configuration.DeckSource = (DeckSource)index;
                    configuration.SaveDebounced();
                });
            });
            if (configuration.DeckSource == DeckSource.OwnDeck)
            {
                SettingsRow.Draw(Loc.T(L.Deck.OwnSlot), Loc.T(L.Deck.OwnSlotHelp), SettingsControls.RowComboWidth, () =>
                {
                    DrawSlotCombo(configuration, "##tta_deck_own_slot", configuration.OwnDeckSlot, (int slot) =>
                    {
                        configuration.OwnDeckSlot = slot;
                    });
                });
                return;
            }
            SettingsRow.Draw(Loc.T(L.Deck.WriteSlot), Loc.T(L.Deck.WriteSlotHelp), SettingsControls.RowComboWidth, () =>
            {
                DrawSlotCombo(configuration, "##tta_deck_write_slot", configuration.OptimizedDeckSlot, (int slot) =>
                {
                    configuration.OptimizedDeckSlot = slot;
                });
            });
            SettingsGroup.Footnote(Loc.T(L.Deck.WriteSlotWarning));
        }
    }

    private static void DrawOptimizerGroup(Configuration configuration)
    {
        using (SettingsGroup.Begin(Loc.T(L.Deck.GroupOptimizer)))
        {
            SettingsRow.Draw(Loc.T(L.Deck.Timeout), Loc.T(L.Deck.TimeoutHelp), SettingsControls.RowSliderWidth, () =>
            {
                SettingsControls.DrawIntSlider(configuration, "##tta_deck_timeout", () => configuration.OptimizerTimeoutSeconds, (int value) =>
                {
                    configuration.OptimizerTimeoutSeconds = value;
                }, 10, 300, Loc.T(L.Deck.SecondsFormat));
            });
            SettingsRow.Draw(Loc.T(L.Deck.Threads), Loc.T(L.Deck.ThreadsHelp), SettingsControls.RowSliderWidth, () =>
            {
                SettingsControls.DrawIntSlider(configuration, "##tta_deck_threads", () => configuration.OptimizerThreads, (int value) =>
                {
                    configuration.OptimizerThreads = value;
                }, 0, Environment.ProcessorCount, (configuration.OptimizerThreads == 0) ? Loc.T(L.Deck.ThreadsAuto) : "%d");
            });
        }
    }

    private static void DrawSlotCombo(Configuration configuration, string id, int current, Action<int> apply)
    {
        var index = Math.Clamp(current, 0, DeckSlots - 1);
        if (!SettingsControls.DrawPlainCombo(id, ref index, slotLabels, SettingsControls.RowComboWidth))
        {
            return;
        }
        apply(index);
        configuration.SaveDebounced();
    }

    private static string[] BuildSlotLabels()
    {
        var labels = new string[DeckSlots];
        for (var i = 0; i < DeckSlots; i++)
        {
            labels[i] = Loc.T(L.Deck.SlotLabel, i + 1);
        }
        return labels;
    }
}
