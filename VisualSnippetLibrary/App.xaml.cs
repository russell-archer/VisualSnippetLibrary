using Callisto.Controls;
using VisualSnippetLibrary.Common;
using System;
using VisualSnippetLibrary.UserControls;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Storage;
using Windows.UI.ApplicationSettings;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace VisualSnippetLibrary
{
    sealed partial class App : Application
    {
        public const string RootSnippetFolderName = "Visual Snippet Library";
        public const string RootSnippetFolderAlias = "Visual Snippet Library";
        public static string RootSnippetPath;

        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

        protected override async void OnLaunched(LaunchActivatedEventArgs args)
        {
            var rootFrame = Window.Current.Content as Frame;

            if (rootFrame == null)
            {
                rootFrame = new Frame();

                try
                {
                    // Create a background image to be used on all pages
                    rootFrame.Background = new ImageBrush
                    {
                        ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/AppBackground.png")),
                        Stretch = Stretch.UniformToFill
                    };
                }
                catch
                {
                    // Quietly ignore this error for setting the background image
                }

                SuspensionManager.RegisterFrame(rootFrame, "AppFrame");

                if (args.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    try
                    {
                        await SuspensionManager.RestoreAsync();
                    }
                    catch (SuspensionManagerException)
                    {
                    }
                }

                Window.Current.Content = rootFrame;
            }

            if (rootFrame.Content == null)
            {
                // Navigate to the snippet explorer page
                if (!rootFrame.Navigate(typeof(HomePage))) throw new Exception("Failed to create initial page");
            }

            Window.Current.Activate();

            // Register handler for CommandsRequested events from the settings charm (for the About box)
            SettingsPane.GetForCurrentView().CommandsRequested += OnCommandsRequested;
        }

        private void OnCommandsRequested(SettingsPane sender, SettingsPaneCommandsRequestedEventArgs args)
        {
            // Add an About command using a Callisto SettingsFlyout
            var about = new SettingsCommand("about", "About", handler =>
            {
                new SettingsFlyout
                {
                    Content = new AboutUserControl(),
                    Background = Application.Current.Resources["AboutBoxBrush"] as SolidColorBrush,
                    HeaderBrush = Application.Current.Resources["AboutBoxBrush"] as SolidColorBrush,
                    HeaderText = "About",
                    SmallLogoImageSource = new BitmapImage { UriSource = new Uri("ms-appx:///Assets/SmallLogo.png") },
                    IsOpen = true
                };
            });

            args.Request.ApplicationCommands.Add(about);

            // Add an About command using a Callisto SettingsFlyout
            var preferences = new SettingsCommand("preferences", "Preferences", handler =>
            {
                new SettingsFlyout
                {
                    Content = new PreferencesUserControl(),
                    Background = Application.Current.Resources["AboutBoxBrush"] as SolidColorBrush,
                    HeaderBrush = Application.Current.Resources["AboutBoxBrush"] as SolidColorBrush,
                    HeaderText = "Preferences",
                    SmallLogoImageSource = new BitmapImage { UriSource = new Uri("ms-appx:///Assets/SmallLogo.png") },
                    IsOpen = true
                };
            });

            args.Request.ApplicationCommands.Add(preferences);
        }

        private async void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            await SuspensionManager.SaveAsync();
            deferral.Complete();
        }

        protected async override void OnSearchActivated(SearchActivatedEventArgs args)
        {
            // OnSearchActivated is called in lieu of OnLaunched if the operating system launches the app from the search pane

            if (args.PreviousExecutionState == ApplicationExecutionState.NotRunning ||
                args.PreviousExecutionState == ApplicationExecutionState.ClosedByUser ||
                args.PreviousExecutionState == ApplicationExecutionState.Terminated)
            {
                // The app was launched by the user initiating a search
                var rootFrame = new Frame();

                SuspensionManager.RegisterFrame(rootFrame, "AppFrame");
                Window.Current.Content = rootFrame;

                try
                {
                    // Create a background image to be used on all pages
                    rootFrame.Background = new ImageBrush
                    {
                        ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/AppBackground.png")),
                        Stretch = Stretch.UniformToFill
                    };
                }
                catch
                {
                    // Quietly ignore this error for setting the background image
                }

                // Make sure any search starts from the root folder
                ApplicationData.Current.RoamingSettings.Values["CurrentSnippetPath"] = RootSnippetPath;

                // Register handler for CommandsRequested events from the settings charm (for the About box)
                SettingsPane.GetForCurrentView().CommandsRequested += OnCommandsRequested;
            }

            var previousContent = Window.Current.Content;
            var frame = previousContent as Frame;

            if (frame == null)
            {
                frame = new Frame();
                SuspensionManager.RegisterFrame(frame, "AppFrame");

                if (args.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    try
                    {
                        await SuspensionManager.RestoreAsync();
                    }
                    catch (SuspensionManagerException)
                    {
                    }
                }
            }

            frame.Navigate(typeof(HomePage), args.QueryText);
            Window.Current.Content = frame;
            Window.Current.Activate();
        }
    }
}
