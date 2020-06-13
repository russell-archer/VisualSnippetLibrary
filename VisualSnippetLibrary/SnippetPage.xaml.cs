using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Callisto.Controls;
using VisualSnippetLibrary.DataModel;
using VisualSnippetLibrary.UserControls;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Navigation;

namespace VisualSnippetLibrary
{
    public sealed partial class SnippetPage : Common.LayoutAwarePage
    {
        private readonly ISnippetRepository _snippetRepository;
        private bool _unsavedChanges;
        private CodeSnippet _savedSnippet;  // Holds a copy of the snippet while editing takes place (so we can roll back)

        public CodeSnippet Snippet { get; set; }
        public bool IsEditing { get; set; }
        public bool IsCodeEditorExpanded { get; set; }

        public SnippetPage()
        {
            this.InitializeComponent();

            _unsavedChanges = false;
            _snippetRepository = new SnippetRepository();
        }

        private void ApplicationViewStatesOnCurrentStateChanged(object sender, VisualStateChangedEventArgs visualStateChangedEventArgs)
        {
            switch (visualStateChangedEventArgs.NewState.Name)
            {
                case "FullScreenLandscape":
                case "Filled":
                    IsCodeEditorExpanded = false;
                    VisualStateManager.GoToState(this, "LocalFileSaveCollapsed", true);
                    VisualStateManager.GoToState(this, IsEditing ? "EditModeOnContractCodeEditor" : "EditModeOffContractCodeEditor", true);
                    break;

                case "FullScreenPortrait":
                case "Snapped":
                    IsCodeEditorExpanded = true;
                    VisualStateManager.GoToState(this, "LocalFileSaveCollapsed", true);
                    VisualStateManager.GoToState(this, IsEditing ? "EditModeOnExpandCodeEditor" : "EditModeOffExpandCodeEditor", true);
                    GridSnippetFieldsExpandedCol0.Width = new GridLength(0, GridUnitType.Star);  // Hide the expand/contract button
                    break;
            }
        }

        protected override void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
        {
        }

        protected override void SaveState(Dictionary<String, Object> pageState)
        {
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            // Hook into visual state changes 
            ApplicationViewStates.CurrentStateChanged += ApplicationViewStatesOnCurrentStateChanged;

            // Register for DataRequested events (Share contract) - this has to be done in each page that can share it's data
            DataTransferManager.GetForCurrentView().DataRequested += OnShareDataRequested;

            // A null parameter means the user wants to create a new snippet from scratch
            if (e.Parameter == null)
            {
                BtnNewSnippetTapped(this, null);
                return;
            }

            try
            {
                // We either need to prompt the user to open a file from the document library (e.Parameter will contain the string "{prompt}"),
                // or we open a specified file (the e.Parameter will contain a string path to the file)...
                var path = e.Parameter as string;
                if (String.CompareOrdinal(path, "{prompt}") == 0)
                {
                    var file = await FileSystemUtils.SelectSnippetFromDocumentsLibraryAsync();
                    if (file == null)
                    {
                        // User cancelled the file picker
                        Frame.Navigate(typeof (HomePage));
                        return;
                    }

                    Snippet = await _snippetRepository.GetSnippetAsync(file);
                }
                else Snippet = await _snippetRepository.GetSnippetAsync(path);

                GridSnippetFields.DataContext = Snippet;
                GridSnippetFieldsExpanded.DataContext = Snippet;

                if (Snippet.Keywords != null && Snippet.Keywords.Count > 0) ComboKeywords.SelectedIndex = 0;
                if (Snippet.Assemblies != null && Snippet.Assemblies.Count > 0) ComboAssemblies.SelectedIndex = 0;
                if (Snippet.Namespaces != null && Snippet.Namespaces.Count > 0) ComboNamespaces.SelectedIndex = 0;
                if (Snippet.DeclarationList != null && Snippet.DeclarationList.Count > 0) comboDeclarations.SelectedIndex = 0;
            }
            catch
            {
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            // De-Register for DataRequested events (Share contract) - this MUST be done or an exception will be thrown if the page is re-visited
            DataTransferManager.GetForCurrentView().DataRequested -= OnShareDataRequested;
        }

        private void OnShareDataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            // Share... This method's called when the user invokes the share charm
            if (Snippet == null) return;

            // Setup the summary data shown when the share charm's invoked
            args.Request.Data.Properties.Title = Snippet.Title;
            args.Request.Data.Properties.Description = Snippet.Description;

            // Share the text - this is what actually gets shared with the other app
            args.Request.Data.SetText(Snippet.Code);

        }

        private void CloseAppBar()
        {
            var bottomAppBar = this.BottomAppBar;
            if (bottomAppBar != null) bottomAppBar.IsOpen = false;
        }

        private void BtnEditSnippetTapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            try
            {
                _savedSnippet = CodeSnippet.Copy(Snippet);  // Save changes for possible roll-back
                IsEditing = true;
                VisualStateManager.GoToState(this, IsCodeEditorExpanded ? "EditModeOnExpandCodeEditor" : "EditModeOnContractCodeEditor", true);
                _unsavedChanges = true;
                CloseAppBar();
            }
            catch
            {
            }
        }

        private void ResetFields()
        {
            try
            {
                IsEditing = false;
                VisualStateManager.GoToState(this, IsCodeEditorExpanded ? "EditModeOffExpandCodeEditor" : "EditModeOffContractCodeEditor", true);
                _unsavedChanges = false;
            }
            catch
            {
            }
        }

        private void BtnSaveSnippetTapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            // Save As...
            try
            {
                CloseAppBar();

                VisualStateManager.GoToState(this, "LocalFileSaveVisible", true);
                LocalFileSaveUserControl.Snippet = Snippet;
                LocalFileSaveUserControl.GridViewFolders.Width = this.ActualWidth-100;  // Hack to get the grid to scroll (the 100 is the 50,50 left/right margin)
                LocalFileSaveUserControl.PickerClosed += async (o, args) => 
                {
                    VisualStateManager.GoToState(this, "LocalFileSaveCollapsed", true);

                    var arg = args as LocalFilePickerClosedEventArgs;
                    if (arg == null || arg.OperationCancelled) return;

                    // Write the file
                    var result = await _snippetRepository.WriteSnippetAsync(Snippet.Path, Snippet);
                    if (result)
                    {
                        var dlg = new MessageDialog("Snippet saved OK");
                        await dlg.ShowAsync();
                        ResetFields();
                    }
                    else
                    {
                        var dlg = new MessageDialog("Sorry, unable to save the snippet to " + FileSystemUtils.GetLocalDataStoreRelativeSnippetRootPath(Snippet.Path));
                        await dlg.ShowAsync();                
                    }
                };
            }
            catch
            {
            }
        }

        private void BtnCopySnippetTapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            try
            {
                // Copy the snippet code to the clipboard
                var dataPackage = new DataPackage();
                dataPackage.RequestedOperation = DataPackageOperation.Copy;
                dataPackage.SetText(Snippet.Code);
                Clipboard.SetContent(dataPackage);
                CloseAppBar();
            }
            catch
            {
                // Throw away the non-critical exception
            }
        }

        private async void BtnNewSnippetTapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            Snippet = await CodeSnippet.CreateNew();
            GridSnippetFields.DataContext = Snippet;
            GridSnippetFieldsExpanded.DataContext = Snippet;
            BtnEditSnippetTapped(this, null);
        }

        private async void BtnLoadSnippetTapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            try
            {
                // Create the file open picker
                var picker = new FileOpenPicker();

                // Set the type of file to pick
                picker.FileTypeFilter.Add(".snippet");
                picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

                // Single-file selection
                var file = await picker.PickSingleFileAsync();
                if (file == null) return;

                Snippet = await _snippetRepository.GetSnippetAsync(file);
                ResetFields();
                GridSnippetFields.DataContext = Snippet;
                GridSnippetFieldsExpanded.DataContext = Snippet;
                CloseAppBar();
            }
            catch
            {
            }
        }

        protected async override void GoBack(object sender, RoutedEventArgs e)
        {
            if(await SaveChangesPrompt()) BtnSaveSnippetChangesTapped(this, null);

            base.GoBack(sender, e);
        }

        private async Task<bool> SaveChangesPrompt()
        {
            if (_unsavedChanges)
            {
                var saveChanges = false;
                var dlg = new MessageDialog("You have unsaved changes to the current snippet.");
                dlg.Commands.Add(new UICommand("Save Changes", command => { saveChanges = true; }));
                dlg.Commands.Add(new UICommand("Discard Changes", command => { }));

                dlg.DefaultCommandIndex = 0;
                dlg.CancelCommandIndex = 1;

                await dlg.ShowAsync();

                return saveChanges;
            }
            
            return false;
        }

        private void BtnCancelEditSnippetTapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            Snippet = _savedSnippet;
            GridSnippetFields.DataContext = Snippet;
            GridSnippetFieldsExpanded.DataContext = Snippet;
            CloseAppBar();

            ResetFields();
        }

        private async void BtnSaveSnippetChangesTapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            // Save changes back to the current file
            CloseAppBar();

            if (string.IsNullOrEmpty(Snippet.Path))
            {
                // File has never been saved before - do save as
                BtnSaveSnippetTapped(this, null);
                return;
            }

            // Write the file
            var result = await _snippetRepository.WriteSnippetAsync(Snippet.Path, Snippet);
            if (result)
            {
                var dlg = new MessageDialog("Snippet saved as \"" + FileSystemUtils.GetLocalDataStoreRelativeSnippetRootPath(Snippet.Path) + "\"");
                await dlg.ShowAsync();
                ResetFields();
            }
            else
            {
                // "Sorry, unable to save the snippet to "
                var dlg = new MessageDialog("Sorry, unable to save the snippet as \"" + FileSystemUtils.GetLocalDataStoreRelativeSnippetRootPath(Snippet.Path) + "\"");
                await dlg.ShowAsync();
            }
        }

        private void ButtonKeywordsEditTapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            DoStringCollectionEditFlyout<StringCollectionFlyout>(sender as Button, ComboKeywords, Snippet.Keywords, "Keywords");
        }

        private void ButtonAssembliesEditTapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            DoStringCollectionEditFlyout<StringCollectionFlyout>(sender as Button, ComboAssemblies, Snippet.Assemblies, "Assemblies");
        }

        private void ButtonNamespacesEditTapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            DoStringCollectionEditFlyout<StringCollectionFlyout>(sender as Button, ComboNamespaces, Snippet.Namespaces, "Namespaces");
        }

        private void DoStringCollectionEditFlyout<TFlyoutContent>(Control sender, ComboBox comboToUpdate, ObservableCollection<string> collection, string snippetCollectionPropertyName)
        {
            CloseAppBar();

            try
            {
                if(collection == null) collection = new ObservableCollection<string>();

                var flyout = new Flyout();
                var tmpCollection = new ObservableCollection<string>(collection);
                var content = Activator.CreateInstance(typeof (TFlyoutContent), tmpCollection, snippetCollectionPropertyName);
                var flyoutCancelled = false;

                flyout.Content = content;
                flyout.Placement = PlacementMode.Top;
                flyout.PlacementTarget = sender as Button;

                flyout.KeyDown += (o, args) =>
                {
                    if (args.Key == VirtualKey.Escape) flyoutCancelled = true;
                    else if (args.Key == VirtualKey.Enter) flyout.IsOpen = false;
                };

                flyout.Closed += (o, o1) =>
                {
                    if (flyoutCancelled) return;

                    collection.Clear();
                    foreach (var s in tmpCollection)
                        collection.Add(s);

                    if (collection.Count > 0) comboToUpdate.SelectedIndex = 0;

                    _unsavedChanges = true;
                };

                flyout.IsOpen = true;
            }
            catch
            {
            }

        }

        private void ButtonDeclarationsEditTapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            CloseAppBar();

            if(Snippet.DeclarationList == null) Snippet.DeclarationList = new ObservableCollection<Declaration>();

            var flyout = new Flyout();
            var tmpCollection = new ObservableCollection<Declaration>(Snippet.DeclarationList);
            var content = new DeclarationCollectionFlyout(tmpCollection);
            var flyoutCancelled = false;

            flyout.Content = content;
            flyout.Placement = PlacementMode.Top;
            flyout.PlacementTarget = sender as Button;

            flyout.KeyDown += (o, args) =>
            {
                if (args.Key == VirtualKey.Escape) flyoutCancelled = true;
                else if (args.Key == VirtualKey.Enter) flyout.IsOpen = false;
            };

            flyout.Closed += (o, o1) =>
            {
                if (flyoutCancelled) return;

                Snippet.DeclarationList.Clear();
                foreach (var declaration in tmpCollection)
                    Snippet.DeclarationList.Add(declaration);

                if (Snippet.DeclarationList.Count > 0) comboDeclarations.SelectedIndex = 0;

                _unsavedChanges = true;
            };

            flyout.IsOpen = true;
        }

        private void LangChanged(object sender, SelectionChangedEventArgs e)
        {
            if (String.Compare(Snippet.CodeLanguage, "VB", StringComparison.Ordinal) == 0)
            {
                ComboAssemblies.IsEnabled = true;
                ButtonAssembliesEdit.IsEnabled = true;
            }
            else
            {
                ComboAssemblies.IsEnabled = false;
                ButtonAssembliesEdit.IsEnabled = false;                
            }
        }

        private async void BtnRawSnippetTapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            var flyout = new Flyout();
            var rawXml = await _snippetRepository.WriteSnippetToString(Snippet);
            var content = new RawXmlFlyout(rawXml);

            flyout.Content = content;
            flyout.Placement = PlacementMode.Top;
            flyout.PlacementTarget = sender as Button;
            flyout.IsOpen = true;
            CloseAppBar();
        }

        private void ButtonExpandCodeTapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            try
            {
                IsCodeEditorExpanded = true;
                VisualStateManager.GoToState(this, IsEditing ? "EditModeOnExpandCodeEditor" : "EditModeOffExpandCodeEditor", true);
            }
            catch
            {
            }
        }

        private void ButtonContractCodeTapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            try
            {
                IsCodeEditorExpanded = false;
                VisualStateManager.GoToState(this, IsEditing ? "EditModeOnContractCodeEditor" : "EditModeOffContractCodeEditor", true);
            }
            catch
            {
            }
        }

        private async void BtnDelSnippetTapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            CloseAppBar();

            var dlg = new MessageDialog("Delete this snippet - are you sure?");
            dlg.Commands.Add(new UICommand("Yes"));
            dlg.Commands.Add(new UICommand("No"));
            var answer = await dlg.ShowAsync();
            if (String.Compare(answer.Label, "No", StringComparison.Ordinal) == 0)
                return;

            if (await FileSystemUtils.DeleteFileAsync(Snippet)) dlg = new MessageDialog("Snippet was removed successfully");
            else dlg = new MessageDialog("Unable to delete Snippet");

            await dlg.ShowAsync();

            Frame.Navigate(typeof (HomePage));
        }
    }
}
