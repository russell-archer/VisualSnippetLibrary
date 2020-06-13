using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;

namespace VisualSnippetLibrary
{
    public sealed partial class HelpPage : Common.LayoutAwarePage
    {
        public ObservableCollection<HelpItem> HelpItems { get; set; }

        private static readonly Uri ImageBaseUri = new Uri("ms-appx:///");

        public HelpPage()
        {
            this.InitializeComponent();

            HelpItems = new ObservableCollection<HelpItem>();
            HelpItems.Add(new HelpItem { HelpImage = ImageBaseUri + "Assets/screenshot_00.png" });
            HelpItems.Add(new HelpItem { HelpImage = ImageBaseUri + "Assets/txt_00.png" });
            HelpItems.Add(new HelpItem { HelpImage = ImageBaseUri + "Assets/screenshot_02.png" });
            HelpItems.Add(new HelpItem { HelpImage = ImageBaseUri + "Assets/txt_01.png" });
            HelpItems.Add(new HelpItem { HelpImage = ImageBaseUri + "Assets/screenshot_04.png" });
            HelpItems.Add(new HelpItem { HelpImage = ImageBaseUri + "Assets/txt_02.png" });
            HelpItems.Add(new HelpItem { HelpImage = ImageBaseUri + "Assets/screenshot_05.png" });
            HelpItems.Add(new HelpItem { HelpImage = ImageBaseUri + "Assets/screenshot_06.png" });

            GridHelpItems.ItemsSource = HelpItems;

            // Initialize the ToggleSwitch from roaming settings
            if (ApplicationData.Current.RoamingSettings.Values.ContainsKey("ShowHelpPage"))
                ShowAtStartupToggle.IsOn = (bool) ApplicationData.Current.RoamingSettings.Values["ShowHelpPage"];
            else
            {
                ShowAtStartupToggle.IsOn = false; // Off by default
                ApplicationData.Current.RoamingSettings.Values["ShowHelpPage"] = false;
            }
        }

        protected override void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
        {
        }

        protected override void SaveState(Dictionary<String, Object> pageState)
        {
        }

        private void BtnStartTapped(object sender, TappedRoutedEventArgs e)
        {
            Frame.Navigate(typeof (HomePage), null);
        }

        private void ShowToggled(object sender, RoutedEventArgs e)
        {
            ApplicationData.Current.RoamingSettings.Values["ShowHelpPage"] = ShowAtStartupToggle.IsOn;
        }
    }

    public class HelpItem 
    {
        public string HelpImage { get; set; }
    }
}
