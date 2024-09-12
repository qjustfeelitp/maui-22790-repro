using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using Polly;
using ContentPresenter = Microsoft.UI.Xaml.Controls.ContentPresenter;
using Frame = Microsoft.UI.Xaml.Controls.Frame;
using HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment;
using NavigationEventArgs = Microsoft.UI.Xaml.Navigation.NavigationEventArgs;
using Page = Microsoft.UI.Xaml.Controls.Page;
using VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment;

namespace Maui22790;

internal sealed class ExtendedNavigationViewHandler : NavigationViewHandler
{
    private StackNavigationManager? stackNavigationManager;

    /// <inheritdoc />
    protected override StackNavigationManager CreateNavigationManager()
    {
        return this.stackNavigationManager ??= new ExtendedStackNavigationManager(this.MauiContext!);
    }

    /// <summary>
    /// This is basically copy of base class with modifications in <see cref="OnNavigated"/>
    /// </summary>
    private sealed class ExtendedStackNavigationManager : StackNavigationManager
    {
        private IView? currentPage;
        private Frame? navigationFrame;
        private Action? pendingNavigationFinished;
        private bool connected;
        private IStackNavigation? NavigationView { get; set; }

        /// <inheritdoc />
        public ExtendedStackNavigationManager(IMauiContext mauiContext) : base(mauiContext)
        { }

        public override void Connect(IStackNavigation navigationView, Frame frame)
        {
            this.connected = true;

            if (this.navigationFrame != null)
            {
                this.navigationFrame.Navigated -= OnNavigated;
            }

            FirePendingNavigationFinished();

            this.navigationFrame = frame;

            this.navigationFrame.Navigated += OnNavigated;
            this.NavigationView = navigationView;

            if (this.WindowManager?.RootView is NavigationView nv)
            {
                nv.IsPaneVisible = true;
            }
        }

        public override void Disconnect(IStackNavigation navigationView, Frame frame)
        {
            this.connected = false;

            if (this.navigationFrame != null)
            {
                this.navigationFrame.Navigated -= OnNavigated;
            }

            FirePendingNavigationFinished();
            this.navigationFrame = null;
            this.NavigationView = null;
        }

        public override void NavigateTo(NavigationRequest args)
        {
            IReadOnlyList<IView> newPageStack = new List<IView>(args.NavigationStack);
            var previousNavigationStack = this.NavigationStack;
            int previousNavigationStackCount = previousNavigationStack.Count;
            bool initialNavigation = this.NavigationStack.Count == 0;

            // User has modified navigation stack but not the currently visible page
            // So we just sync the elements in the stack
            if (!initialNavigation &&
                (newPageStack[^1] ==
                 previousNavigationStack[previousNavigationStackCount - 1]))
            {
                SyncBackStackToNavigationStack(newPageStack);
                this.NavigationStack = newPageStack;
                FireNavigationFinished();

                return;
            }

            var transition = GetNavigationTransition(args);
            this.currentPage = newPageStack[^1];

            _ = this.currentPage ?? throw new InvalidOperationException("Navigation Request Contains Null Elements");

            if (previousNavigationStack.Count < args.NavigationStack.Count)
            {
                var destinationPageType = GetDestinationPageType();
                this.NavigationStack = newPageStack;
                this.navigationFrame?.Navigate(destinationPageType, null, transition);
            }
            else if (previousNavigationStack.Count == args.NavigationStack.Count)
            {
                var destinationPageType = GetDestinationPageType();
                this.NavigationStack = newPageStack;
                this.navigationFrame?.Navigate(destinationPageType, null, transition);
            }
            else
            {
                this.NavigationStack = newPageStack;
                this.navigationFrame?.GoBack(transition);
            }
        }

        protected override Type GetDestinationPageType()
        {
            return typeof(Page);
        }

        protected override NavigationTransitionInfo? GetNavigationTransition(NavigationRequest args)
        {
            if (!args.Animated)
            {
                return null;
            }

            // GoBack just plays the animation in reverse so we always just return the same animation
            return new SlideNavigationTransitionInfo
            {
                Effect = SlideNavigationTransitionEffect.FromRight
            };
        }

        private void SyncBackStackToNavigationStack(IReadOnlyList<IView> pageStack)
        {
            if (this.navigationFrame is null)
            {
                return;
            }

            // Back stack depth doesn't count the currently visible page
            int nativeStackCount = this.navigationFrame.BackStackDepth + 1;

            // BackStack entries have no hard relationship with a specific IView
            // Everytime an entry is about to become visible it just grabs whatever
            // IView is going to be the visible so all we're doing here is syncing
            // up the number of things on the stack
            while (nativeStackCount != pageStack.Count)
            {
                if (nativeStackCount > pageStack.Count)
                {
                    this.navigationFrame.BackStack.RemoveAt(0);
                }
                else
                {
                    this.navigationFrame.BackStack.Insert(0, new PageStackEntry(GetDestinationPageType(), null, null));
                }

                nativeStackCount = this.navigationFrame.BackStackDepth + 1;
            }
        }

        // This is used to fire NavigationFinished back to the xplat view
        // Firing NavigationFinished from Loaded is the latest reliable point
        // in time that I know of for firing `NavigationFinished`
        // Ideally we could fire it when the `NavigationTransitionInfo` is done but
        // I haven't found a way to do that
        private void OnNavigated(object sender, NavigationEventArgs e)
        {
            if (sender is not Frame frame)
            {
                return;
            }

            // If the user has inserted or removed any extra pages
            SyncBackStackToNavigationStack(this.NavigationStack);

            ProcessNavigation(frame, e);
        }

        /// <summary>
        /// Process navigation.
        /// </summary>
        /// <param name="frame">Frame</param>
        /// <param name="eventArgs">Event args</param>
        private void ProcessNavigation(UIElement frame, NavigationEventArgs eventArgs)
        {
            if (eventArgs.Content is not (Page page and FrameworkElement fe))
            {
                return;
            }

            try
            {
                Policy.Handle<Exception>()
                      .Retry(10)
                      .Execute(() => SetContent(frame, page, this.MauiContext));
            }
            catch (Exception ex)
            {
                try
                {
                    Policy.Handle<Exception>()
                          .Retry(10)
                          .Execute(() =>
                           {
                               // create new context as a fallback, it will crash but then it will pick it up, it seems that it is due to invalid initial XamlRoot, this property can not be changed after it has been set
                               SetContent(frame, page, new MauiContext(this.MauiContext.Services));
                           });
                }
                catch (Exception exception)
                {
                    FireNavigationFinished();

                    throw;
                }
            }

            this.pendingNavigationFinished = () =>
            {
                if (page.Content is not FrameworkElement pc)
                {
                    FireNavigationFinished();
                }
                else
                {
                    OnLoaded(pc, FireNavigationFinished);
                }

                if (this.NavigationView is IView view && this.connected)
                {
                    Arrange(view, fe);
                }
            };

            OnLoaded(fe, FirePendingNavigationFinished);
        }

        /// <summary>
        /// Sets content to page.
        /// </summary>
        /// <param name="frame">Frame</param>
        /// <param name="page">Page</param>
        /// <param name="mauiContext"></param>
        private void SetContent(UIElement frame, UserControl page, IMauiContext mauiContext)
        {
            if (this.currentPage is null)
            {
                return;
            }

            frame.XamlRoot ??= this.WindowManager.RootView.XamlRoot ?? WindowStateManager.Default.GetActiveWindow()?.Content.XamlRoot;
            page.XamlRoot ??= frame.XamlRoot;

            var presenter = new ContentPresenter
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                XamlRoot = page.XamlRoot
            };

            var content = this.currentPage.ToPlatform(mauiContext);
            content.XamlRoot ??= presenter.XamlRoot;

            presenter.Content = content;
            page.Content = presenter;
        }

        private void FireNavigationFinished()
        {
            this.pendingNavigationFinished = null;
            this.NavigationView?.NavigationFinished(this.NavigationStack);
        }

        private void FirePendingNavigationFinished()
        {
            Interlocked.Exchange(ref this.pendingNavigationFinished, null)?.Invoke();
        }

        private static bool IsLoaded(FrameworkElement? frameworkElement)
        {
            if (frameworkElement == null)
            {
                return false;
            }

            return frameworkElement.IsLoaded;
        }

        private static IDisposable OnLoaded(FrameworkElement? frameworkElement, Action action)
        {
            if (IsLoaded(frameworkElement))
            {
                action();

                return new ActionDisposable(() => { });
            }

            RoutedEventHandler? routedEventHandler = null;

            var disposable = new ActionDisposable(() =>
            {
                if (routedEventHandler != null)
                {
                    frameworkElement!.Loaded -= routedEventHandler;
                }
            });

            routedEventHandler = (_, __) =>
            {
                disposable.Dispose();
                action();
            };

            frameworkElement!.Loaded += routedEventHandler;

            return disposable;
        }

        private static void Arrange(IView view, FrameworkElement frameworkElement)
        {
            var rect = new Rect(0, 0, frameworkElement.ActualWidth, frameworkElement.ActualHeight);

            if (!view.Frame.Equals(rect))
            {
                view.Arrange(rect);
            }
        }

        private sealed class ActionDisposable : IDisposable
        {
            private volatile Action? action;

            public ActionDisposable(Action action)
            {
                this.action = action;
            }

            public void Dispose()
            {
                Interlocked.Exchange(ref this.action, null)?.Invoke();
            }
        }
    }
}
