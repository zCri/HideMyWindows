using HideMyWindows.App.Controls;
using HideMyWindows.App.Helpers;
using HideMyWindows.App.Models;
using HideMyWindows.App.Services.ConfigProvider;
using HideMyWindows.App.Views.Pages;
using HideMyWindows.App.Views.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace HideMyWindows.App.Services.TourService
{
    public sealed class TourService : ITourService
    {
        public IList<TourStep> Steps { get; set; } = new List<TourStep>();

        private IConfigProvider _configProvider;
        private INavigationService _navigationService;
        private TourOverlay? _overlay;

        private int _index = -1;
        private bool _tempQuickLaunchEntry = false;
        private bool _tempWindowRule = false;

        public TourService(IConfigProvider configProvider, INavigationService navigationService)
        {
            _configProvider = configProvider;
            _navigationService = navigationService;

            configProvider.Load();
        }

        public void SetTourOverlay(TourOverlay overlay)
        {
            _overlay = overlay;

            overlay.NextCommand = new RelayCommand(Next);
            overlay.BackCommand = new RelayCommand(Back, () => _index > 0);
            overlay.SkipCommand = new RelayCommand(() => Skip(true));

            //_frame.Navigated += (_, __) => RefreshAnchor();

            //_window.SizeChanged += (_, __) => _overlay.Reposition();
        }

        public void TryAutoStart()
        {
            if (!IsDismissed())
                Start();
        }

        public void Start()
        {
            if (Application.Current.MainWindow is null || _overlay is null)
                return;

            KeyboardNavigation.SetTabNavigation(Application.Current.MainWindow, KeyboardNavigationMode.None);
            KeyboardNavigation.SetControlTabNavigation(Application.Current.MainWindow, KeyboardNavigationMode.None);
            KeyboardNavigation.SetDirectionalNavigation(Application.Current.MainWindow, KeyboardNavigationMode.None);

            if (Steps.Count == 0)
            {
                Steps = new List<TourStep>
                {
                    new() { Page = typeof(DashboardPage), Title = LocalizationUtils.GetString("Guide_WelcomeTitle"), Text = LocalizationUtils.GetString("Guide_WelcomeText") },
                    new() { Title = LocalizationUtils.GetString("Guide_StartHereTitle"), Target = "RunAHiddenProcessStackPanel", Text = LocalizationUtils.GetString("Guide_StartHereText") },
                    new() { Title = LocalizationUtils.GetString("Guide_AlreadyRunningTitle"), Target = "HideARunningProcessStackPanel", Text = LocalizationUtils.GetString("Guide_AlreadyRunningText") },
                    new() { Title = LocalizationUtils.GetString("Guide_CantFindTitle"), Target = "FindProcessByClickButton", Text = LocalizationUtils.GetString("Guide_CantFindText") },
                    new() { Page = typeof(DashboardPage), Title = LocalizationUtils.GetString("Guide_FinerControlTitle"), Target = "FindAndHideStackPanel", Text = LocalizationUtils.GetString("Guide_FinerControlText") },
                    new() { Page = typeof(QuickLaunchPage), Title = LocalizationUtils.GetString("Guide_QuickLaunchOftenTitle"), Text = LocalizationUtils.GetString("Guide_QuickLaunchOftenText") },
                    new() {
                        Page = typeof(QuickLaunchPage),
                        Action = () => {
                            if (_configProvider.Config!.QuickLaunchEntries.Count == 0)
                            {
                                _configProvider.Config!.QuickLaunchEntries.Add(new QuickLaunchEntry {
                                    Name = LocalizationUtils.GetString("Guide_TempQuickLaunchEntryName")
                                });
                                _tempQuickLaunchEntry = true;
                            }
                        },
                        Title = LocalizationUtils.GetString("Guide_SimpleAndQuickTitle"),
                        Target = "QuickLaunchVirtualizingItemsControl",
                        Text = LocalizationUtils.GetString("Guide_SimpleAndQuickText")
                    },
                    new() { Page = typeof(WindowRulesPage), Title = LocalizationUtils.GetString("Guide_AutoHideTitle"), Text = LocalizationUtils.GetString("Guide_AutoHideText") },
                    new() {
                        Page = typeof(WindowRulesPage),
                        Action = () => {
                            if (_configProvider.Config!.WindowRules.Count == 0)
                            {
                                _configProvider.Config!.WindowRules.Add(new WindowRule {
                                    Value = "chrome.exe",
                                });
                                _tempWindowRule = true;
                            }
                        },
                        Title = LocalizationUtils.GetString("Guide_ManyFilteringOptionsTitle"),
                        Target = "WindowRulesItemsControl",
                        Text = LocalizationUtils.GetString("Guide_ManyFilteringOptionsText")
                    },
                    new() { Page = typeof(SettingsPage), Title = LocalizationUtils.GetString("Guide_TweakSettingsTitle"), Text = LocalizationUtils.GetString("Guide_TweakSettingsText") },
                    new() { Page = typeof(SettingsPage), Title = LocalizationUtils.GetString("Guide_AllSetTitle"), Text = LocalizationUtils.GetString("Guide_AllSetText") }
                };
            }

            _index = 0;
            _overlay.Visibility = Visibility.Visible;
            RenderStep();
        }

        public void Next()
        {
            if (_index < 0) return;

            if (_index + 1 < Steps.Count)
            {
                _index++;
                RenderStep();
            }
            else
            {
                Skip(true);
            }

            (_overlay?.BackCommand as RelayCommand)?.NotifyCanExecuteChanged();
        }

        public void Back()
        {
            if (_index <= 0) return;

            _index--;
            RenderStep();

            (_overlay?.BackCommand as RelayCommand)?.NotifyCanExecuteChanged();
        }

        public void Skip(bool remember)
        {
            if (_tempQuickLaunchEntry)
            {
                _configProvider.Config!.QuickLaunchEntries.RemoveAt(0);
                _tempQuickLaunchEntry = false;
            }

            if (_tempWindowRule)
            {
                _configProvider.Config!.WindowRules.RemoveAt(0);
                _tempWindowRule = false;
            }

            KeyboardNavigation.SetTabNavigation(Application.Current.MainWindow, KeyboardNavigationMode.Continue);
            KeyboardNavigation.SetControlTabNavigation(Application.Current.MainWindow, KeyboardNavigationMode.Continue);
            KeyboardNavigation.SetDirectionalNavigation(Application.Current.MainWindow, KeyboardNavigationMode.Continue);

            if (_overlay is not null) _overlay.Visibility = Visibility.Collapsed;
            _index = -1;
            if (remember)
            {
                _configProvider.Config!.TourCompleted = true;
                _configProvider.Save();
            }
        }

        public void RefreshAnchor()
        {
            if (_overlay is null || _overlay.Visibility != Visibility.Visible || _index < 0) return;
            var step = Steps[_index];

            if (step.Target is not null)
            {
                _overlay.HighlightTarget = FindByNameOrFirstItem(step.Target);
            }

            _overlay.Reposition();
        }

        public void ResetSeenFlag() => _configProvider.Config!.TourCompleted = false;

        public bool IsDismissed() => _configProvider.Config!.TourCompleted;

        private void RenderStep()
        {
            if (Application.Current.MainWindow is null) return;

            var step = Steps[_index];

            if (step.Page is not null) _navigationService.Navigate(step.Page);

            step.Action?.Invoke();

            Application.Current.MainWindow.Dispatcher.InvokeAsync(() =>
            {
                _overlay.TitleText = step.Title;
                _overlay.BodyText = step.Text;

                _overlay.HighlightTarget = step.Target is not null ? FindByNameOrFirstItem(step.Target) : null;

                _overlay?.Reposition();
            }, DispatcherPriority.Background);
        }

        public FrameworkElement? FindByNameOrFirstItem(string childName, DependencyObject? parent = null)
        {
            var found = InternalFindByName(childName, parent ?? Application.Current.MainWindow);
            if (found is null) return null;

            if (found is ItemsControl ic)
                return GetFirstFromItemsControl(ic) ?? found;

            if (found is Decorator dec)
                return dec.Child as FrameworkElement ?? found;

            return found;
        }

        private FrameworkElement? GetFirstFromItemsControl(ItemsControl ic)
        {
            if (!ic.HasItems) return null;

            ic.ApplyTemplate();
            ic.UpdateLayout();

            DependencyObject? container = ic.ItemContainerGenerator.ContainerFromIndex(0);

            if (container == null)
            {
                switch (ic)
                {
                    case ListBox lb when lb.Items.Count > 0:
                        lb.ScrollIntoView(lb.Items[0]);
                        lb.UpdateLayout();
                        container = ic.ItemContainerGenerator.ContainerFromIndex(0);
                        break;
                    case ListView lv when lv.Items.Count > 0:
                        lv.ScrollIntoView(lv.Items[0]);
                        lv.UpdateLayout();
                        container = ic.ItemContainerGenerator.ContainerFromIndex(0);
                        break;
                }
            }

            if (container == null) return null;

            if (container is FrameworkElement feContainer)
            {
                feContainer.ApplyTemplate();
                var contentPresenter = FindFirstVisualChild<ContentPresenter>(feContainer);
                if (contentPresenter != null)
                {
                    contentPresenter.ApplyTemplate();

                    var templatedRoot = FindFirstVisualChild<Border>(contentPresenter);
                    if (templatedRoot != null) return templatedRoot;
                }

                return FindFirstVisualChild<FrameworkElement>(feContainer) ?? feContainer;
            }

            return null;
        }

        private T? FindFirstVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;
            int n = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < n; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T t) return t;
                var nested = FindFirstVisualChild<T>(child);
                if (nested != null) return nested;
            }
            return null;
        }

        private FrameworkElement? InternalFindByName(string childName, DependencyObject? parent)
        {
            if (parent is null) return null;

            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is FrameworkElement fe && fe.Name == childName)
                    return fe;

                var found = InternalFindByName(childName, child);
                if (found != null) return found;
            }

            if (count == 0)
            {
                foreach (var logical in LogicalTreeHelper.GetChildren(parent).OfType<DependencyObject>())
                {
                    var found = InternalFindByName(childName, logical);
                    if (found != null) return found;
                }
            }

            return null;
        }
    }
}
