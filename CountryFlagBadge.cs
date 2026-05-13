using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;

namespace RemoSystemProfiler;

public sealed class CountryFlagBadge : Control
{
    public static readonly StyledProperty<string?> CountryCodeProperty =
        AvaloniaProperty.Register<CountryFlagBadge, string?>(nameof(CountryCode));

    public static readonly StyledProperty<IBrush?> TextBrushProperty =
        AvaloniaProperty.Register<CountryFlagBadge, IBrush?>(nameof(TextBrush));

    private static readonly IBrush White = Brushes.White;
    private static readonly IBrush Black = Brushes.Black;
    private static readonly IBrush JapanRed = new SolidColorBrush(Color.FromRgb(188, 0, 45));
    private static readonly IBrush ChinaRed = new SolidColorBrush(Color.FromRgb(222, 41, 16));
    private static readonly IBrush ChinaYellow = new SolidColorBrush(Color.FromRgb(255, 222, 0));
    private static readonly IBrush UsRed = new SolidColorBrush(Color.FromRgb(178, 34, 52));
    private static readonly IBrush UsBlue = new SolidColorBrush(Color.FromRgb(60, 59, 110));
    private static readonly IBrush Blue = new SolidColorBrush(Color.FromRgb(0, 82, 180));
    private static readonly IBrush FranceRed = new SolidColorBrush(Color.FromRgb(239, 65, 53));
    private static readonly IBrush GermanyRed = new SolidColorBrush(Color.FromRgb(221, 0, 0));
    private static readonly IBrush GermanyGold = new SolidColorBrush(Color.FromRgb(255, 206, 0));
    private static readonly IBrush ItalyGreen = new SolidColorBrush(Color.FromRgb(0, 146, 70));
    private static readonly IBrush ItalyRed = new SolidColorBrush(Color.FromRgb(206, 43, 55));
    private static readonly IBrush CanadaRed = new SolidColorBrush(Color.FromRgb(255, 0, 0));
    private static readonly IBrush KoreaRed = new SolidColorBrush(Color.FromRgb(205, 46, 58));
    private static readonly IBrush KoreaBlue = new SolidColorBrush(Color.FromRgb(0, 71, 160));
    private static readonly IBrush FallbackBackground = new SolidColorBrush(Color.FromRgb(236, 241, 247));
    private static readonly IBrush FallbackText = new SolidColorBrush(Color.FromRgb(72, 91, 115));
    private static readonly Pen FlagStroke = new(new SolidColorBrush(Color.FromRgb(188, 198, 210)), 1);

    static CountryFlagBadge()
    {
        AffectsRender<CountryFlagBadge>(CountryCodeProperty, TextBrushProperty);
    }

    public string? CountryCode
    {
        get => GetValue(CountryCodeProperty);
        set => SetValue(CountryCodeProperty, value);
    }

    public IBrush? TextBrush
    {
        get => GetValue(TextBrushProperty);
        set => SetValue(TextBrushProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (Bounds.Width <= 0 || Bounds.Height <= 0)
        {
            return;
        }

        string code = NormalizeCode(CountryCode);
        Rect flag = new(0.5, 0.5, Math.Min(Bounds.Width - 1, 30), Math.Min(Bounds.Height - 1, 20));
        if (code.Length != 2)
        {
            DrawFallback(context, flag, code);
            return;
        }

        using (context.PushClip(flag))
        {
            switch (code)
            {
                case "JP":
                    DrawJapan(context, flag);
                    break;
                case "US":
                    DrawUnitedStates(context, flag);
                    break;
                case "CN":
                    DrawChina(context, flag);
                    break;
                case "DE":
                    DrawGermany(context, flag);
                    break;
                case "FR":
                    DrawFrance(context, flag);
                    break;
                case "IT":
                    DrawItaly(context, flag);
                    break;
                case "CA":
                    DrawCanada(context, flag);
                    break;
                case "KR":
                    DrawKorea(context, flag);
                    break;
                default:
                    DrawFallback(context, flag, code);
                    break;
            }
        }

        context.DrawRectangle(null, FlagStroke, flag, 2, 2);
    }

    private static string NormalizeCode(string? countryCode)
    {
        if (string.IsNullOrWhiteSpace(countryCode))
        {
            return "--";
        }

        string code = countryCode.Trim().ToUpperInvariant();
        return code.Length == 2 && code.All(ch => ch is >= 'A' and <= 'Z') && code is not ("XX" or "T1")
            ? code
            : code.Length <= 3 ? code : code[..3];
    }

    private static void DrawJapan(DrawingContext context, Rect rect)
    {
        context.DrawRectangle(White, null, rect);
        context.DrawEllipse(JapanRed, null, rect.Center, rect.Width * 0.24, rect.Height * 0.36);
    }

    private static void DrawUnitedStates(DrawingContext context, Rect rect)
    {
        double stripeHeight = rect.Height / 13d;
        for (int i = 0; i < 13; i++)
        {
            IBrush brush = (i & 1) == 0 ? UsRed : White;
            context.DrawRectangle(brush, null, new Rect(rect.X, rect.Y + i * stripeHeight, rect.Width, stripeHeight + 0.5));
        }

        context.DrawRectangle(UsBlue, null, new Rect(rect.X, rect.Y, rect.Width * 0.42, stripeHeight * 7));
    }

    private static void DrawChina(DrawingContext context, Rect rect)
    {
        context.DrawRectangle(ChinaRed, null, rect);
        context.DrawEllipse(ChinaYellow, null, new Point(rect.X + rect.Width * 0.23, rect.Y + rect.Height * 0.28), rect.Width * 0.08, rect.Width * 0.08);
        for (int i = 0; i < 4; i++)
        {
            context.DrawEllipse(ChinaYellow, null, new Point(rect.X + rect.Width * (0.39 + i * 0.06), rect.Y + rect.Height * (0.18 + i * 0.12)), rect.Width * 0.025, rect.Width * 0.025);
        }
    }

    private static void DrawGermany(DrawingContext context, Rect rect)
    {
        double band = rect.Height / 3d;
        context.DrawRectangle(Black, null, new Rect(rect.X, rect.Y, rect.Width, band));
        context.DrawRectangle(GermanyRed, null, new Rect(rect.X, rect.Y + band, rect.Width, band));
        context.DrawRectangle(GermanyGold, null, new Rect(rect.X, rect.Y + band * 2, rect.Width, band));
    }

    private static void DrawFrance(DrawingContext context, Rect rect)
    {
        DrawVerticalTricolor(context, rect, Blue, White, FranceRed);
    }

    private static void DrawItaly(DrawingContext context, Rect rect)
    {
        DrawVerticalTricolor(context, rect, ItalyGreen, White, ItalyRed);
    }

    private static void DrawCanada(DrawingContext context, Rect rect)
    {
        context.DrawRectangle(CanadaRed, null, new Rect(rect.X, rect.Y, rect.Width * 0.25, rect.Height));
        context.DrawRectangle(White, null, new Rect(rect.X + rect.Width * 0.25, rect.Y, rect.Width * 0.5, rect.Height));
        context.DrawRectangle(CanadaRed, null, new Rect(rect.X + rect.Width * 0.75, rect.Y, rect.Width * 0.25, rect.Height));
        context.DrawEllipse(CanadaRed, null, rect.Center, rect.Width * 0.08, rect.Height * 0.16);
    }

    private static void DrawKorea(DrawingContext context, Rect rect)
    {
        context.DrawRectangle(White, null, rect);
        context.DrawEllipse(KoreaRed, null, new Point(rect.Center.X, rect.Center.Y - rect.Height * 0.08), rect.Width * 0.14, rect.Height * 0.2);
        context.DrawEllipse(KoreaBlue, null, new Point(rect.Center.X, rect.Center.Y + rect.Height * 0.08), rect.Width * 0.14, rect.Height * 0.2);
    }

    private static void DrawVerticalTricolor(DrawingContext context, Rect rect, IBrush left, IBrush middle, IBrush right)
    {
        double band = rect.Width / 3d;
        context.DrawRectangle(left, null, new Rect(rect.X, rect.Y, band, rect.Height));
        context.DrawRectangle(middle, null, new Rect(rect.X + band, rect.Y, band, rect.Height));
        context.DrawRectangle(right, null, new Rect(rect.X + band * 2, rect.Y, rect.Width - band * 2, rect.Height));
    }

    private void DrawFallback(DrawingContext context, Rect rect, string code)
    {
        context.DrawRectangle(FallbackBackground, null, rect, 2, 2);
        FormattedText text = new(
            code,
            System.Globalization.CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            Typeface.Default,
            9,
            TextBrush ?? FallbackText);
        context.DrawText(text, new Point(rect.Center.X - text.Width / 2, rect.Center.Y - text.Height / 2));
    }
}
