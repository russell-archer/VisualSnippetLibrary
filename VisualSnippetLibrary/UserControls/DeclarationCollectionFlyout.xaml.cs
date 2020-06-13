using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Callisto.Controls;
using VisualSnippetLibrary.DataModel;
using VisualSnippetLibrary.Properties;
using Windows.System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

namespace VisualSnippetLibrary.UserControls
{
    public sealed partial class DeclarationCollectionFlyout : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<Declaration> Collection { get; set; }
        public string CollectionItemTitle { get; set; }
        public bool IsItemSelected { get { return ListViewCollection.SelectedIndex != -1; } }

        public DeclarationCollectionFlyout()
        {
            this.InitializeComponent();

            CollectionItemTitle = "Declarations";  // Default, should never be used
        }

        public DeclarationCollectionFlyout(ObservableCollection<Declaration> collection, string collectionItemTitle="Declarations")
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
            var selectedDeclaration = ListViewCollection.SelectedItem as Declaration;
            if (selectedDeclaration == null)
                return;

            var tempDeclaration = new Declaration
                                      {
                                          DeclarationType = selectedDeclaration.DeclarationType,
                                          Default = selectedDeclaration.Default,
                                          Editable = selectedDeclaration.Editable,
                                          Function = selectedDeclaration.Function,
                                          ID = selectedDeclaration.ID,
                                          ToolTip = selectedDeclaration.ToolTip,
                                          Type = selectedDeclaration.Type
                                      };

            var content = new DeclarationItemFlyout(tempDeclaration);
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

                Collection[ListViewCollection.SelectedIndex] = tempDeclaration;  // Update the underlying list with the changes
            };

            flyout.IsOpen = true;       
        }

        private void BtnAddItemTapped(object sender, TappedRoutedEventArgs e)
        {
            if(Collection == null)
                Collection = new ObservableCollection<Declaration>();

            var dec = new Declaration
            {
                ID = "ID" + Collection.Count.ToString(),
                DeclarationType = DeclarationTypes.Object,
                Default = "Default Value",
                Editable = true,
                Function = "HelloWorld($exp$)",
                ToolTip = "Tooltip text",
                Type = "Object"
            };

            Collection.Add(dec);
        }

        private void BtnDelItemTapped(object sender, TappedRoutedEventArgs e)
        {
            if (Collection == null || ListViewCollection.SelectedIndex == -1)
                return;

            var selectedItem = ListViewCollection.SelectedItem as Declaration;

            if (selectedItem != null)
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
