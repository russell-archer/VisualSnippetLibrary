using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace VisualSnippetLibrary.UserControls
{
    public sealed partial class PreferencesUserControl : UserControl
    {
        public PreferencesUserControl()
        {
            this.InitializeComponent();

            // Initialize the ToggleSwitch from roaming settings
            if (ApplicationData.Current.RoamingSettings.Values.ContainsKey("SearchFromCurrentFolder"))
                ToggleSearchStartPath.IsOn = (bool)ApplicationData.Current.RoamingSettings.Values["SearchFromCurrentFolder"];
            else
                ToggleSearchStartPath.IsOn = true;

            LocalSnippetLibraryPath.Text = App.RootSnippetPath;

        }

        private void OnToggledSearchStart(object sender, RoutedEventArgs e)
        {
            ApplicationData.Current.RoamingSettings.Values["SearchFromCurrentFolder"] = ToggleSearchStartPath.IsOn;
        }

        private void CopyPath(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            try
            {
                // Copy the path to the clipboard
                var dataPackage = new DataPackage();
                dataPackage.RequestedOperation = DataPackageOperation.Copy;
                dataPackage.SetText(App.RootSnippetPath);
                Clipboard.SetContent(dataPackage);
            }
            catch
            {
                // Throw away the non-critical exception
            }
        }
    }
}
