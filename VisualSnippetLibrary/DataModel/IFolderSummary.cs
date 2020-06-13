using Windows.UI.Xaml;

namespace VisualSnippetLibrary.DataModel
{
    interface IFolderSummary
    {
        string Path { get; set; }
        int FileCount { get; set; }
        string TopFolderName { get; }
        string FileCountFormat { get; }
        Visibility UpFolderButtonVisibility { get; }
        Visibility FolderInfoVisibility { get; }
    }
}
