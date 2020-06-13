using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using VisualSnippetLibrary.DataModel;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace VisualSnippetLibrary.UserControls
{
    public sealed partial class SnippetFinderUserControl : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler SnippetSelected;
        public event EventHandler FolderChanged;

        // The snippet repository manages access to/parsing of code snippets
        private ISnippetRepository _snippetRepository;

        private string _currentSnippetFolder = "";
        public string CurrentSnippetFolder 
        {
            get { return _currentSnippetFolder; }
            set 
            { 
                _currentSnippetFolder = value;
                CurrentRelativeSnippetFolder = FileSystemUtils.GetLocalDataStoreRelativeSnippetRootPath(value); 
                OnPropertyChanged(); 
                UpdateViews(); 
            }
        }

        // Property is bound to TextBlockCurrentPath control
        private string _currentRelativeSnippetFolder = "";
        public string CurrentRelativeSnippetFolder
        {
            get { return _currentRelativeSnippetFolder; }
            set { _currentRelativeSnippetFolder = value; OnPropertyChanged(); }
        }

        // Property is bound to TextBlockQueryResults control
        private string _queryResults = "";
        public string QueryResults
        {
            get { return _queryResults; }
            set { _queryResults = value; OnPropertyChanged(); }
        }

        // Property is bound to TextBlockCurrentPath control
        private ObservableCollection<CodeSnippet> _snippetFiles;
        public ObservableCollection<CodeSnippet> SnippetFiles
        {
            get { return _snippetFiles; }
            set { _snippetFiles = value; OnPropertyChanged(); }
        }

        // Property is bound to GridViewSnippets control
        private ObservableCollection<FolderSummary> _folders { get; set; }
        public ObservableCollection<FolderSummary> Folders
        {
            get { return _folders; }
            set { _folders = value; OnPropertyChanged(); }
        }

        public SnippetFinderUserControl()
        {
            this.InitializeComponent();
            Init();
        }

        private void Init()
        {
            _snippetRepository = new SnippetRepository();
            _snippetRepository.LengthyOpStarting += (sender, args) => { ProgressRingSnippets.IsActive = true; };
            _snippetRepository.LengthyOpHasEnded += (sender, args) => { ProgressRingSnippets.IsActive = false; };
        }

        private async Task GetFoldersAsync()
        {
            if (string.IsNullOrEmpty(CurrentSnippetFolder))
                return;

            Folders = await FileSystemUtils.GetAllFoldersAsync(CurrentSnippetFolder);
        }

        private async Task GetSnippetFilenamesAsync()
        {
            if (string.IsNullOrEmpty(CurrentSnippetFolder))
                return;

            SnippetFiles = await _snippetRepository.GetAllSnippetsAsync(CurrentSnippetFolder, null);
        }

        private async void UpdateViews()
        {
            await GetFoldersAsync();
            await GetSnippetFilenamesAsync();
        }

        private async void ShowMessage(string msg)
        {
            var dlg = new MessageDialog(msg);
            await dlg.ShowAsync();
        }

        private void FolderTapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(QueryResults))
                return;

            // Was it the up folder button? If the user clicks on the list view item containing 
            // the button, but not ON the button, the correct item is selected. If nothing is 
            // selected it means the actual up button was clicked on
            if (ListViewFolders.SelectedIndex == -1)
                ListViewFolders.SelectedIndex = 0;

            try
            {
                var selectedFolder = ListViewFolders.SelectedItem as FolderSummary;
                if (selectedFolder == null) 
                    return;

                CurrentSnippetFolder = selectedFolder.Path;
                UpdateViews();

                // Fire an event to tell our host the folder has changed
                var folderChangedEventArgs = new FolderChangedEventArgs {SelectedPath = CurrentSnippetFolder};
                OnFolderChanged(folderChangedEventArgs);
            }
            catch
            {
                ShowMessage("Unable to change to folder:\n\n" + CurrentSnippetFolder);
            }            
        }

        private void SnippetTapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            if (GridViewSnippets.SelectedIndex == -1)
                return;

            var selectedSnippet = GridViewSnippets.SelectedItem as CodeSnippet;
            if (selectedSnippet == null)
                return;

            // Fire the event which allows the host page to use the selected snippet
            var snippetEventArgs = new SnippetSelectedEventArgs {Snippet = selectedSnippet};
            OnSnippetSelected(snippetEventArgs);
        }

        public void SetVisualState(string newState)
        {
            switch (newState)
            {
                case "FullScreenLandscape":
                case "Filled":
                case "FullScreenPortrait":
                    var defaultDataTemplate = App.Current.Resources["ListViewFoldersTemplate"] as DataTemplate;
                    ListViewFolders.ItemTemplate = defaultDataTemplate;
                    GridViewSnippets.Visibility = Visibility.Visible;
                    TextBlockPath.Visibility = Visibility.Visible;
                    Col0.Width = new GridLength(0.3, GridUnitType.Star);
                    Col1.Width = new GridLength(1, GridUnitType.Star);
                    break;

                case "Snapped":
                    var snappedDataTemplate = App.Current.Resources["ListViewFoldersTemplateSnapped"] as DataTemplate;
                    ListViewFolders.ItemTemplate = snappedDataTemplate;
                    GridViewSnippets.Visibility = Visibility.Collapsed;
                    TextBlockPath.Visibility = Visibility.Collapsed;
                    Col0.Width = new GridLength(1, GridUnitType.Star);
                    Col1.Width = new GridLength(0, GridUnitType.Star);
                    break;
            }
        }

        public async void DoSnippetQuery(string path, string query)
        {
            var folder = await StorageFolder.GetFolderFromPathAsync(path);
            await DoSnippetQuery(folder, query);
        }

        public async Task DoSnippetQuery(StorageFolder folder, string query)
        {
            SnippetFiles = await _snippetRepository.GetAllSnippetsRecursivelyAsync(folder, query);

            if (SnippetFiles.Count == 0)
                QueryResults = "No snippets found matching the search text";
            else if (SnippetFiles.Count == 1)
                QueryResults = "1 snippet found matching the search text";
            else
                QueryResults = SnippetFiles.Count.ToString() + " snippets found matching the search text";
        }

        public void GotoParentFolder()
        {
            FolderTapped(this, null);
        }

        public void GotoRootFolder()
        {
            CurrentSnippetFolder = App.RootSnippetPath;
        }

        public void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public void OnSnippetSelected(SnippetSelectedEventArgs e)
        {
            if (SnippetSelected != null)
                SnippetSelected(this, e);
        }

        public void OnFolderChanged(FolderChangedEventArgs e)
        {
            if (FolderChanged != null)
                FolderChanged(this, e);
        }
    }

    public class SnippetSelectedEventArgs : EventArgs
    {
        public CodeSnippet Snippet { get; set; }
    }

    public class FolderChangedEventArgs : EventArgs
    {
        public string SelectedPath { get; set; }
    }
}
