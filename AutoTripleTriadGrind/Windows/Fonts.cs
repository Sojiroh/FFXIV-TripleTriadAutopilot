using System;
using System.IO;
using AutoTripleTriadGrind.Core;
using AutoTripleTriadGrind.Core.Localization;
using AutoTripleTriadGrind.Core.Travel;
using AutoTripleTriadGrind.Core.Triad.Data;
using Dalamud;
using Dalamud.Interface;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

namespace AutoTripleTriadGrind.Windows;

internal static class Fonts
{
    private sealed class NoOpScope : IDisposable
    {
        public void Dispose()
        {
        }
    }

    private const float CaptionScale = 1f;

    private const float BodyScale = 1.125f;

    private const float HeadlineScale = 1.25f;

    private const float TitleScale = 1.625f;

    private const float IconScale = 1.125f;

    private const float IconLargeScale = 1.625f;

    private const float IconDisplayScale = 2.25f;

    private const string LatinFontFile = "NotoSans-Medium-Latin.ttf";

    private const int LatinBlocksEnd = 879;

    private const int LatinAdditionalStart = 7680;

    private const int LatinAdditionalEnd = 7935;

    private const int NotoCjkJapaneseFace = 0;

    private const int NotoCjkSimplifiedChineseFace = 2;

    private static readonly ushort[] LatinBlocks = new ushort[8] { 32, 255, 256, 383, 384, 591, 8192, 8303 };

    private static readonly ushort[] SymbolBlocks = new ushort[4] { 8592, 8703, 8704, 8959 };

    private const ushort QuestSyncGlyph = 57534;

    private static readonly ushort[] QuestSyncRange = new ushort[3] { QuestSyncGlyph, QuestSyncGlyph, 0 };

    private static readonly NoOpScope noOp = new();

    private static IUiBuilder? builder;

    private static IFontAtlas? atlas;

    private static byte[]? latinFont;

    private static ushort[] latinRanges = new ushort[1];

    private static ushort[] mergeRanges = new ushort[1];

    private static int cjkFace = NotoCjkJapaneseFace;

    private static float unitPx = UiBuilder.DefaultFontSizePx;

    private static IFontHandle? body;

    private static IFontHandle? title;

    private static IFontHandle? headline;

    private static IFontHandle? caption;

    private static IFontHandle? icon;

    private static IFontHandle? iconLarge;

    private static IFontHandle? iconDisplay;

    public static void Initialize(IUiBuilder uiBuilder, string pluginDirectory)
    {
        builder = uiBuilder;
        atlas = uiBuilder.FontAtlas;
        latinFont = LoadLatinFont(Path.Combine(pluginDirectory, "Fonts", LatinFontFile));
        RefreshRanges();
        body = TextHandle(BodyScale);
        title = TextHandle(TitleScale);
        headline = TextHandle(HeadlineScale);
        caption = TextHandle(CaptionScale);
        icon = IconHandle(IconScale);
        iconLarge = IconHandle(IconLargeScale);
        iconDisplay = IconHandle(IconDisplayScale);
        uiBuilder.DefaultFontChanged += Rebuild;
        TriadData.DataLoaded += Rebuild;
        if (atlas.AutoRebuildMode == FontAtlasAutoRebuildMode.Disable)
        {
            atlas.BuildFontsAsync();
        }
    }

    public static void OnLanguageChanged()
    {
        Rebuild();
    }

    public static void Dispose()
    {
        if (builder != null)
        {
            builder.DefaultFontChanged -= Rebuild;
        }
        TriadData.DataLoaded -= Rebuild;
        body?.Dispose();
        title?.Dispose();
        headline?.Dispose();
        caption?.Dispose();
        icon?.Dispose();
        iconLarge?.Dispose();
        iconDisplay?.Dispose();
        body = (title = (headline = (caption = (icon = (iconLarge = (iconDisplay = null))))));
        builder = null;
        atlas = null;
        latinFont = null;
    }

    public static IDisposable PushBody()
    {
        return body?.Push() ?? noOp;
    }

    public static IDisposable PushTitle()
    {
        return title?.Push() ?? noOp;
    }

    public static IDisposable PushHeadline()
    {
        return headline?.Push() ?? noOp;
    }

    public static IDisposable PushCaption()
    {
        return caption?.Push() ?? noOp;
    }

    public static IDisposable PushIcon()
    {
        return icon?.Push() ?? ImRaii.PushFont(UiBuilder.IconFont);
    }

    public static IDisposable PushIconLarge()
    {
        return iconLarge?.Push() ?? ImRaii.PushFont(UiBuilder.IconFont);
    }

    public static IDisposable PushIconDisplay()
    {
        return iconDisplay?.Push() ?? ImRaii.PushFont(UiBuilder.IconFont);
    }

    public static IDisposable PushIconFor(float targetHeight)
    {
        var scaledUnit = unitPx * ImGuiHelpers.GlobalScale;
        if (targetHeight > scaledUnit * IconLargeScale)
        {
            return PushIconDisplay();
        }
        if (targetHeight > scaledUnit * IconScale)
        {
            return PushIconLarge();
        }
        return PushIcon();
    }

    private static byte[]? LoadLatinFont(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                return File.ReadAllBytes(path);
            }
            RunLog.Warning("Latin font missing at '" + path + "'; falling back to the Dalamud default font, Latin Extended letters will not render");
        }
        catch (Exception exception)
        {
            RunLog.Error(exception, "Failed to read the Latin font");
        }
        return null;
    }

    private static IFontHandle TextHandle(float scale)
    {
        return atlas!.NewDelegateFontHandle(e =>
        {
            e.OnPreBuild(tk =>
            {
                var sizePx = unitPx * scale;
                var mergeFont = ((latinFont != null) ? tk.AddFontFromMemory(latinFont, new SafeFontConfig
                {
                    SizePx = sizePx,
                    GlyphRanges = latinRanges
                }, LatinFontFile) : tk.AddDalamudDefaultFont(sizePx, latinRanges));
                tk.Font = mergeFont;
                tk.AddDalamudAssetFont(DalamudAsset.LodestoneGameSymbol, new SafeFontConfig
                {
                    SizePx = sizePx,
                    GlyphRanges = QuestSyncRange,
                    MergeFont = mergeFont
                });
                if (mergeRanges.Length <= 1)
                {
                    return;
                }
                tk.AddDalamudAssetFont(DalamudAsset.NotoSansCjkRegular, new SafeFontConfig
                {
                    SizePx = sizePx,
                    GlyphRanges = mergeRanges,
                    MergeFont = mergeFont,
                    FontNo = cjkFace
                });
            });
        });
    }

    private static IFontHandle IconHandle(float scale)
    {
        return atlas!.NewDelegateFontHandle(e =>
        {
            e.OnPreBuild(tk =>
            {
                tk.AddFontAwesomeIconFont(new SafeFontConfig
                {
                    SizePx = unitPx * scale
                });
            });
        });
    }

    private static void Rebuild()
    {
        RefreshRanges();
        if (atlas != null)
        {
            atlas.BuildFontsAsync();
        }
    }

    private static void RefreshRanges()
    {
        unitPx = builder?.DefaultFontSpec.SizePx ?? UiBuilder.DefaultFontSizePx;
        bool[] latinMask = new bool[GlyphRanges.CodepointCount];
        bool[] symbolMask = new bool[GlyphRanges.CodepointCount];
        bool[] extraMask = new bool[GlyphRanges.CodepointCount];
        GlyphRanges.MarkRanges(latinMask, LatinBlocks);
        GlyphRanges.MarkRanges(symbolMask, SymbolBlocks);
        GlyphRanges.MarkRanges(extraMask, Loc.Current.ExtraGlyphRanges);
        GlyphRanges.MarkRanges(extraMask, Loc.CatalogGlyphRanges);
        MarkNativeNames(extraMask);
        MarkGameNames(extraMask);
        for (var i = GlyphRanges.FirstNonAsciiCodepoint; i < GlyphRanges.CodepointCount; i++)
        {
            if (!latinMask[i] && extraMask[i] && i != QuestSyncGlyph)
            {
                if (IsLatinCodepoint(i))
                {
                    latinMask[i] = true;
                }
                else
                {
                    symbolMask[i] = true;
                }
            }
        }
        latinRanges = GlyphRanges.ToRanges(latinMask);
        mergeRanges = GlyphRanges.ToRanges(symbolMask);
        cjkFace = ((Loc.Current == Languages.Chinese) ? NotoCjkSimplifiedChineseFace : NotoCjkJapaneseFace);
    }

    private static bool IsLatinCodepoint(int codepoint)
    {
        if (codepoint > LatinBlocksEnd)
        {
            if (codepoint >= LatinAdditionalStart)
            {
                return codepoint <= LatinAdditionalEnd;
            }
            return false;
        }
        return true;
    }

    private static void MarkNativeNames(bool[] extra)
    {
        LanguageInfo[] languages = Languages.All;
        for (var i = 0; i < languages.Length; i++)
        {
            GlyphRanges.MarkText(extra, languages[i].NativeName);
        }
    }

    private static void MarkGameNames(bool[] extra)
    {
        var triadSet = TriadData.Set;
        MarkTexts(extra, triadSet.NpcNames);
        MarkTexts(extra, triadSet.NpcUnlockQuestNames);
        MarkTexts(extra, triadSet.CardNames);
        MarkTexts(extra, triadSet.RuleNames);
        TriadNpc[] npcList = triadSet.Npcs;
        for (var i = 0; i < npcList.Length; i++)
        {
            GlyphRanges.MarkText(extra, TerritoryNames.Of(npcList[i].TerritoryId));
        }
    }

    private static void MarkTexts(bool[] extra, string[] texts)
    {
        for (var i = 0; i < texts.Length; i++)
        {
            GlyphRanges.MarkText(extra, texts[i]);
        }
    }
}
