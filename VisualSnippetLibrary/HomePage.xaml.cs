using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Callisto.Controls;
using VisualSnippetLibrary.DataModel;
using VisualSnippetLibrary.UserControls;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

namespace VisualSnippetLibrary
{
    public sealed partial class HomePage : Common.LayoutAwarePage
    {
        private string _rootSnippetPath;
        private string RootSnippetPath
        {
            get { return _rootSnippetPath; }
            set 
            { 
                _rootSnippetPath = value; 
                App.RootSnippetPath = value;
                ApplicationData.Current.RoamingSettings.Values["RootSnippetPath"] = value;
            }
        }

        private string _currentSnippetPath;
        private string CurrentSnippetPath
        {
            get { return _currentSnippetPath; }
            set 
            { 
                _currentSnippetPath = value;
                ApplicationData.Current.RoamingSettings.Values["CurrentSnippetPath"] = value;
            }
        }

        public HomePage()
        {
            this.InitializeComponent();

            RootSnippetPath = FileSystemUtils.GetRootSnippetFolderPath();
        }

        protected async override void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
        {
            try
            {
                // Setup the required root and current folders
                SetFolderDefaults();

                // Hook into visual state changes so we can reflect changes to our user control
                ApplicationViewStates.CurrentStateChanged += ApplicationViewStatesOnCurrentStateChanged;
                
                // Do we need to install some samples (is the library empty)?
                var folders = await FileSystemUtils.GetAllFoldersAsync(RootSnippetPath);
                if (folders == null || folders.Count == 0)
                {
                    await InstallSampleSnippets();
                }

                if (navigationParameter != null)
                {
                    // Page was opened to do a search
                    BtnClearSearch.Visibility = Visibility.Visible;

                    var searchItem = navigationParameter.ToString();

                    bool searchFromCurrentFolder = true;
                    if (ApplicationData.Current.RoamingSettings.Values.ContainsKey("SearchFromCurrentFolder"))
                        searchFromCurrentFolder = (bool)ApplicationData.Current.RoamingSettings.Values["SearchFromCurrentFolder"];

                    FileSystemUserControl.DoSnippetQuery(searchFromCurrentFolder ? CurrentSnippetPath : RootSnippetPath, searchItem);
                }
                else
                {
                    BtnClearSearch.Visibility = Visibility.Collapsed;

                    if (!string.IsNullOrEmpty(CurrentSnippetPath)) FileSystemUserControl.CurrentSnippetFolder = CurrentSnippetPath;
                    FileSystemUserControl.QueryResults = string.Empty;
                }
            }
            catch
            {
            }

            // Hook into our user control's SnippetSelected event so we can navigate to a new page to display info on the snippet
            FileSystemUserControl.SnippetSelected += (sender, args) =>
            {
                try
                {
                    if (!(args is SnippetSelectedEventArgs)) return;

                    var selectedSnippetArgs = args as SnippetSelectedEventArgs;
                    if (selectedSnippetArgs == null) return;

                    var selectedSnippet = selectedSnippetArgs.Snippet;
                    Frame.Navigate(typeof (SnippetPage), selectedSnippet.Path);
                }
                catch
                {
                }
            };

            // Hook into the user control's FolderChanged event 
            FileSystemUserControl.FolderChanged += (sender, args) =>
            {
                if (args == null) return;

                var folderChangedArgs = args as FolderChangedEventArgs;
                if (folderChangedArgs == null || string.IsNullOrEmpty(folderChangedArgs.SelectedPath)) return;

                CurrentSnippetPath = folderChangedArgs.SelectedPath;
            };
        }

        private void ApplicationViewStatesOnCurrentStateChanged(object sender, VisualStateChangedEventArgs visualStateChangedEventArgs)
        {
            // Tell the custom control to change appearance for the new visual state
            FileSystemUserControl.SetVisualState(visualStateChangedEventArgs.NewState.Name);
        }

        protected override void SaveState(Dictionary<String, Object> pageState)
        {
        }

        private async void SetFolderDefaults()
        {
            // Set default values for the root and current snippet folders
            try
            {
                var root = await FileSystemUtils.EnsureRootFolderExistsAsync();
                if (root != null && !string.IsNullOrEmpty(root.Path))
                {
                    RootSnippetPath = root.Path;

                    // See if we stored a value for the current folder...
                    if (ApplicationData.Current.RoamingSettings.Values.ContainsKey("CurrentSnippetPath"))
                        CurrentSnippetPath = ApplicationData.Current.RoamingSettings.Values["CurrentSnippetPath"].ToString();

                    // ...we did. Make sure it still exists...
                    if (!await FileSystemUtils.FolderExistsAsync(CurrentSnippetPath))
                        CurrentSnippetPath = RootSnippetPath;  // ...it doesn't. Set a default value

                    return;
                }
            }
            catch
            {
            }

            // We couldn't locate or create a sensible default root folder...
            var dlg = new MessageDialog("Unable to locate or create a folder to contain your snippet library. This app will now quit.");
            await dlg.ShowAsync();
            App.Current.Exit();
        }

        private void BtnNewSnippetTapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            CloseAppBar();

            try
            {
                Frame.Navigate(typeof(SnippetPage), null);  // null indicates a new snippet will be created
            }
            catch
            {
            }
        }

        private bool EnsureUnsnapped()
        {
            // FilePicker APIs will not work if the application is in a snapped state.
            // If an app wants to show a FilePicker while snapped, it must attempt to unsnap first
            bool unsnapped = true;
            if (ApplicationView.Value == ApplicationViewState.Snapped) unsnapped = ApplicationView.TryUnsnap();

            return unsnapped;
        }

        private void BtnHelpTapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            CloseAppBar();

            try
            {
                Frame.Navigate(typeof (HelpPage));
            }
            catch
            {
            }
        }

        private async Task<bool> InstallSampleSnippets()
        {
            // Install some sample snippets
            MessageDialog dlg;

            try
            {
                if (await FileSystemUtils.FolderExistsAsync(RootSnippetPath + "\\Samples"))
                    return true;  // Samples have been previously installed

                var folder = await FileSystemUtils.CreateFolderAsync(RootSnippetPath, "Samples");
                if (folder == null) throw new Exception();

                var repository = new SnippetRepository();

                // /////////////
                // Snippet #1...
                var sampleSnippet = await CodeSnippet.CreateNew();
                sampleSnippet.Filename = "ReadTextFile.snippet";
                sampleSnippet.Title = "Read a text file";
                sampleSnippet.Description = "Read a text file from the Documents library";
                sampleSnippet.Author = "Russell Archer";
                sampleSnippet.Code = "// Get a file in the Documents library by specifying a relative path\r\nvar file = await KnownFolders.DocumentsLibrary.GetFileAsync($relativePath$\\$filename$);\r\n\r\n// Read the contents of the as one long string\r\nvar txt = await FileIO.ReadTextAsync(file);\r\n\r\n// Display the text\r\ntbTextBlock.Text = txt;\r\n";
                sampleSnippet.DeclarationList = new ObservableCollection<Declaration>();
                sampleSnippet.DeclarationList.Add(new Declaration
                {
                    DeclarationType = DeclarationTypes.Literal,
                    Editable = true,
                    ID = "relativePath",
                    ToolTip = "Relative path",
                    Default = "relativePath"
                });

                sampleSnippet.DeclarationList.Add(new Declaration
                {
                    DeclarationType = DeclarationTypes.Literal,
                    Editable = true,
                    ID = "filename",
                    ToolTip = "Filename",
                    Default = "filename"
                });

                if (!await repository.WriteSnippetAsync(folder, sampleSnippet)) throw new Exception();

                // /////////////
                // Snippet #2...
                sampleSnippet = await CodeSnippet.CreateNew();
                sampleSnippet.Filename = "ReadJsonFile.snippet";
                sampleSnippet.Title = "Read a JSON file";
                sampleSnippet.Description = "Read a static text file containing JSON from a subdirectory in the app's location";
                sampleSnippet.Author = "Russell Archer";
                sampleSnippet.Code = "var file = await Package.Current.InstalledLocation.GetFileAsync($appFolder$\\$filename$);\r\nvar txt = await FileIO.ReadTextAsync(file);\r\n\r\n// Here we use Newtonsoft Json.net to deserialize the json\r\nvar results = await JsonConvert.DeserializeObjectAsync<ObservableCollection<t>>(txt);";
                sampleSnippet.DeclarationList = new ObservableCollection<Declaration>();
                sampleSnippet.DeclarationList.Add(new Declaration
                {
                    DeclarationType = DeclarationTypes.Literal,
                    Editable = true,
                    ID = "appFolder",
                    ToolTip = "App Folder",
                    Default = "appFolder"
                });

                sampleSnippet.DeclarationList.Add(new Declaration
                {
                    DeclarationType = DeclarationTypes.Literal,
                    Editable = true,
                    ID = "filename",
                    ToolTip = "Filename",
                    Default = "filename"
                });

                if (!await repository.WriteSnippetAsync(folder, sampleSnippet)) throw new Exception();

                // /////////////
                // Snippet #3...
                sampleSnippet = await CodeSnippet.CreateNew();
                sampleSnippet.Filename = "ReadTextFileOpenPicker.snippet";
                sampleSnippet.Title = "Read text file selected by user";
                sampleSnippet.Description = "Read a text file selected by the user via the FileOpenPicker";
                sampleSnippet.Author = "Russell Archer";
                sampleSnippet.Code = "// Create the file open picker\r\nvar picker = new FileOpenPicker();\r\n\r\n// Set the type of file to pick\r\npicker.FileTypeFilter.Add(\".txt\");\r\n\r\n// Single-file selection\r\nvar file = await picker.PickSingleFileAsync();\r\n\r\n// If the user chose something, read it into a string var\r\nif (file != null)\r\n    var myText = await FileIO.ReadTextAsync(file);\r\n";

                if (!await repository.WriteSnippetAsync(folder, sampleSnippet)) throw new Exception();

                // End of sample snippet creation

                dlg = new MessageDialog("A small selection of sample code snippets has been created in the \"Samples\" folder of your snippet library. \n\nYou may also wish to use the \"Import\" command to import existing Visual Studio code snippets from your Documents library. If you modify snippets you can use the \"Export\" command. \n\nAlternatively, add your snippet library path to Visual Studio using Tools > Code Snippets Manager. This will give Visual Studio direct access to your snippet library. The path to the snippet library may be copied from this app's Preferences page.");
                await dlg.ShowAsync();

                CurrentSnippetPath = folder.Path;  // Jump to the samples folder
                FileSystemUserControl.CurrentSnippetFolder = CurrentSnippetPath;

                return true;
            }
            catch
            {
            }

            dlg = new MessageDialog("Unable to create sample snippets");
            await dlg.ShowAsync();
            return false;
        }

        private async void BtnExportToDocLibTapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            CloseAppBar();

            var fromFolder = await FileSystemUtils.PromptForFolder("Select folder to export to");
            if (fromFolder == null) return;

            var repository = new SnippetRepository();
            repository.LengthyOpStarting += (x, args) => { ProgressRingSnippets.IsActive = true; };
            repository.LengthyOpHasEnded += (x, args) => { ProgressRingSnippets.IsActive = false; };

            var nFilesExported = await repository.ExportAllSnippetsToDocumentsLibrary(fromFolder);

            var msg = nFilesExported == -1 ?
                "An error occurred during snippet export. The process did not complete successfully" :
                nFilesExported.ToString() + " snippets exported from the local snippet data store";
            var dlg = new MessageDialog(msg);
            await dlg.ShowAsync();
        }

        private async void BtnImportFromDocLibTapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            CloseAppBar();

            var fromFolder = await FileSystemUtils.PromptForFolder("Select folder to import from");
            if (fromFolder == null) return;

            var repository = new SnippetRepository();
            repository.LengthyOpStarting += (x, args) => { ProgressRingSnippets.IsActive = true; };
            repository.LengthyOpHasEnded += (x, args) => { ProgressRingSnippets.IsActive = false; };

            var nFilesImported = await repository.ImportAllSnippetsFromDocumentsLibrary(fromFolder);

            var msg = nFilesImported == -1 ?
                "An error occurred during snippet import. The process did not complete successfully" :
                nFilesImported.ToString() + " snippets imported into the local snippet data store";
            var dlg = new MessageDialog(msg);
            await dlg.ShowAsync();

            // Goto the root of the library and refresh
            FileSystemUserControl.GotoRootFolder();
        }

        private void CloseAppBar()
        {
            var bottomAppBar = this.BottomAppBar;
            if (bottomAppBar != null) bottomAppBar.IsOpen = false;
        }

        private void BtnNewFolderTapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            // Create a new folder in the CurrentFolder folder

            var flyout = new Flyout();
            var content = new NewFolderFlyout();

            flyout.Content = content;
            flyout.Placement = PlacementMode.Top;
            flyout.PlacementTarget = sender as Button;

            content.CloseAndCreate += async (o, args) =>
            {
                flyout.IsOpen = false;

                var folderName = content.NewFolderName;
                if (string.IsNullOrEmpty(folderName)) return;

                var folder = await FileSystemUtils.CreateFolderAsync(CurrentSnippetPath, folderName);
                if (folder != null)
                {
                    FileSystemUserControl.CurrentSnippetFolder = CurrentSnippetPath;  // Force a refresh
                }
                else
                {
                    var dlg = new MessageDialog("Unable to create folder \"" + folderName + "\"");
                    await dlg.ShowAsync();
                }

                CloseAppBar();
            };

            flyout.IsOpen = true;
        }

        private async void BtnDelFolderTapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            CloseAppBar();

            var tmpCurrentSnippetPath = CurrentSnippetPath;
            if (tmpCurrentSnippetPath.EndsWith("\\")) tmpCurrentSnippetPath = tmpCurrentSnippetPath.TrimEnd('\\');

            if (String.Compare(tmpCurrentSnippetPath, App.RootSnippetPath, StringComparison.Ordinal) == 0)
            {
                var notAllowedDlg = new MessageDialog("You cannot delete the root of the snippet library");
                await notAllowedDlg.ShowAsync();
                return;
            }

            var msg = "Delete the folder \"" +
                      FileSystemUtils.GetLocalDataStoreRelativeSnippetRootPath(CurrentSnippetPath) +
                      "\" (and all files and folders contained in it) - are you sure?";

            var dlg = new MessageDialog(msg);
            dlg.Commands.Add(new UICommand("Yes"));
            dlg.Commands.Add(new UICommand("No"));
            var answer = await dlg.ShowAsync();

            if (String.Compare(answer.Label, "No", StringComparison.Ordinal) == 0) return;

            bool folderRemoved;
            try
            {
                var folderToDelete = await StorageFolder.GetFolderFromPathAsync(CurrentSnippetPath);
                
                await folderToDelete.DeleteAsync(StorageDeleteOption.Default);  // Delete to the recycle bin
                FileSystemUserControl.GotoParentFolder();  // Go up one level
                folderRemoved = true;
            }
            catch
            {
                folderRemoved = false;
            }

            if (folderRemoved) return;

            dlg = new MessageDialog("Unable to remove folder");
            await dlg.ShowAsync();
        }

        private void BtnClearSearchTapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(CurrentSnippetPath)) FileSystemUserControl.CurrentSnippetFolder = App.RootSnippetPath;
            FileSystemUserControl.QueryResults = string.Empty;
            CloseAppBar();
            BtnClearSearch.Visibility = Visibility.Collapsed;
        }
    }
}
