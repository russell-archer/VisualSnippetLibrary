using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Callisto.Controls;
using VisualSnippetLibrary.Properties;
using Windows.System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

namespace VisualSnippetLibrary.UserControls
{
    public sealed partial class StringCollectionFlyout : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<string> Collection { get; set; }
        public string CollectionItemTitle { get; set; }
        public bool IsItemSelected { get { return ListViewCollection.SelectedIndex != -1; } }

        public StringCollectionFlyout()
        {
            this.InitializeComponent();

            CollectionItemTitle = "Item";  // Default, should never be used
        }

        public StringCollectionFlyout(ObservableCollection<string> collection, string collectionItemTitle)
        {
            this.InitializeComponent();

            Collection = collection;
            CollectionItemTitle = collectionItemTitle;
        }

        private void BtnEditItemTapped(object sender, TappedRoutedEventArgs e)
        {
            if (ListViewCollection.SelectedIndex == -1 || ListViewCollection.SelectedItem == null)
                return;

            // Get the actual control containing the text we want to edit
            var selectedListItem = ListViewCollection.ItemContainerGenerator.ContainerFromIndex(ListViewCollection.SelectedIndex) as ListViewItem;
            if (selectedListItem == null)
                return;
            
            var flyout = new Flyout();
            var content = new TextBox { Text = ListViewCollection.SelectedItem.ToString(), FontSize = 24 };
            var flyoutCancelled = false;

            flyout.Content = content;
            flyout.Placement = PlacementMode.Top;
            flyout.PlacementTarget = selectedListItem;

            flyout.KeyDown += (o, args) =>
            {
                if (args.Key == VirtualKey.Escape)
                    flyoutCancelled = true;
                else if (args.Key == VirtualKey.Enter)
                    flyout.IsOpen = false;
            };

            flyout.Closed += (o, o1) =>
            {
                if (flyoutCancelled)
                    return;

                Collection[ListViewCollection.SelectedIndex] = content.Text;  // Update the underlying list with the changes
            };

            flyout.IsOpen = true;       
        }

        private void BtnAddItemTapped(object sender, TappedRoutedEventArgs e)
        {
            if(Collection == null)
                Collection = new ObservableCollection<string>();

            Collection.Add(CollectionItemTitle + Collection.Count.ToString());
        }

        private void BtnDelItemTapped(object sender, TappedRoutedEventArgs e)
        {
            if (Collection == null || ListViewCollection.SelectedIndex == -1)
                return;

            var selectedItem = ListViewCollection.SelectedItem as string;

            if (selectedItem != null && !string.IsNullOrWhiteSpace(selectedItem))
                Collection.Remove(selectedItem);
        }

        private void CollectionSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            OnPropertyChanged("IsItemSelected");
        }

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) 
                handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
