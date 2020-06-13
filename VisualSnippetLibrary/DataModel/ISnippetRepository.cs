using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.Storage;

namespace VisualSnippetLibrary.DataModel
{
    interface ISnippetRepository
    {
        event EventHandler LengthyOpStarting;
        event EventHandler LengthyOpHasEnded;

        Task<ObservableCollection<CodeSnippet>> GetAllSnippetsAsync(string path, string query, bool raiseLengthOpEvents = true);
        Task<ObservableCollection<CodeSnippet>> GetAllSnippetsAsync(StorageFolder folder, string query, bool raiseLengthOpEvents = true);

        Task<ObservableCollection<CodeSnippet>> GetAllSnippetsRecursivelyAsync(string path, string query);
        Task<ObservableCollection<CodeSnippet>> GetAllSnippetsRecursivelyAsync(StorageFolder folder, string query);

        Task<CodeSnippet> GetSnippetAsync(string path);
        Task<CodeSnippet> GetSnippetAsync(StorageFile file);

        Task<bool> WriteSnippetAsync(StorageFile file, CodeSnippet snippet);
        Task<bool> WriteSnippetAsync(string path, CodeSnippet snippet);
        Task<string> WriteSnippetToString(CodeSnippet snippet);

        Task<int> ImportAllSnippetsFromDocumentsLibrary(StorageFolder fromFolder);
        Task<int> ExportAllSnippetsToDocumentsLibrary(StorageFolder toFolder);
    }
}
