using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Media;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

internal static class Launcher
{
    public static readonly string Root = AppDomain.CurrentDomain.BaseDirectory;

    [STAThread]
    private static int Main(string[] args)
    {
        bool preview = Array.IndexOf(args, "--preview") >= 0;
        bool frames = Array.IndexOf(args, "--frames") >= 0;
        bool hold = Array.IndexOf(args, "--hold") >= 0;
        if (Array.IndexOf(args, "--make-sound") >= 0)
        {
            Soundtrack.Write(Path.Combine(Root, "assets", "launch.wav"));
            return 0;
        }
        bool first;
        using (var instance = new Mutex(true, "Local\\AsukaCodexLauncher", out first))
        {
            if (!first) return 0;
            bool started = false;
            var flow = new LaunchFlow(preview || frames || hold, delegate { if (!started) OpenCodex(); });
            try
            {
                WindowTarget target = frames ? null : WindowTarget.FindCodex();
                if (!frames && target == null && !preview && !hold)
                {
                    if (!OpenCodex()) return 1;
                    started = true;
                    for (int i = 0; i < 160 && target == null; i++)
                    {
                        Thread.Sleep(50);
                        target = WindowTarget.FindCodex();
                    }
                }
                if (!frames && target == null)
                    throw new InvalidOperationException("No visible Codex window was found. Open Codex before previewing the animation.");
                var app = new Application();
                var window = new LaunchWindow(flow, target, frames, hold, Array.IndexOf(args, "--measure") >= 0);
                app.DispatcherUnhandledException += delegate(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
                {
                    Log(e.Exception);
                    e.Handled = true;
                    flow.Complete();
                    app.Shutdown(1);
                };
                app.Run(window);
                return 0;
            }
            catch (Exception e)
            {
                Log(e);
                if (preview || frames || hold)
                {
                    MessageBox.Show("Preview could not load.\n" + e.Message, "Asuka Codex", MessageBoxButton.OK, MessageBoxImage.Error);
                    return 1;
                }
                flow.Complete();
                return 0;
            }
            finally { instance.ReleaseMutex(); }
        }
    }

    private static void Log(Exception error)
    {
        try { File.AppendAllText(Path.Combine(Root, "launcher-error.txt"), DateTime.Now.ToString("s") + " " + error + Environment.NewLine); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private static bool OpenCodex()
    {
        try
        {
            CodexActivation.Activate("OpenAI.Codex_2p2nqsd0c76g0!App");
            return true;
        }
        catch (Exception e)
        {
            MessageBox.Show("Unable to open Codex. Please use your original shortcut.\n" + e.Message,
                "Asuka Codex", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }
}

internal sealed class LaunchWindow : Window
{
    private readonly LaunchFlow flow;
    private readonly Canvas scene;
    private readonly Storyboard timeline = new Storyboard { Duration = TimeSpan.FromSeconds(5) };
    private SoundPlayer sound;

    public LaunchWindow(LaunchFlow flow, WindowTarget target, bool frames, bool hold, bool measure)
    {
        this.flow = flow;
        Title = "Asuka × Codex — Personal Launch Sequence";
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        Background = Brushes.Black;
        Topmost = false;
        ShowInTaskbar = hold;
        Cursor = Cursors.None;
        Width = 960;
        Height = 540;
        using (Stream xaml = Assembly.GetExecutingAssembly().GetManifestResourceStream("Scene.xaml"))
            scene = (Canvas)XamlReader.Load(xaml);
        Content = new Viewbox { Stretch = Stretch.Uniform, Child = scene };
        BitmapImage portrait = LoadImage(Path.Combine(Launcher.Root, "assets", "asuka.png"));
        Find<Image>("HeroImage").Source = portrait;
        BitmapImage closed = LoadImage(Path.Combine(Launcher.Root, "assets", "asuka-closed.png"));
        Find<Image>("ClosedEyes").Source = closed;
        Find<Image>("HalfEyes").Source = LoadImage(Path.Combine(Launcher.Root, "assets", "asuka-half.png"));
        Find<Image>("LineImage").Source = MakeLines(closed);
        Geometry knot = Geometry.Parse(File.ReadAllText(Path.Combine(Launcher.Root, "assets", "knot.path.txt")));
        knot.Freeze();
        Find<System.Windows.Shapes.Path>("IntroKnot").Data = knot;
        Find<System.Windows.Shapes.Path>("FinalKnot").Data = knot;
        PrepareTimeline();
        ResizeScene(1920);
        SourceInitialized += delegate
        {
            if (frames) return;
            var helper = new WindowInteropHelper(this);
            helper.Owner = target.Handle;
            target.Align(helper.Handle);
        };
        SizeChanged += delegate { if (!frames && ActualHeight > 0) ResizeScene(1080 * ActualWidth / ActualHeight); };
        var follow = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(60) };
        follow.Tick += delegate { target.Align(new WindowInteropHelper(this).Handle); };
        PreviewKeyDown += delegate(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) { e.Handled = true; Complete(); }
            if (e.Key == Key.System && e.SystemKey == Key.F4) flow.Cancel();
        };
        Closing += delegate { flow.Cancel(); };
        Closed += delegate
        {
            timeline.Remove(scene);
            follow.Stop();
            if (sound != null) { sound.Stop(); sound.Dispose(); }
        };
        ContentRendered += delegate
        {
            if (frames)
            {
                ExportFrames();
                Close();
                return;
            }
            target.Align(new WindowInteropHelper(this).Handle);
            follow.Start();
            timeline.Completed += delegate { if (!hold) Complete(); };
            if (!hold) PlaySound();
            if (measure) MeasurePlayback(target);
            timeline.Begin(scene, true);
            if (hold)
            {
                timeline.SeekAlignedToLastTick(scene, TimeSpan.FromSeconds(4.3), TimeSeekOrigin.BeginTime);
                timeline.Pause(scene);
            }
        };
    }

    private void ResizeScene(double width)
    {
        bool compact = width < 1350;
        double margin = compact ? 55 : 126;
        scene.Width = width;
        foreach (UIElement item in scene.Children)
        {
            FrameworkElement element = item as FrameworkElement;
            if (element != null && !(element is System.Windows.Shapes.Rectangle && element == Find<System.Windows.Shapes.Rectangle>("LeftShade")))
                element.Width = width;
        }
        foreach (string name in new[] { "Atmosphere", "TitleCard" })
        {
            var canvas = Find<Canvas>(name);
            ((FrameworkElement)canvas.Children[0]).Width = width;
        }
        var intro = Find<Canvas>("Intro");
        Canvas.SetLeft(intro.Children[0], width / 2 - 66);
        ((FrameworkElement)intro.Children[1]).Width = width;
        Canvas.SetLeft(intro.Children[2], width / 2 - 96);
        var titleCard = Find<Canvas>("TitleCard");
        ((TextBlock)titleCard.Children[1]).FontSize = Math.Min(420, width * 0.22);
        Canvas.SetLeft(titleCard.Children[2], Math.Max(55, width - 760));
        Find<Canvas>("ColorFill").Width = width;
        Find<Canvas>("Character").RenderTransformOrigin = compact ? new Point(0.5,0.37) : new Point(0.71,0.37);
        Find<TextBlock>("BackNumber").RenderTransform = new TranslateTransform(width - 1920, 0);

        double imageWidth = compact ? width * 1.13 : Math.Min(940, width * 0.57);
        double imageLeft = compact ? (width - imageWidth) / 2 : width - imageWidth - 90;
        double imageTop = compact ? 65 : 24;
        var layers = Find<Canvas>("PortraitLayers");
        layers.Width = imageWidth; layers.Height = imageWidth * 1.5;
        Canvas.SetLeft(layers, imageLeft); Canvas.SetTop(layers, imageTop);
        foreach (string name in new[] { "HeroImage", "ClosedEyes", "HalfEyes", "LineImage" })
        {
            var image = Find<Image>(name);
            image.Width = imageWidth; image.Height = imageWidth * 1.5;
            if (name == "LineImage") { Canvas.SetLeft(image, imageLeft); Canvas.SetTop(image, imageTop); }
        }
        var eyePatches = Geometry.Parse("M390,446 C423,424 499,414 542,446 L550,504 C500,541 418,529 385,491 Z M586,398 C626,356 700,347 738,382 L745,448 C703,492 638,490 594,460 Z").Clone();
        eyePatches.Transform = new ScaleTransform(imageWidth / 1024, imageWidth / 1024);
        Find<Image>("ClosedEyes").Clip = eyePatches;
        Find<Image>("HalfEyes").Clip = eyePatches;

        var shade = Find<System.Windows.Shapes.Rectangle>("LeftShade");
        shade.Width = compact ? width : Math.Min(1280, width * 0.67);
        if (compact)
        {
            shade.Fill = new LinearGradientBrush(new GradientStopCollection {
                new GradientStop(Color.FromArgb(0,7,8,11),0), new GradientStop(Color.FromArgb(0,7,8,11),0.52),
                new GradientStop(Color.FromArgb(225,7,8,11),0.78), new GradientStop(Color.FromArgb(250,7,8,11),1)
            }, new Point(0,0), new Point(0,1));
        }
        else shade.Fill = new LinearGradientBrush(new GradientStopCollection {
            new GradientStop(Color.FromArgb(245,7,8,11),0), new GradientStop(Color.FromArgb(217,7,8,11),0.5),
            new GradientStop(Color.FromArgb(0,7,8,11),1)
        }, new Point(0,0), new Point(1,0));

        var heroType = Find<Canvas>("HeroType");
        ((FrameworkElement)heroType.Children[0]).Visibility = compact ? Visibility.Hidden : Visibility.Visible;
        Canvas.SetLeft(Find<TextBlock>("PilotName"), compact ? margin : 200);
        Canvas.SetTop(Find<TextBlock>("PilotName"), compact ? 678 : 291);
        Find<TextBlock>("PilotName").FontSize = compact ? Math.Min(16, width * 0.021) : 18;
        Canvas.SetLeft(Find<TextBlock>("AsukaTitle"), compact ? margin - 7 : 112);
        Canvas.SetTop(Find<TextBlock>("AsukaTitle"), compact ? 695 : 318);
        Find<TextBlock>("AsukaTitle").FontSize = compact ? width * 0.205 : Math.Min(203, width * 0.105);
        Canvas.SetLeft(Find<TextBlock>("JapaneseName"), compact ? margin : 127);
        Canvas.SetTop(Find<TextBlock>("JapaneseName"), compact ? 875 : 552);
        Find<TextBlock>("JapaneseName").FontSize = compact ? Math.Min(18, width * 0.023) : 22;
        Find<TextBlock>("SecondChild").Visibility = compact ? Visibility.Hidden : Visibility.Visible;
        Find<TextBlock>("UnitLabel").Visibility = compact ? Visibility.Hidden : Visibility.Visible;
        Canvas.SetLeft(Find<Canvas>("Lockup"), compact ? margin - 130 : 0);
        Canvas.SetTop(Find<Canvas>("Lockup"), compact ? 130 : 0);

        var details = Find<Canvas>("Details");
        Canvas.SetLeft(details.Children[0], margin);
        Canvas.SetTop(details.Children[0], compact ? 48 : 90);
        ((TextBlock)details.Children[0]).FontSize = compact ? 10 : 13;
        ((FrameworkElement)details.Children[1]).Visibility = compact ? Visibility.Hidden : Visibility.Visible;
        ((FrameworkElement)details.Children[2]).Visibility = compact ? Visibility.Hidden : Visibility.Visible;
        Canvas.SetLeft(details.Children[3], margin);
        ((FrameworkElement)details.Children[3]).Width = width - 2 * margin;
        Canvas.SetTop(details.Children[3], compact ? 1015 : 944);
        Canvas.SetLeft(details.Children[4], margin);
        ((FrameworkElement)details.Children[4]).Width = width - 2 * margin;
        Canvas.SetTop(details.Children[4], compact ? 1015 : 944);
        Canvas.SetLeft(details.Children[5], margin);
        Canvas.SetTop(details.Children[5], compact ? 1036 : 974);
        Canvas.SetLeft(details.Children[6], compact ? width - margin - 90 : 700);
        Canvas.SetTop(details.Children[6], compact ? 1036 : 974);
    }

    private static BitmapSource MakeLines(BitmapSource image)
    {
        var bgra = new FormatConvertedBitmap(image, PixelFormats.Bgra32, null, 0);
        int width = bgra.PixelWidth, height = bgra.PixelHeight, stride = width * 4;
        byte[] pixels = new byte[stride * height], lines = new byte[stride * height];
        bgra.CopyPixels(pixels, stride, 0);
        for (int y = 1; y < height - 1; y++)
            for (int x = 1; x < width - 1; x++)
            {
                int p = y * stride + x * 4;
                if (pixels[p + 3] < 8) continue;
                double dx = Luma(pixels,p+4) - Luma(pixels,p-4);
                double dy = Luma(pixels,p+stride) - Luma(pixels,p-stride);
                double edge = Math.Max(Math.Sqrt(dx*dx+dy*dy) * 3.4 - 20,
                    Math.Abs(pixels[p+7]-pixels[p-1]) + Math.Abs(pixels[p+stride+3]-pixels[p-stride+3]));
                lines[p] = 125; lines[p+1] = 180; lines[p+2] = 255;
                lines[p+3] = (byte)(Math.Min(255, Math.Max(0,edge)) * pixels[p+3] / 255);
            }
        var result = BitmapSource.Create(width,height,96,96,PixelFormats.Bgra32,null,lines,stride);
        result.Freeze();
        return result;
    }

    private static double Luma(byte[] pixels, int at)
    {
        return (pixels[at] * 0.0722 + pixels[at+1] * 0.7152 + pixels[at+2] * 0.2126) * pixels[at+3] / 255;
    }

    private void MeasurePlayback(WindowTarget target)
    {
        var intervals = new List<double>();
        double last = -1;
        var elapsed = Stopwatch.StartNew();
        EventHandler collect = delegate(object sender, EventArgs args)
        {
            double now = ((RenderingEventArgs)args).RenderingTime.TotalMilliseconds;
            if (last >= 0 && now > last) intervals.Add(now - last);
            last = now;
        };
        CompositionTarget.Rendering += collect;
        Closing += delegate
        {
            CompositionTarget.Rendering -= collect;
            intervals.Sort();
            string report = string.Format(CultureInfo.InvariantCulture,
                "elapsed_ms={0:F1}\nrender_intervals={1}\nmedian_interval_ms={2:F2}\np95_interval_ms={3:F2}\nwindow_dips={4:F0}x{5:F0}\nrender_tier={6}\ntarget_pixels={7}\nanimation_pixels={8}\n",
                elapsed.Elapsed.TotalMilliseconds, intervals.Count,
                intervals.Count == 0 ? 0 : intervals[intervals.Count / 2],
                intervals.Count == 0 ? 0 : intervals[(int)((intervals.Count - 1) * 0.95)],
                ActualWidth, ActualHeight, RenderCapability.Tier >> 16, target.Bounds,
                WindowTarget.GetBounds(new WindowInteropHelper(this).Handle));
            Directory.CreateDirectory(Path.Combine(Launcher.Root, "previews"));
            File.WriteAllText(Path.Combine(Launcher.Root, "previews", "playback.txt"), report);
        };
    }

    private static BitmapImage LoadImage(string path)
    {
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.UriSource = new Uri(path);
        image.EndInit();
        image.Freeze();
        return image;
    }

    private T Find<T>(string name) where T : class
    {
        return (T)scene.FindName(name);
    }

    private void Complete()
    {
        // Hide the final black frame before activating the application behind it.
        Hide();
        flow.Complete();
        Close();
    }

    private void PlaySound()
    {
        try
        {
            sound = new SoundPlayer(Path.Combine(Launcher.Root, "assets", "launch.wav"));
            sound.Load();
            sound.Play();
        }
        catch (Exception) { if (sound != null) { sound.Dispose(); sound = null; } }
    }

    private void Animate(DependencyObject target, DependencyProperty property, bool hardCut, params double[] pairs)
    {
        Animate(target, new PropertyPath(property), hardCut, pairs);
    }

    private void Animate(DependencyObject target, PropertyPath property, bool hardCut, params double[] pairs)
    {
        var animation = new DoubleAnimationUsingKeyFrames();
        for (int i = 0; i < pairs.Length; i += 2)
        {
            KeyTime at = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(pairs[i]));
            if (hardCut) animation.KeyFrames.Add(new DiscreteDoubleKeyFrame(pairs[i + 1], at));
            else animation.KeyFrames.Add(new SplineDoubleKeyFrame(pairs[i + 1], at, new KeySpline(0.16, 1, 0.3, 1)));
        }
        Storyboard.SetTarget(animation, target);
        Storyboard.SetTargetProperty(animation, property);
        timeline.Children.Add(animation);
    }

    private void Fade(string name, bool hardCut, params double[] pairs)
    {
        Animate(Find<UIElement>(name), UIElement.OpacityProperty, hardCut, pairs);
    }

    private void PrepareTimeline()
    {
        Fade("Atmosphere", false, 0,0, 0.65,0.28, 1.85,0.6, 2.45,1);
        Fade("Intro", false, 0,0, 0.3,1, 0.68,1, 0.79,0);
        Fade("TitleCard", true, 0,0, 0.74,1, 0.88,0, 0.98,1, 1.14,0);
        Fade("Character", true, 0,0, 1.08,1);
        Fade("LineImage", false, 0,0, 1.08,0, 1.3,1, 2.3,1, 2.75,0);
        var trace = new DoubleAnimation(-240,1600,TimeSpan.FromSeconds(1.05)) { BeginTime = TimeSpan.FromSeconds(1.08) };
        Storyboard.SetTarget(trace, Find<Image>("LineImage"));
        Storyboard.SetTargetProperty(trace, new PropertyPath("(0).(1).(2)", UIElement.OpacityMaskProperty, Brush.TransformProperty, TranslateTransform.YProperty));
        timeline.Children.Add(trace);
        var character = Find<Canvas>("Character");
        Animate(character, new PropertyPath("(0).(1)[0].(2)", UIElement.RenderTransformProperty, TransformGroup.ChildrenProperty, ScaleTransform.ScaleXProperty), false, 0,1.14, 1.72,1.14, 2.55,1.01, 4.75,1);
        Animate(character, new PropertyPath("(0).(1)[0].(2)", UIElement.RenderTransformProperty, TransformGroup.ChildrenProperty, ScaleTransform.ScaleYProperty), false, 0,1.14, 1.72,1.14, 2.55,1.01, 4.75,1);
        Animate(character, new PropertyPath("(0).(1)[1].(2)", UIElement.RenderTransformProperty, TransformGroup.ChildrenProperty, TranslateTransform.XProperty), false, 0,70, 1.72,70, 2.55,0, 4.75,-15);
        var fill = Find<Canvas>("ColorFill");
        var mask = (LinearGradientBrush)fill.OpacityMask;
        Animate(fill, new PropertyPath("(0).(1).(2)", UIElement.OpacityMaskProperty, Brush.RelativeTransformProperty, TranslateTransform.XProperty), false, 0,1.12, 1.95,1.12, 2.72,-0.16);
        Fade("ClosedEyes", false, 0,1, 2.85,1, 3.03,0);
        Fade("HalfEyes", false, 0,0, 2.85,0, 2.97,1, 3.04,1, 3.25,0);

        Fade("LeftShade", false, 0,0, 2.0,0, 2.4,1);
        Fade("HeroType", false, 0,0, 3.15,0, 3.48,1);
        Move("HeroType", TranslateTransform.XProperty, 0,-70, 3.15,-70, 3.5,0);
        Fade("Sweep", false, 0,0, 1.74,0, 1.78,1, 2.4,0);
        Move("Sweep", TranslateTransform.XProperty, 0,1350, 1.74,1350, 2.4,-700);
        Fade("Details", false, 0,0, 2.45,0, 2.9,1);
        Animate(Find<System.Windows.Shapes.Rectangle>("Progress"), new PropertyPath("(0).(1)", UIElement.RenderTransformProperty, ScaleTransform.ScaleXProperty), false, 0,0, 2.45,0, 4.75,1);
        Fade("Lockup", false, 0,0, 3.83,0, 4.16,1);
        Move("Lockup", TranslateTransform.YProperty, 0,18, 3.83,18, 4.22,0);
        Fade("Flash", false, 0,0, 1.73,0, 1.75,0.24, 1.87,0);
        Fade("Curtain", false, 0,0, 4.68,0, 5,1);
        Animate(Find<TextBlock>("BackNumber"), Canvas.LeftProperty, false, 0,1050, 1.75,1050, 4.75,980);
    }

    private void Move(string name, DependencyProperty property, params double[] pairs)
    {
        Animate(Find<UIElement>(name), new PropertyPath("(0).(1)", UIElement.RenderTransformProperty, property), false, pairs);
    }

    private void ExportFrames()
    {
        string directory = Path.Combine(Launcher.Root, "previews");
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "render-diagnostics.txt"), "");
        timeline.Begin(scene, true);
        double[] seconds = { 0.5, 0.8, 1.45, 2.45, 2.9, 3.05, 3.3, 4.3 };
        foreach (double second in seconds)
        {
            timeline.SeekAlignedToLastTick(scene, TimeSpan.FromSeconds(second), TimeSeekOrigin.BeginTime);
            scene.Measure(new Size(1920, 1080));
            scene.Arrange(new Rect(0, 0, 1920, 1080));
            scene.UpdateLayout();
            var character = Find<Canvas>("Character");
            var brush = (LinearGradientBrush)Find<Canvas>("ColorFill").OpacityMask;
            File.AppendAllText(Path.Combine(directory, "render-diagnostics.txt"), string.Format(CultureInfo.InvariantCulture,
                "{0}: opacity={1}, mask={2}, pan={3}, scale={4}\n", second, character.Opacity,
                ((TranslateTransform)brush.RelativeTransform).X, ((TranslateTransform)((TransformGroup)character.RenderTransform).Children[1]).X,
                ((ScaleTransform)((TransformGroup)character.RenderTransform).Children[0]).ScaleX));
            var bitmap = new RenderTargetBitmap(1920, 1080, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(scene);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            string filename = "frame-" + second.ToString("0.00", CultureInfo.InvariantCulture) + ".png";
            using (var output = File.Create(Path.Combine(directory, filename))) encoder.Save(output);
        }
        foreach (double width in new[] { 764.25, 1440.0, 2592.0 })
        {
            ResizeScene(width);
            scene.Measure(new Size(width,1080)); scene.Arrange(new Rect(0,0,width,1080)); scene.UpdateLayout();
            foreach (double at in new[] { 1.45, 4.3 })
            {
                timeline.SeekAlignedToLastTick(scene, TimeSpan.FromSeconds(at), TimeSeekOrigin.BeginTime);
                var bitmap = new RenderTargetBitmap((int)Math.Round(width),1080,96,96,PixelFormats.Pbgra32);
                bitmap.Render(scene);
                var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
                string prefix = at < 2 ? "lines-" : "layout-";
                using (var output = File.Create(Path.Combine(directory,prefix + ((int)width) + ".png"))) encoder.Save(output);
            }
        }
    }
}

internal static class Soundtrack
{
    public static void Write(string path)
    {
        const int rate = 44100;
        const int count = rate * 5;
        var samples = new double[count];
        var random = new Random(2);
        double peak = 0;
        for (int i = 0; i < count; i++)
        {
            double t = (double)i / rate;
            double noise = random.NextDouble() * 2 - 1;
            double swell = Math.Sin(Math.PI * Math.Min(t / 1.75, 1));
            double value = 0.10 * swell * (Math.Sin(2 * Math.PI * 48 * t) + 0.25 * Math.Sin(2 * Math.PI * 96 * t));
            value += Hit(t, 0.74, noise, 0.19) + Hit(t, 0.98, noise, 0.23) + Hit(t, 1.75, noise, 0.62);
            double sweep = t - 1.18;
            if (sweep >= 0 && sweep < 0.57)
                value += 0.13 * Math.Sin(Math.PI * sweep / 0.57) * (0.6 * noise + 0.4 * Math.Sin(2 * Math.PI * (190 * sweep + 600 * sweep * sweep)));
            double chord = t - 1.75;
            if (chord >= 0)
                value += 0.065 * Math.Exp(-chord * 0.85) * (Math.Sin(2 * Math.PI * 146.83 * chord) + 0.6 * Math.Sin(2 * Math.PI * 220 * chord) + 0.4 * Math.Sin(2 * Math.PI * 293.66 * chord));
            double end = t - 3.92;
            if (end >= 0)
                value += 0.055 * Math.Exp(-end * 5) * (Math.Sin(2 * Math.PI * 587.33 * end) + 0.4 * Math.Sin(2 * Math.PI * 880 * end));
            value *= Math.Min(1, t / 0.025) * Math.Min(1, (5 - t) / 0.35);
            samples[i] = value;
            peak = Math.Max(peak, Math.Abs(value));
        }
        using (var writer = new BinaryWriter(File.Create(path)))
        {
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + count * 4);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16);
            writer.Write((short)1); writer.Write((short)2); writer.Write(rate); writer.Write(rate * 4);
            writer.Write((short)4); writer.Write((short)16);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(count * 4);
            for (int i = 0; i < count; i++)
            {
                short sample = (short)(samples[i] / Math.Max(1, peak) * 0.72 * short.MaxValue);
                writer.Write(sample); writer.Write(sample);
            }
        }
    }

    private static double Hit(double t, double at, double noise, double volume)
    {
        double d = t - at;
        if (d < 0) return 0;
        double phase = 2 * Math.PI * (42 * d + 58 * (1 - Math.Exp(-d * 16)) / 16);
        return volume * (Math.Exp(-d * 7) * Math.Sin(phase) + 0.14 * Math.Exp(-d * 40) * noise) * Math.Min(1, d / 0.004);
    }
}
