using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HideMyWindows.App.Controls
{
    /// <summary>
    /// Interaction logic for TourOverlay.xaml
    /// </summary>
    public partial class TourOverlay : UserControl
    {
        public TourOverlay()
        {
            Visibility = Visibility.Collapsed;

            InitializeComponent();
            SizeChanged += (_, __) => Reposition();
            Loaded += (_, __) => Reposition();
        }

        public static readonly DependencyProperty TitleTextProperty =
            DependencyProperty.Register(nameof(TitleText), typeof(string), typeof(TourOverlay), new PropertyMetadata(""));

        public static readonly DependencyProperty BodyTextProperty =
            DependencyProperty.Register(nameof(BodyText), typeof(string), typeof(TourOverlay), new PropertyMetadata(""));

        public static readonly DependencyProperty HighlightTargetProperty =
            DependencyProperty.Register(nameof(HighlightTarget), typeof(FrameworkElement), typeof(TourOverlay),
                new PropertyMetadata(null, OnTargetChanged));

        public static readonly DependencyProperty NextCommandProperty =
            DependencyProperty.Register(nameof(NextCommand), typeof(ICommand), typeof(TourOverlay), new PropertyMetadata(null));

        public static readonly DependencyProperty BackCommandProperty =
            DependencyProperty.Register(nameof(BackCommand), typeof(ICommand), typeof(TourOverlay), new PropertyMetadata(null));

        public static readonly DependencyProperty SkipCommandProperty =
            DependencyProperty.Register(nameof(SkipCommand), typeof(ICommand), typeof(TourOverlay), new PropertyMetadata(null));

        public string TitleText { get => (string)GetValue(TitleTextProperty); set => SetValue(TitleTextProperty, value); }
        public string BodyText { get => (string)GetValue(BodyTextProperty); set => SetValue(BodyTextProperty, value); }

        public FrameworkElement HighlightTarget
        {
            get => (FrameworkElement)GetValue(HighlightTargetProperty);
            set => SetValue(HighlightTargetProperty, value);
        }

        public ICommand NextCommand
        {
            get => (ICommand)GetValue(NextCommandProperty);
            set => SetValue(NextCommandProperty, value);
        }

        public ICommand BackCommand
        {
            get => (ICommand)GetValue(BackCommandProperty);
            set => SetValue(BackCommandProperty, value);
        }

        public ICommand SkipCommand
        {
            get => (ICommand)GetValue(SkipCommandProperty);
            set => SetValue(SkipCommandProperty, value);
        }

        private static void OnTargetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TourOverlay overlay)
                overlay.Reposition();
        }

        public void Reposition()
        {
            if (Root == null) return;

            if (Parent is FrameworkElement p)
            {
                Root.Width = p.ActualWidth;
                Root.Height = p.ActualHeight;
            }

            const double pad = 16;
            const double inset = 16;

            if (HighlightTarget == null || !HighlightTarget.IsVisible)
            {
                Highlight.Visibility = Visibility.Collapsed;

                Callout.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                var sz = Callout.DesiredSize;
                var cx = Math.Max(inset, (Root.ActualWidth - sz.Width) / 2);
                var cy = Math.Max(inset, (Root.ActualHeight - sz.Height) / 2);
                Canvas.SetLeft(Callout, cx);
                Canvas.SetTop(Callout, cy);
                return;
            }

            var t = HighlightTarget.TransformToVisual(Root);
            var rect = t.TransformBounds(new Rect(0, 0, HighlightTarget.ActualWidth, HighlightTarget.ActualHeight));

            Highlight.Width = rect.Width + 8;
            Highlight.Height = rect.Height + 8;
            Highlight.Visibility = Visibility.Visible;
            Canvas.SetLeft(Highlight, rect.Left - 4);
            Canvas.SetTop(Highlight, rect.Top - 4);

            Callout.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var csz = Callout.DesiredSize;
            double cw = csz.Width, ch = csz.Height;

            bool placed = false;
            double x = 0, y = 0;

            {
                double tryX = rect.Right + pad;
                double tryY = Math.Min(Math.Max(rect.Top, inset), Root.ActualHeight - inset - ch);
                if (tryX + cw <= Root.ActualWidth - inset)
                {
                    x = tryX; y = tryY; placed = true;
                }
            }

            if (!placed)
            {
                double tryX = rect.Left - pad - cw;
                double tryY = Math.Min(Math.Max(rect.Top, inset), Root.ActualHeight - inset - ch);
                if (tryX >= inset)
                {
                    x = tryX; y = tryY; placed = true;
                }
            }

            if (!placed)
            {
                double tryY = rect.Bottom + pad;
                if (tryY + ch <= Root.ActualHeight - inset)
                {
                    double tryX = Math.Min(Math.Max(rect.Left, inset), Root.ActualWidth - inset - cw);
                    x = tryX; y = tryY; placed = true;
                }
            }

            if (!placed)
            {
                double tryY = rect.Top - pad - ch;
                if (tryY >= inset)
                {
                    double tryX = Math.Min(Math.Max(rect.Left, inset), Root.ActualWidth - inset - cw);
                    x = tryX; y = tryY; placed = true;
                }
            }

            if (!placed)
            {
                x = Math.Max(inset, (Root.ActualWidth - cw) / 2);
                y = Math.Max(inset, (Root.ActualHeight - ch) / 2);
            }

            x = Math.Min(Math.Max(inset, x), Math.Max(inset, Root.ActualWidth - inset - cw));
            y = Math.Min(Math.Max(inset, y), Math.Max(inset, Root.ActualHeight - inset - ch));

            Canvas.SetLeft(Callout, x);
            Canvas.SetTop(Callout, y);
        }
    }
}