using AutoTripleTriadGrind.Core.Localization;
using AutoTripleTriadGrind.Windows.Components;

namespace AutoTripleTriadGrind.Windows.Sections.Config;

internal static class GeneralSettings
{
    public static void Draw(Configuration configuration)
    {
        DrawLanguageGroup(configuration);
        DrawWindowGroup(configuration);
        DrawBehaviorGroup(configuration);
        PostRunSettings.Draw(configuration);
    }

    private static void DrawLanguageGroup(Configuration configuration)
    {
        using (SettingsGroup.Begin(Loc.T(L.Settings.Language)))
        {
            SettingsRow.Draw(Loc.T(L.Settings.Language), Loc.T(L.Settings.LanguageHelp), SettingsControls.RowComboWidth, () =>
            {
                SettingsControls.DrawLanguageCombo(configuration);
            });
        }
    }

    private static void DrawWindowGroup(Configuration configuration)
    {
        using (SettingsGroup.Begin(Loc.T(L.Settings.GeneralWindow)))
        {
            SettingsRow.Draw(Loc.T(L.Settings.OpenOnLogin), Loc.T(L.Settings.OpenOnLoginHelp), SettingsControls.ToggleWidth, () =>
            {
                SettingsControls.DrawToggle(configuration, () => configuration.AutoShowOnLogin, (bool value) =>
                {
                    configuration.AutoShowOnLogin = value;
                }, "##tta_general_autoshow");
            }, SettingsRow.ToggleHeight);
            SettingsRow.Draw(Loc.T(L.Settings.DragAnywhere), Loc.T(L.Settings.DragAnywhereHelp), SettingsControls.ToggleWidth, () =>
            {
                SettingsControls.DrawToggle(configuration, () => configuration.DragWindowFromAnywhere, (bool value) =>
                {
                    configuration.DragWindowFromAnywhere = value;
                }, "##tta_general_draganywhere");
            }, SettingsRow.ToggleHeight);
        }
    }

    private static void DrawBehaviorGroup(Configuration configuration)
    {
        using (SettingsGroup.Begin(Loc.T(L.Settings.GeneralBehavior)))
        {
            SettingsRow.Draw(Loc.T(L.Settings.AutoPause), Loc.T(L.Settings.AutoPauseHelp), SettingsControls.ToggleWidth, () =>
            {
                SettingsControls.DrawToggle(configuration, () => configuration.AutoPauseInContent, (bool value) =>
                {
                    configuration.AutoPauseInContent = value;
                }, "##tta_general_autopause");
            }, SettingsRow.ToggleHeight);
            SettingsRow.Draw(Loc.T(L.Session.AutoResume), Loc.T(L.Session.AutoResumeHelp), SettingsControls.ToggleWidth, () =>
            {
                SettingsControls.DrawToggle(configuration, () => configuration.AutoResumeOnFault, (bool value) =>
                {
                    configuration.AutoResumeOnFault = value;
                }, "##tta_general_autoresume");
            }, SettingsRow.ToggleHeight);
        }
    }
}
