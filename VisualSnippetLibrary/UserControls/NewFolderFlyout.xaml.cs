using System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace VisualSnippetLibrary.UserControls
{
    public sealed partial class NewFolderFlyout : UserControl
    {
        public event EventHandler CloseAndCreate;

        public string NewFolderName { get; set; }

        public NewFolderFlyout()
        {
            this.InitializeComponent();
        }

        private void CreateTapped(object sender, TappedRoutedEventArgs e)
        {
            OnCloseAndCreate();
        }

        private void OnCloseAndCreate()
        {
            if (CloseAndCreate != null) CloseAndCreate(this, EventArgs.Empty);
        }
    }
}
