using System;
using System.Collections.ObjectModel;

namespace VisualSnippetLibrary.DataModel
{
    public class DesignTimeSnippetRepository
    {
        // Property is bound to gridViewSnippets control
        private ObservableCollection<FolderSummary> _folders;
        public ObservableCollection<FolderSummary> Folders
        {
            get { return GetAllFolders(); }
        }

        private ObservableCollection<CodeSnippet> _snippetFiles;
        public ObservableCollection<CodeSnippet> SnippetFiles
        {
            get { return GetAllSnippets(); }
        }

        public ObservableCollection<FolderSummary> GetAllFolders()
        {
            if (_folders != null)
                return _folders;

            _folders = new ObservableCollection<FolderSummary>();
            _folders.Add(new FolderSummary { FileCount = 1, Path = @"C:\Users\Russell\Documents\Visual Studio 2012\Code Snippets"});
            _folders.Add(new FolderSummary { FileCount = 5, Path = @"C:\Users\Russell\Documents\Visual Studio 2012\TestDir" });
            _folders.Add(new FolderSummary { FileCount = 12, Path = @"C:\Users\Russell\Documents\Visual Studio 2012\TestDir2" });

            return _folders;
        }

        public ObservableCollection<CodeSnippet> GetAllSnippets()
        {
            if (_snippetFiles != null)
                return _snippetFiles;

            _snippetFiles = new ObservableCollection<CodeSnippet>();
            _snippetFiles.Add(new CodeSnippet { Author = "RPA", Path = @"C:\Users\Russell\Documents\Visual Studio 2012\Code Snippets\test1.xaml", Title = "Test1", Code = "Code 1 goes here"});
            _snippetFiles.Add(new CodeSnippet { Author = "RPA", Path = @"C:\Users\Russell\Documents\Visual Studio 2012\Code Snippets\test2.xaml", Title = "Test2", Code = "Code 2 goes here" });
            _snippetFiles.Add(new CodeSnippet { Author = "RPA", Path = @"C:\Users\Russell\Documents\Visual Studio 2012\Code Snippets\test3.xaml", Title = "Test3", Code = "Code 3 goes here" });
            _snippetFiles.Add(new CodeSnippet { Author = "RPA", Path = @"C:\Users\Russell\Documents\Visual Studio 2012\Code Snippets\test4.xaml", Title = "Test4", Code = "Code 4 goes here" });
            _snippetFiles.Add(new CodeSnippet { Author = "RPA", Path = @"C:\Users\Russell\Documents\Visual Studio 2012\Code Snippets\test5.xaml", Title = "Test5", Code = "Code 5 goes here" });

            return _snippetFiles;
        }

        public CodeSnippet GetSnippet(string path)
        {
            throw new NotImplementedException();
        }

    }
}