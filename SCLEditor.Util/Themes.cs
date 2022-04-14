using System.Reactive.Subjects;
using MudBlazor;

namespace Reductech.Utilities.SCLEditor.Util;

/// <summary>
/// MudBlazor Themes
/// </summary>
public static class Themes
{
    //Reductech Color

    //language=css prefix=body{color: postfix=}
    public static string Color2 = "#a9cabf";

    //language=css prefix=body{color: postfix=}
    public static string Color1 = "#c85a16";

    //language=css prefix=body{color: postfix=}
    public static string Color3 = "#efa500";

    //language=css prefix=body{color: postfix=}
    public static string Color4 = "#244b5c";

    /// <summary>
    /// Whether it is Dark Mode
    /// </summary>
    public static BehaviorSubject<bool> IsDarkMode { get; set; } = new(false);

    /// <summary>
    /// The Default Theme
    /// </summary>
    public static readonly MudTheme DefaultTheme = new()
    {
        Palette = new()
        {
            Primary = Color1, Secondary = Color2, Tertiary = Color3, AppbarBackground = Color4,
        }
    };

    public static MudTheme CurrentTheme => IsDarkMode.Value ? DarkTheme : DefaultTheme;

    /// <summary>
    /// The Dark Mode Theme
    /// </summary>
    public static readonly MudTheme DarkTheme = new()
    {
        Palette = new Palette()
        {
            Primary                  = Color1,
            Secondary                = Color2,
            Tertiary                 = Color3,
            Black                    = Color4,
            Background               = "#32333d",
            BackgroundGrey           = "#27272f",
            Surface                  = "#373740",
            DrawerBackground         = "#27272f",
            DrawerText               = "rgba(255,255,255, 0.50)",
            DrawerIcon               = "rgba(255,255,255, 0.50)",
            AppbarBackground         = Color4,
            AppbarText               = "rgba(255,255,255, 0.70)",
            TextPrimary              = "rgba(255,255,255, 0.70)",
            TextSecondary            = "rgba(255,255,255, 0.50)",
            ActionDefault            = "#adadb1",
            ActionDisabled           = "rgba(255,255,255, 0.26)",
            ActionDisabledBackground = "rgba(255,255,255, 0.12)",
            Divider                  = "rgba(255,255,255, 0.12)",
            DividerLight             = "rgba(255,255,255, 0.06)",
            TableLines               = "rgba(255,255,255, 0.12)",
            LinesDefault             = "rgba(255,255,255, 0.12)",
            LinesInputs              = "rgba(255,255,255, 0.3)",
            TextDisabled             = "rgba(255,255,255, 0.2)"
        }
    };
}
