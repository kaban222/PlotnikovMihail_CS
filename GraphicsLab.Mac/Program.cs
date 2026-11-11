using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;

namespace GraphicsLab.Mac;

// ===== Точка входа =====
internal static class Program
{
    [STAThread]
    public static void Main(string[] args) =>
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>().UsePlatformDetect().LogToTrace();
}

// =====  =====
public class App : Application
{
    public override void Initialize()
    {
        RequestedThemeVariant = ThemeVariant.Light;
        Styles.Add(new FluentTheme());
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new MainWindow();
        base.OnFrameworkInitializationCompleted();
    }
}

// ===== Область рисования и описание фигур =====
public enum ShapeKind { Ellipse, Rectangle, Line, Point, Text }

public class ShapeInfo
{
    public ShapeKind Kind;
    public double X, Y;     // первая точка / левый верхний угол
    public double A, B;     // Ширина+Высота, либо X2+Y2 для линии, либо размер точки
    public string Text = "";
    public Color Color;
    public double FontSize;
}

// Область рисования. Аналог формы WinForms: рисуем в методе Render
// при помощи DrawingContext (аналог Graphics).
public class DrawingCanvas : Control
{
    public List<ShapeInfo> Shapes { get; } = new List<ShapeInfo>();

    public override void Render(DrawingContext g)
    {
        g.FillRectangle(Brushes.White, new Rect(Bounds.Size));
        foreach (var s in Shapes)
            DrawShape(g, s);
    }

    private void DrawShape(DrawingContext g, ShapeInfo s)
    {
        var brush = new SolidColorBrush(s.Color);
        var pen = new Pen(brush, 2);

        switch (s.Kind)
        {
            case ShapeKind.Ellipse:
                g.DrawEllipse(null, pen, new Rect(s.X, s.Y, s.A, s.B));
                break;
            case ShapeKind.Rectangle:
                g.DrawRectangle(null, pen, new Rect(s.X, s.Y, s.A, s.B));
                break;
            case ShapeKind.Line:
                g.DrawLine(pen, new Point(s.X, s.Y), new Point(s.A, s.B));
                break;
            case ShapeKind.Point:
                DrawPoint(g, brush, s.X, s.Y, s.A);
                break;
            case ShapeKind.Text:
                DrawText(g, brush, s.Text, s.X, s.Y, s.FontSize);
                break;
        }
    }

    // Функция рисования точки
    private void DrawPoint(DrawingContext g, IBrush brush, double x, double y, double size)
    {
        if (size < 1) size = 1;
        g.DrawEllipse(brush, null, new Rect(x - size / 2, y - size / 2, size, size));
    }

    // Функция добавления текста
    private void DrawText(DrawingContext g, IBrush brush, string text, double x, double y, double fontSize)
    {
        var ft = new FormattedText(text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
                                   new Typeface("Arial"), fontSize, brush);
        g.DrawText(ft, new Point(x, y));
    }
}

// ===== Главное окно =====
public class MainWindow : Window
{
    private readonly DrawingCanvas canvas = new DrawingCanvas();
    private readonly ComboBox cbKind = new ComboBox();
    private readonly TextBlock lX = new TextBlock(), lY = new TextBlock(),
                               lA = new TextBlock(), lB = new TextBlock();
    private readonly TextBox tX = new TextBox { Text = "50" }, tY = new TextBox { Text = "60" },
                             tA = new TextBox { Text = "200" }, tB = new TextBox { Text = "120" },
                             tText = new TextBox { Text = "Компьютерная графика" },
                             tFont = new TextBox { Text = "20" };
    private readonly ComboBox cbColor = new ComboBox();

    private static readonly (string Name, Color Color)[] Palette =
    {
        ("Чёрный", Colors_.Black), ("Синий", Colors_.Blue), ("Красный", Colors_.Red),
        ("Зелёный", Colors_.Green), ("Фиолетовый", Colors_.Purple), ("Оранжевый", Colors_.Orange)
    };

    public MainWindow()
    {
        Title = "Графические примитивы (macOS)";
        Width = 960;
        Height = 620;

        cbKind.ItemsSource = new[] { "Эллипс", "Прямоугольник", "Линия", "Точка", "Текст" };
        cbKind.SelectedIndex = 0;
        cbKind.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
        cbKind.SelectionChanged += (s, e) => UpdateLabels();

        cbColor.ItemsSource = Palette.Select(c => c.Name).ToArray();
        cbColor.SelectedIndex = 0;
        cbColor.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;

        var btnDraw = new Button { Content = "Нарисовать" };
        var btnDemo = new Button { Content = "Нарисовать все примитивы" };
        var btnClear = new Button { Content = "Очистить" };
        btnDraw.Click += (s, e) => DrawFromInput();
        btnDemo.Click += (s, e) => Demo();
        btnClear.Click += (s, e) => { canvas.Shapes.Clear(); canvas.InvalidateVisual(); };

        var panel = new StackPanel { Width = 250, Margin = new Thickness(10), Spacing = 6 };
        panel.Children.Add(new TextBlock { Text = "Фигура:" });
        panel.Children.Add(cbKind);
        panel.Children.Add(lX); panel.Children.Add(tX);
        panel.Children.Add(lY); panel.Children.Add(tY);
        panel.Children.Add(lA); panel.Children.Add(tA);
        panel.Children.Add(lB); panel.Children.Add(tB);
        panel.Children.Add(new TextBlock { Text = "Текст:" }); panel.Children.Add(tText);
        panel.Children.Add(new TextBlock { Text = "Размер шрифта:" }); panel.Children.Add(tFont);
        panel.Children.Add(new TextBlock { Text = "Цвет:" }); panel.Children.Add(cbColor);
        panel.Children.Add(btnDraw);
        panel.Children.Add(btnDemo);
        panel.Children.Add(btnClear);

        var dock = new DockPanel();
        DockPanel.SetDock(panel, Dock.Left);
        dock.Children.Add(panel);
        dock.Children.Add(canvas);
        Content = dock;

        UpdateLabels();
    }

    private void UpdateLabels()
    {
        switch (cbKind.SelectedIndex)
        {
            case 0:
            case 1:
                lX.Text = "X (левый верх):"; lY.Text = "Y (левый верх):";
                lA.Text = "Ширина:"; lB.Text = "Высота:"; break;
            case 2:
                lX.Text = "X1:"; lY.Text = "Y1:"; lA.Text = "X2:"; lB.Text = "Y2:"; break;
            case 3:
                lX.Text = "X:"; lY.Text = "Y:";
                lA.Text = "Размер точки (px):"; lB.Text = "(не используется)"; break;
            default:
                lX.Text = "X:"; lY.Text = "Y:";
                lA.Text = "(не используется)"; lB.Text = "(не используется)"; break;
        }
    }

    private static double Num(TextBox t) =>
        double.TryParse(t.Text?.Replace(',', '.'), System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 0;

    private void DrawFromInput()
    {
        var kind = (ShapeKind)cbKind.SelectedIndex;
        if (kind == ShapeKind.Text && string.IsNullOrWhiteSpace(tText.Text)) return;

        canvas.Shapes.Add(new ShapeInfo
        {
            Kind = kind,
            X = Num(tX), Y = Num(tY), A = Num(tA), B = Num(tB),
            Text = tText.Text ?? "",
            FontSize = Math.Max(6, Num(tFont)),
            Color = Palette[Math.Max(0, cbColor.SelectedIndex)].Color
        });
        canvas.InvalidateVisual();
    }

    private void Demo()
    {
        var l = canvas.Shapes;
        l.Clear();
        l.Add(new ShapeInfo { Kind = ShapeKind.Ellipse, X = 50, Y = 60, A = 200, B = 120, Color = Colors_.Blue });
        l.Add(new ShapeInfo { Kind = ShapeKind.Rectangle, X = 300, Y = 60, A = 200, B = 120, Color = Colors_.Red });
        l.Add(new ShapeInfo { Kind = ShapeKind.Line, X = 50, Y = 230, A = 500, B = 300, Color = Colors_.Green });
        for (int i = 0; i < 5; i++)
            l.Add(new ShapeInfo { Kind = ShapeKind.Point, X = 70 + i * 30, Y = 360, A = 10, Color = Colors_.Purple });
        l.Add(new ShapeInfo { Kind = ShapeKind.Text, X = 50, Y = 420, Text = "Компьютерная графика",
                              Color = Colors_.Black, FontSize = 24 });
        canvas.InvalidateVisual();
    }
}

// Короткие имена цветов
internal static class Colors_
{
    public static readonly Color Black = Color.FromRgb(0, 0, 0);
    public static readonly Color Blue = Color.FromRgb(0, 0, 255);
    public static readonly Color Red = Color.FromRgb(255, 0, 0);
    public static readonly Color Green = Color.FromRgb(0, 128, 0);
    public static readonly Color Purple = Color.FromRgb(128, 0, 128);
    public static readonly Color Orange = Color.FromRgb(255, 140, 0);
}
