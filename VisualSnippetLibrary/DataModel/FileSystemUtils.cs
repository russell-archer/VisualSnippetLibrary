using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace VisualSnippetLibrary.DataModel
{
    public class FileSystemUtils
    {
        /// <summary>
        /// Gets the path to the root of the local data store snippet library
        /// </summary>
        /// <returns>Returns the path to the root of the local data store snippet library</returns>
        public static string GetRootSnippetFolderPath()
        {
            return ApplicationData.Current.LocalFolder.Path + "\\" + App.RootSnippetFolderName;
        }

        /// <summary>
        /// Gets the path relative to the local data store snippet root folder. For example, if the specified path is:
        /// "C:\Users\Russell\AppData\Local\Packages\VisualSnippetLibrary_1dm842cv64fym\LocalState\Visual Snippet Library\WinRT\Microsoft"
        /// the relative path returned would be "Snippet Library > WinRT > Microsoft"
        /// </summary>
        /// <param name="path">An absolute path in the local data store</param>
        /// <returns>A releative path starting at the local data store snippet library root. The return value does NOT represent a valid file system path</returns>
        public static string GetLocalDataStoreRelativeSnippetRootPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            try
            {
                // Find the location of the root snippet library folder
                var i = path.IndexOf(App.RootSnippetPath, StringComparison.Ordinal);
                if (i == -1) return null;

                // Strip off the path to the root snippet folder
                var relativePath = path.Substring(App.RootSnippetPath.Length);
                if (relativePath.StartsWith("\\")) relativePath = relativePath.TrimStart('\\');
                if (relativePath.EndsWith("\\")) relativePath = relativePath.TrimEnd('\\');

                // Replace any '\' chars with '>'
                relativePath = relativePath.Replace("\\", " > ");

                return string.IsNullOrEmpty(relativePath) ? App.RootSnippetFolderAlias : App.RootSnippetFolderAlias + " > " + relativePath;
            }
            catch
            {
            }

            return null;
        }

        /// <summary>
        /// Gets the parent of the specified folder
        /// </summary>
        /// <param name="path">The folder for which the parent is required</param>
        /// <returns>Returns the parent of the specified folder</returns>
        public static string GetLocalDataStoreParentFolder(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            try
            {
                if (path.EndsWith("\\")) path = path.TrimEnd('\\');
                var i = path.LastIndexOf("\\", StringComparison.Ordinal);
                if (i == -1) return null;

                path = path.Remove(i);
                if (path.EndsWith("\\")) path = path.TrimEnd('\\');

                return path;
            }
            catch
            {
            }

            return null;
        }

        /// <summary>
        /// Gets the StoragFolder object for the root of the local data store snippet library
        /// </summary>
        /// <returns>Returns the StoragFolder object for the root of the local data store snippet library</returns>
        public static async Task<StorageFolder> GetRootSnippetStorageFolderAsync()
        {
            return await StorageFolder.GetFolderFromPathAsync(GetRootSnippetFolderPath());
        }

        /// <summary>
        /// Ensures that all elements in the specified path exist. Folders are created as required if 
        /// they don't exist. Note that the app must have rw access to all elements of the path
        /// </summary>
        /// <param name="path">Returns a valid StorageFolder for the given path, or null if an error occurrs</param>
        /// <returns>Returns a valid StorageFolder object for the path, or null if an error occurrs (or we don't have access permissions)</returns>
        public static async Task<StorageFolder> EnsureAllLocalDataPathElementsExistAsync(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            var pathElements = path.Split('\\');
            if (pathElements == null || pathElements.Length == 0) return null;

            // Is the path for our local data store?
            if (!path.ToLower().Contains(ApplicationData.Current.LocalFolder.Path.ToLower())) return null;

            var tmpPath = new StringBuilder(pathElements[0]);  // This will be the drive
            try
            {
                // Check each part of the path. We skip over checking parts of the path
                // prior to "LocalState", because we don't have access permissions
                bool foundLocalStateFolder = false;
                for (var i = 1; i < pathElements.Length; i++)
                {
                    var element = pathElements[i];
                    if (!foundLocalStateFolder)
                    {
                        if (String.Compare(element.ToLower(), "localstate", StringComparison.Ordinal) == 0)
                            foundLocalStateFolder = true;
                        else
                        {
                            tmpPath.Append("\\" + element); // Continue building the path without checking elements to whcih we don't have access
                            continue;
                        }
                    }

                    var pathToCheck = tmpPath.ToString() + "\\" + element;
                    var pathExists = await FolderExistsAsync(pathToCheck);
                    if (!pathExists)
                        if(await CreateFolderAsync(tmpPath.ToString(), element) == null) return null;

                    tmpPath.Append("\\" + element);  // Continue building the path
                }

                return await StorageFolder.GetFolderFromPathAsync(path);
            }
            catch
            {
            }

            return null;
        }

        /// <summary>
        /// Ensures that all elements in the specified path exist. Folders are created as required if 
        /// they don't exist. Note that the app must have rw access to all elements of the path
        /// </summary>
        /// <param name="path">Returns a valid StorageFolder for the given path, or null if an error occurrs</param>
        /// <returns>Returns a valid StorageFolder object for the path, or null if an error occurrs (or we don't have access permissions)</returns>
        public static async Task<StorageFolder> EnsureAllDataPathElementsExistAsync(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            var pathElements = path.Split('\\');
            if (pathElements == null || pathElements.Length == 0) return null;

            // Is the path for our local data store?
            if (!path.ToLower().Contains(ApplicationData.Current.LocalFolder.Path.ToLower())) return null;

            var tmpPath = new StringBuilder(pathElements[0]);  // This will be the drive
            try
            {
                // Check each part of the path. We skip over checking parts of the path
                // prior to "LocalState", because we don't have access permissions
                bool foundLocalStateFolder = false;
                for (var i = 1; i < pathElements.Length; i++)
                {
                    var element = pathElements[i];
                    if (!foundLocalStateFolder)
                    {
                        if (String.Compare(element.ToLower(), "localstate", StringComparison.Ordinal) == 0)
                            foundLocalStateFolder = true;
                        else
                        {
                            tmpPath.Append("\\" + element); // Continue building the path without checking elements to whcih we don't have access
                            continue;
                        }
                    }

                    var pathToCheck = tmpPath.ToString() + "\\" + element;
                    var pathExists = await FolderExistsAsync(pathToCheck);
                    if (!pathExists)
                        if (await CreateFolderAsync(tmpPath.ToString(), element) == null) return null;

                    tmpPath.Append("\\" + element);  // Continue building the path
                }

                return await StorageFolder.GetFolderFromPathAsync(path);
            }
            catch
            {
            }

            return null;
        }

        /// <summary>
        /// Because we can't access the main Visual Studio code snippets folders, the root folder for all snippets managed by VSL
        /// will be: "C:\Users\{username}\AppData\Local\Packages\{package name}\LocalState\Visual Snippet Library".
        /// If this folder doesn't exist, which normally indicates the app's not been run before, we create the folder
        /// </summary>
        /// <returns>Returns a valid StorageFolder for the path to the local data store snippet library</returns>
        public static async Task<StorageFolder> EnsureRootFolderExistsAsync()
        {
            try
            {
                var root = GetRootSnippetFolderPath();
                if(await FolderExistsAsync(root)) return await StorageFolder.GetFolderFromPathAsync(root);  // The snippet library already exists

                // Create the Snippets root folder
                var folder = await CreateFolderAsync(ApplicationData.Current.LocalFolder.Path, App.RootSnippetFolderName);
                if (folder != null && !string.IsNullOrEmpty(folder.Path)) return folder;
            }
            catch
            {
            }

            return null;
        }

        /// <summary>
        /// Creates a new sub-folder in the specified path (which must exist or null is returned)
        /// </summary>
        /// <param name="path">The path to the folder in which a new sub-folder will be created</param>
        /// <param name="folderName">The name of the new sub-folder</param>
        /// <returns>Retuns a valid StorageFolder object for the path to the new sub-folder, or null if an error occurrs</returns>
        public static async Task<StorageFolder> CreateFolderAsync(string path, string folderName)
        {
            try
            {
                var folder = await StorageFolder.GetFolderFromPathAsync(path);
                return await folder.CreateFolderAsync(folderName);
            }
            catch
            {
            }

            return null;
        }

        /// <summary>
        /// Creates a new sub-folder in the specified path (which must exist or null is returned)
        /// </summary>
        /// <param name="path">A StorageFolder object for the path to the folder in which a new sub-folder will be created</param>
        /// <param name="folderName">The name of the new sub-folder</param>
        /// <returns>Retuns a valid StorageFolder object for the path to the new sub-folder, or null if an error occurrs</returns>
        public static async Task<StorageFolder> CreateFolderAsync(StorageFolder path, string folderName)
        {
            try
            {
                return await path.CreateFolderAsync(folderName);
            }
            catch
            {
            }

            return null;
        }

        /// <summary>
        /// Checks to see if the path exists. Note that the app must have access permissions to the specified folder
        /// </summary>
        /// <param name="path">The path to check exists</param>
        /// <returns>Returns true if the path exists, false otherwise (which could indicate an access denied exception)</returns>
        public static async Task<bool> FolderExistsAsync(string path)
        {
            try
            {
                var folder = await StorageFolder.GetFolderFromPathAsync(path);
                if (folder != null && !string.IsNullOrEmpty(folder.Path)) return true;
            }
            catch
            {
            }
            return false;
        }

        /// <summary>
        /// Uses the Windows 8 folder picker to get a folder from the user
        /// </summary>
        /// <param name="promptText">Optional prompt text</param>
        /// <returns>Returns a StorageFolder object, or null if the user cancelled the picker</returns>
        public static async Task<StorageFolder> PromptForFolder(string promptText)
        {
            var picker = new FolderPicker();
            picker.CommitButtonText = string.IsNullOrEmpty(promptText) ? "Select folder" : promptText ;
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.ViewMode = PickerViewMode.List;
            picker.FileTypeFilter.Add(".snippet");

            return await picker.PickSingleFolderAsync();
        }

        /// <summary>
        /// Checks to see if the path exists and returns the StorageFolder object for the path if it does. 
        /// Note that the app must have access permissions to the specified folder
        /// </summary>
        /// <param name="path">The path to check exists</param>
        /// <returns>Returns the StorageFolder object for the path if it exists. Returns null if the path doesn't exist (which could indicate an access denied exception)</returns>
        public static async Task<StorageFolder> GetFolderAsync(string path)
        {
            try
            {
                var folder = await StorageFolder.GetFolderFromPathAsync(path);
                if (folder != null && !string.IsNullOrEmpty(folder.Path)) return folder;
            }
            catch
            {
            }
            return null;
        }

        /// <summary>
        /// Checks to see if the file exists. Note that the app must have access permissions to the specified path
        /// </summary>
        /// <param name="path">The path and filename to check exists</param>
        /// <returns>Returns true if the file exists, false otherwise (which could indicate an access denied exception)</returns>
        public static async Task<bool> FileExistsAsync(string path)
        {
            try
            {
                var file = await StorageFile.GetFileFromPathAsync(path);
                if (file != null && !string.IsNullOrEmpty(file.Path)) return true;
            }
            catch
            {
            }
            return false;
        }

        /// <summary>
        /// Checks to see if the file exists and returns the StorageFile object for the file if it does. 
        /// Note that the app must have access permissions to the specified path
        /// </summary>
        /// <param name="path">The path and filename to get</param>
        /// <returns>Returns the StorageFile object for the path, if it exists. Returns null if the file doesn't exist (which could indicate an access denied exception)</returns>
        public static async Task<StorageFile> GetFileAsync(string path)
        {
            try
            {
                var file = await StorageFile.GetFileFromPathAsync(path);
                if (file != null && !string.IsNullOrEmpty(file.Path)) return file;
            }
            catch
            {
            }
            return null;
        }

        /// <summary>
        /// Deletes the file associated with a code snippet
        /// </summary>
        /// <param name="snippet">The snippet to delete</param>
        /// <returns>Returns true if the snippet was deleted, false otherwise</returns>
        public static async Task<bool> DeleteFileAsync(CodeSnippet snippet)
        {
            try
            {
                if (snippet == null || string.IsNullOrEmpty(snippet.Path)) return false;

                var file = await StorageFile.GetFileFromPathAsync(snippet.Path);
                await file.DeleteAsync();
                return true;
            }
            catch
            {
            }

            return false;
        }

        /// <summary>
        /// Deletes the file associated with a code snippet
        /// </summary>
        /// <param name="path">The path and filename of the file to delete</param>
        /// <returns>Returns true if the file was deleted, false otherwise</returns>
        public static async Task<bool> DeleteFileAsync(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path)) return false;

                var file = await StorageFile.GetFileFromPathAsync(path);
                await file.DeleteAsync();
                return true;
            }
            catch
            {
            }

            return false;
        }

        /// <summary>
        /// Enumerates all the subfolders immediately below the folder indicated by the path
        /// </summary>
        /// <param name="path">The path in which to enumerate folders</param>
        /// <returns>A collection of FolderSumarry objects that describe each folder</returns>
        public static async Task<ObservableCollection<FolderSummary>> GetAllFoldersAsync(string path)
        {
            try
            {
                var folderSummary = new ObservableCollection<FolderSummary>();

                // Add the "up" item if we can go higher up the path
                var pathParts = path.Split('\\').ToList();
                if (string.IsNullOrEmpty(pathParts[pathParts.Count - 1]))
                    pathParts.RemoveAt(pathParts.Count - 1);

                if (pathParts.Count > 1 && !pathParts[pathParts.Count - 1].Equals(App.RootSnippetFolderName))
                {
                    var parentFolder = new StringBuilder();
                    for (var i = 0; i < pathParts.Count - 1; i++)
                    {
                        parentFolder.Append(pathParts[i]);
                        parentFolder.Append("\\");
                    }

                    folderSummary.Add(new FolderSummary { FileCount = -1, Path = parentFolder.ToString() });
                }

                // Get a StorageFolder for the specified path
                var folder = await StorageFolder.GetFolderFromPathAsync(path);

                // Get a list of all the folders immediately below the specified path
                var subfolders = await folder.GetFoldersAsync();

                // For each subfolder, count how many files are in it
                foreach (var subfolder in subfolders)
                {
                    // Get all the files in the subfolder 
                    var files = await subfolder.GetFilesAsync();
                    folderSummary.Add(files != null
                                          ? new FolderSummary { Path = subfolder.Path, FileCount = files.Count() }
                                          : new FolderSummary { Path = subfolder.Path, FileCount = 0 });
                }

                return folderSummary;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Enumerates all the subfolders immediately below the folder indicated by the path
        /// </summary>
        /// <param name="path">The path in which to enumerate folders</param>
        /// <returns>A collection of FolderPickerItem objects that describe each folder</returns>
        public static async Task<ObservableCollection<FolderPickerItem>> GetAllFolderPickerFoldersAndFilesAsync(string path)
        {
            try
            {
                var foldersAndFiles = new ObservableCollection<FolderPickerItem>();

                // Get a StorageFolder for the specified path
                var folder = await StorageFolder.GetFolderFromPathAsync(path);

                // Get a list of all the folders immediately below the specified path
                var subfolders = await folder.GetFoldersAsync();

                foreach (var subfolder in subfolders)
                {
                    foldersAndFiles.Add(new FolderPickerItem 
                    {
                        Name = subfolder.Name,
                        Created = subfolder.DateCreated.Date, 
                        Path = subfolder.Path,
                        IsFolder = true
                    });
                }

                // Get all the files in the specified folder
                var files = await folder.GetFilesAsync();
                foreach (var file in files)
                {
                    foldersAndFiles.Add(new FolderPickerItem
                    {
                        Name = file.Name,
                        Created = file.DateCreated.Date,
                        Path = file.Path,
                        IsFolder = false
                    });
                }

                return foldersAndFiles;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Prompts the user to select a file from the documents library.
        /// </summary>
        /// <returns>Returns a valid StorageFile for the file selected, or null if the user cancelled the picker</returns>
        public static async Task<StorageFile> SelectSnippetFromDocumentsLibraryAsync()
        {
            try
            {
                // Create the file open picker
                var picker = new FileOpenPicker();

                // Set the type of file to pick
                picker.FileTypeFilter.Add(".snippet");
                picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

                // Single-file selection
                return await picker.PickSingleFileAsync();
            }
            catch
            {
            }

            return null;
        }
    }
}
