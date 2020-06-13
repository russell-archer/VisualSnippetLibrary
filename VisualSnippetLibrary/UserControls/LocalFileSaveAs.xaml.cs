using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using VisualSnippetLibrary.DataModel;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace VisualSnippetLibrary.UserControls
{
    public sealed partial class LocalFileSaveAs : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler PickerClosed;

        private string _currentFolder;
        public string CurrentFolder
        {
            get { return _currentFolder; }
            set 
            { 
                _currentFolder = value;
                CurrentFolderAlias = FileSystemUtils.GetLocalDataStoreRelativeSnippetRootPath(value);
                CanGoUp = String.Compare(CurrentFolderAlias, App.RootSnippetFolderAlias, StringComparison.Ordinal) == 0 ? Visibility.Collapsed : Visibility.Visible;
                OnPropertyChanged();
                OnPropertyChanged("CurrentFolderAlias");
                OnPropertyChanged("CanGoUp");
            }
        }

        public string CurrentFolderAlias { get; set; }
        public Visibility CanGoUp { get; set; }

        private CodeSnippet _snippet;
        public CodeSnippet Snippet
        {
            get { return _snippet; }
            set { _snippet = value; OnPropertyChanged();}
        }

        private ObservableCollection<FolderPickerItem> _folders;
        public ObservableCollection<FolderPickerItem> Folders 
        {
            get { return _folders; }
            set { _folders = value; OnPropertyChanged(); }
        }

        public LocalFileSaveAs()
        {
            this.InitializeComponent();
            Init();
        }

        private async void Init()
        {
            CurrentFolder = App.RootSnippetPath;
            Folders = await FileSystemUtils.GetAllFolderPickerFoldersAndFilesAsync(CurrentFolder);

            GoUp.PointerEntered += (sender, args) => GoUp.FontWeight = FontWeights.Normal;
            GoUp.PointerExited += (sender, args) => GoUp.FontWeight = FontWeights.Thin;
            GoUp.Tapped += async (sender, args) =>
            {
                var tmpPath = FileSystemUtils.GetLocalDataStoreParentFolder(CurrentFolder);
                if (tmpPath == null) return;

                CurrentFolder = tmpPath;
                Folders = await FileSystemUtils.GetAllFolderPickerFoldersAndFilesAsync(CurrentFolder);
            };
        }

        private void SaveTapped(object sender, TappedRoutedEventArgs e)
        {
            if (!Snippet.Filename.EndsWith(".snippet")) Snippet.Filename += ".snippet";
            Snippet.Path = CurrentFolder;
            OnPickerClosed(new LocalFilePickerClosedEventArgs { OperationCancelled = false, Filename = Snippet.Filename, Path = CurrentFolder });
        }

        private void CancelTapped(object sender, TappedRoutedEventArgs e)
        {
            OnPickerClosed(new LocalFilePickerClosedEventArgs { OperationCancelled = true, Filename = null, Path = null });
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OnPickerClosed(LocalFilePickerClosedEventArgs e)
        {
            EventHandler handler = PickerClosed;
            if (handler != null) handler(this, e);
        }

        private async void FolderTapped(object sender, TappedRoutedEventArgs e)
        {
            if (GridViewFolders.SelectedIndex == -1) return;

            var selectedFolder = GridViewFolders.SelectedItem as FolderPickerItem;
            if (selectedFolder == null) return;

            // Is it a folder or a file selected? 
            // If it's a folder, we drill into it, if it's a file we set the snippet's filename property to the selected filename
            if (!selectedFolder.IsFolder)
            {
                Snippet.Filename = selectedFolder.Name;
                return;
            }

            CurrentFolder = selectedFolder.Path;
            Folders = await FileSystemUtils.GetAllFolderPickerFoldersAndFilesAsync(CurrentFolder);
        }
    }

    public class LocalFilePickerClosedEventArgs : EventArgs
    {
        public bool OperationCancelled { get; set; }
        public string Filename { get; set; }
        public string Path { get; set; }
    }

    public sealed class LengthToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return ((int) value) > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value is Visibility && (Visibility)value == Visibility.Visible;
        }
    }

    public sealed class FilePickerItemTypeToFontStyleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return ((bool) value) ? FontStyle.Normal : FontStyle.Italic;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
