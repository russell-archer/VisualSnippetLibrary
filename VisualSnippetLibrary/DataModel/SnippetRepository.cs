using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Data.Xml.Dom;
using Windows.Storage;
using Windows.Storage.Streams;

namespace VisualSnippetLibrary.DataModel
{
    public class SnippetRepository : ISnippetRepository
    {
        public event EventHandler LengthyOpStarting;
        public event EventHandler LengthyOpHasEnded;

        protected void OnLengthyOpStarting(EventArgs e) { if (LengthyOpStarting != null) LengthyOpStarting(this, e); }
        protected void OnLengthyOpHasEnded(EventArgs e) { if (LengthyOpHasEnded != null) LengthyOpHasEnded(this, e); }

        private ObservableCollection<CodeSnippet> _recursiveQueryCodeSnippets;
        private int _importExportFileCount;

        /// <summary>
        /// Enumerates all snippets in the indicated directory and returns a collection of SnippetSummary
        /// </summary>
        /// <param name="path">The path in which to enumerate snippets</param>
        /// <param name="query">An optional search term. If this is null or an empty string, no filtering is performed</param>
        /// <param name="raiseLengthOpEvents">True if you want lengthy op events raised (defaults to true)</param>
        /// <returns>Returns a collection of SnippetSummary</returns>
        public async Task<ObservableCollection<CodeSnippet>> GetAllSnippetsAsync(string path, string query, bool raiseLengthOpEvents = true)
        {
            var folder = await StorageFolder.GetFolderFromPathAsync(path);
            return await GetAllSnippetsAsync(folder, query, raiseLengthOpEvents);
        }

        /// <summary>
        /// Enumerates all snippets in the indicated directory and returns a collection of SnippetSummary
        /// </summary>
        /// <param name="folder">The folder in which to enumerate snippets</param>
        /// <param name="query">An optional search term. If this is null or an empty string, no filtering is performed</param>
        /// <param name="raiseLengthOpEvents">True if you want lengthy op events raised (defaults to true)</param>
        /// <returns>Returns a collection of SnippetSummary</returns>
        public async Task<ObservableCollection<CodeSnippet>> GetAllSnippetsAsync(StorageFolder folder, string query, bool raiseLengthOpEvents = true)
        {
            bool lengthyOpRunning = false;
            try
            {
                // Get all the files in the specified folder
                var files = await folder.GetFilesAsync();
                if (files == null || files.Count == 0) return null;

                // Filter the list to only return .snippet files
                var snippetStorageFiles = from f in files
                                   where f.FileType.Equals(".snippet")
                                   orderby f.Path
                                   select f;

                if (!snippetStorageFiles.Any()) return null;

                var parsedSnippets = new ObservableCollection<CodeSnippet>();


                // Read and parse the XML for each snippet
                // Signal that the next op might take a few seconds
                if (raiseLengthOpEvents)
                {
                    OnLengthyOpStarting(EventArgs.Empty);
                    lengthyOpRunning = true;
                }

                foreach (var snippetStorageFile in snippetStorageFiles)
                {
                    var snippet = await GetSnippetAsync(snippetStorageFile);
                    if (SnippetMatchesQueryItem(snippet, query)) parsedSnippets.Add(snippet);
                }

                if (raiseLengthOpEvents)
                {
                    lengthyOpRunning = false;
                    OnLengthyOpHasEnded(EventArgs.Empty);
                }

                return parsedSnippets;
            }
            catch
            {
                if (lengthyOpRunning && raiseLengthOpEvents) OnLengthyOpHasEnded(EventArgs.Empty);

                return null;
            }
        }

        /// <summary>
        /// Enumerates all snippets in the indicated directory and returns a collection of SnippetSummary
        /// </summary>
        /// <param name="folder">The folder in which to enumerate snippets</param>
        /// <param name="raiseLengthOpEvents">True if you want lengthy op events raised (defaults to true)</param>
        /// <returns>Returns a collection of SnippetSummary</returns>
        public async Task<ObservableCollection<StorageFile>> GetAllSnippetStorageFilesAsync(StorageFolder folder, bool raiseLengthOpEvents = true)
        {
            bool lengthyOpRunning = false;
            try
            {
                // Signal that the next op might take a few seconds
                if (raiseLengthOpEvents)
                {
                    OnLengthyOpStarting(EventArgs.Empty);
                    lengthyOpRunning = true;
                }

                // Get all the files in the specified folder
                var files = await folder.GetFilesAsync();
                if (files == null || files.Count == 0) return null;

                // Filter the list to only return .snippet files
                var snippetStorageFiles = from f in files
                                          where f.FileType.Equals(".snippet")
                                          orderby f.Path
                                          select f;

                if (!snippetStorageFiles.Any()) return null;

                // Create the list of snippet file found in the specified folder
                var foundSnippetFiles = new ObservableCollection<StorageFile>(snippetStorageFiles);

                if (raiseLengthOpEvents)
                {
                    lengthyOpRunning = false;
                    OnLengthyOpHasEnded(EventArgs.Empty);
                }

                return foundSnippetFiles;
            }
            catch
            {
                if (lengthyOpRunning && raiseLengthOpEvents) OnLengthyOpHasEnded(EventArgs.Empty);

                return null;
            }
        }

        /// <summary>
        /// Enumerates all snippets in and below the indicated directory and returns a collection of SnippetSummary
        /// </summary>
        /// <param name="path">The path in which to enumerate snippets</param>
        /// <param name="query">A search query. If null, all snippets are returned</param>
        /// <returns>Returns a collection of SnippetSummary that match the query item</returns>
        public async Task<ObservableCollection<CodeSnippet>> GetAllSnippetsRecursivelyAsync(string path, string query)
        {
            _recursiveQueryCodeSnippets = new ObservableCollection<CodeSnippet>();

            // Start the process of recursively getting snippets...
            try
            {
                OnLengthyOpStarting(EventArgs.Empty);
                await DoGetAllSnippetsRecursivelyAsync(path, query);
                OnLengthyOpHasEnded(EventArgs.Empty);
            }
            catch
            {
                return null;
            }

            return _recursiveQueryCodeSnippets;
        }

        /// <summary>
        /// Enumerates all snippets in and below the indicated directory and returns a collection of SnippetSummary
        /// </summary>
        /// <param name="folder">The folder in which to enumerate snippets</param>
        /// <param name="query">A search query. If null, all snippets are returned</param>
        /// <returns>Returns a collection of SnippetSummary that match the query item</returns>
        public async Task<ObservableCollection<CodeSnippet>> GetAllSnippetsRecursivelyAsync(StorageFolder folder, string query)
        {
            _recursiveQueryCodeSnippets = new ObservableCollection<CodeSnippet>();

            // Start the process of recursively getting snippets...
            try
            {
                OnLengthyOpStarting(EventArgs.Empty);
                await DoGetAllSnippetsRecursivelyAsync(folder, query);
                OnLengthyOpHasEnded(EventArgs.Empty);
            }
            catch
            {
                return null;
            }

            return _recursiveQueryCodeSnippets;
        }

        /// <summary>
        /// Enumerates all snippets in and below the indicated directory and returns a collection of SnippetSummary
        /// </summary>
        /// <param name="path">The path in which to enumerate snippets</param>
        /// <param name="query">A search query. If null, all snippets are returned</param> 
        /// <returns>Returns a collection of SnippetSummary that match the query item</returns>
        private async Task DoGetAllSnippetsRecursivelyAsync(string path, string query)
        {
            // Get the snippets in this folder
            var tmpSnippets = await GetAllSnippetsAsync(path, query, false);
            if (tmpSnippets != null)
            {
                foreach (var codeSnippet in tmpSnippets)
                    _recursiveQueryCodeSnippets.Add(codeSnippet);
            }

            // Get all the folders in this folder...
            var folder = await StorageFolder.GetFolderFromPathAsync(path);
            var subFolders = await folder.GetFoldersAsync();
            foreach (var subFolder in subFolders)
                await DoGetAllSnippetsRecursivelyAsync(subFolder.Path, query);
        }

        /// <summary>
        /// Enumerates all snippets in and below the indicated directory and returns a collection of SnippetSummary
        /// </summary>
        /// <param name="folder">The folder in which to enumerate snippets</param>
        /// <param name="query">A search query. If null, all snippets are returned</param> 
        /// <returns>Returns a collection of SnippetSummary that match the query item</returns>
        private async Task DoGetAllSnippetsRecursivelyAsync(StorageFolder folder, string query)
        {
            // Get the snippets in this folder
            var tmpSnippets = await GetAllSnippetsAsync(folder, query, false);
            if (tmpSnippets != null)
            {
                foreach (var codeSnippet in tmpSnippets)
                    _recursiveQueryCodeSnippets.Add(codeSnippet);
            }

            // Get all the folders in this folder...
            var subFolders = await folder.GetFoldersAsync();
            foreach (var sub in subFolders)
                await DoGetAllSnippetsRecursivelyAsync(sub, query);
        }

        /// <summary>
        /// Determines if the code snippet matches the supplied query
        /// </summary>
        /// <param name="snippet">The code snippet object. If null, false is returned</param>
        /// <param name="query">The search term. If null or empty, true is returned (i.e. no filtering is done)</param>
        /// <returns>Returns true if either the query string is null or empty, or if the query matches key fields in the snippet. Returns false otherwise</returns>
        private bool SnippetMatchesQueryItem(CodeSnippet snippet, string query)
        {
            try
            {
                if (string.IsNullOrEmpty(query)) return true;
                if (snippet == null) return false;

                // If the query is a code language ("VB", "C#", "CSharp", etc.) then we assume the user wants to filter 
                // the list to just snippets of the specified language type...
                if (CodeSnippet.IsStringCodeLanguage(query))
                {
                    if (String.Compare(snippet.CodeLanguage, CodeSnippet.NormalizeCodeLanguage(query), StringComparison.Ordinal) == 0)
                        return true;
                }

                // Try to match the query as part of either the snippet's title or description
                if (!string.IsNullOrEmpty(snippet.Title) && snippet.Title.ToLower().Contains(query.ToLower()))
                    return true;

                if (!string.IsNullOrEmpty(snippet.Description) &&
                    snippet.Description.ToLower().Contains(query.ToLower()))
                    return true;
            }
            catch
            {
            }

            return false;
        }

        /// <summary>
        /// Reads the XML for the indicated snippet and parses the contents into a CodeSnippet object
        /// </summary>
        /// <param name="path">The path in which to enumerate snippets</param>
        /// <returns>Returns a CodeSnippet containing information parsed from the snippet file</returns>
        public async Task<CodeSnippet> GetSnippetAsync(string path)
        {
            CodeSnippet snippet;

            try
            {
                // Get a file in the Document library by specifying a relative path
                var file = await StorageFile.GetFileFromPathAsync(path);

                // Read and parse the snippet's XML
                snippet = await ReadSnippetXmlAsync(file);
            }
            catch (Exception)
            {
                snippet = null;
            }

            return snippet;
        }

        /// <summary>
        /// Reads the XML for the indicated snippet and parses the contents into a CodeSnippet object
        /// </summary>
        /// <param name="file">A StorageFile object that specifies the file to parse</param>
        /// <returns>Returns a CodeSnippet containing information parsed from the snippet file</returns>
        public async Task<CodeSnippet> GetSnippetAsync(StorageFile file)
        {
            CodeSnippet snippet;

            try
            {
                // Read and parse the snippet's XML
                snippet = await ReadSnippetXmlAsync(file);
            }
            catch (Exception)
            {
                snippet = null;
            }

            return snippet;
        }

        /// <summary>
        /// Outputs the specified snippet in XML format to a string
        /// </summary>
        /// <param name="snippet">The snippet to write</param>
        /// <returns>A stirng containing the snippet in XML format</returns>
        public async Task<string> WriteSnippetToString(CodeSnippet snippet)
        {
            var sb = new StringBuilder();
            string resultString = null;

            try
            {
                await Task.Run(() =>
                {
                    sb.Append("<?xml version=\"1.0\" encoding=\"utf-8\" ?> \r\n");
                    sb.Append("<CodeSnippets xmlns=\"http://schemas.microsoft.com/VisualStudio/2005/CodeSnippet\">\r\n");
                    sb.Append("  <CodeSnippet Format=\"1.0.0\">\r\n");
                    sb.Append("    <Header>\r\n");
                    sb.Append("      <Title>" + snippet.Title + "</Title>\r\n");
                    sb.Append("      <Author>" + snippet.Author + "</Author>\r\n");
                    sb.Append("      <Description>" + snippet.Description + "</Description>\r\n");
                    sb.Append("      <HelpUrl>" + snippet.HelpUrl + "</HelpUrl>\r\n");
                    sb.Append("      <Shortcut>" + snippet.Shortcut + "</Shortcut>\r\n\r\n");

                    sb.Append("      <SnippetTypes>\r\n");
                    sb.Append("        <SnippetType>" + snippet.SnippetType + "</SnippetType>\r\n");
                    sb.Append("      </SnippetTypes>\r\n\r\n");

                    if (snippet.Keywords != null && snippet.Keywords.Count > 0)
                    {
                        sb.Append("      <Keywords>\r\n");
                        foreach (var keyword in snippet.Keywords)
                            sb.Append("        <Keyword>" + keyword + "</Keyword>\r\n");
                        sb.Append("      </Keywords>\r\n");
                    }

                    sb.Append("    </Header>\r\n");
                    sb.Append("    <Snippet>\r\n\r\n");

                    if (String.Compare(snippet.CodeLanguage, "VB", StringComparison.Ordinal) == 0 &&
                        snippet.Assemblies != null && snippet.Assemblies.Count > 0)
                    {
                        sb.Append("      <References>\r\n");
                        foreach (var assembly in snippet.Assemblies)
                            sb.Append("        <Reference><Assembly>" + assembly + "</Assembly></Reference>\r\n");
                        sb.Append("      </References>\r\n\r\n");
                    }

                    if (snippet.Namespaces != null && snippet.Namespaces.Count > 0)
                    {
                        sb.Append("      <Imports>\r\n");
                        foreach (var ns in snippet.Namespaces)
                            sb.Append("        <Import><Namespace>" + ns + "</Namespace></Import>\r\n");
                        sb.Append("      </Imports>\r\n\r\n");
                    }

                    if (snippet.DeclarationList != null && snippet.DeclarationList.Count > 0)
                    {
                        sb.Append("      <Declarations>\r\n");
                        foreach (var dec in snippet.DeclarationList)
                        {
                            if (dec.DeclarationType == DeclarationTypes.Literal) 
                                sb.Append(dec.Editable ? "        <Literal Editable=\"true\">\r\n" : "        <Literal Editable=\"false\">\r\n");
                            else
                                sb.Append(dec.Editable ? "        <Object Editable=\"true\">\r\n" : "        <Object Editable=\"false\">\r\n");

                            sb.Append("          <ID>" + dec.ID + "</ID>\r\n");
                            sb.Append("          <Default>" + dec.Default + "</Default>\r\n");
                            sb.Append("          <Function>" + dec.Function + "</Function>\r\n");
                            sb.Append("          <ToolTip>" + dec.ToolTip + "</ToolTip>\r\n");

                            if (dec.DeclarationType == DeclarationTypes.Object)
                            {
                                sb.Append("          <Type>" + dec.Type + "</Type>\r\n");
                                sb.Append("        </Object>\r\n");
                            }
                            else
                                sb.Append("        </Literal>\r\n");
                        }
                        sb.Append("      </Declarations>\r\n\r\n");
                    }

                    var codeStartString = String.Format("      <Code Language=\"{0}\" Kind=\"{1}\" Delimiter=\"{2}\">\r\n",
                            string.IsNullOrEmpty(snippet.CodeLanguageForFile) ? "C#" : snippet.CodeLanguageForFile,
                            string.IsNullOrEmpty(snippet.CodeKind) ? "method body" : snippet.CodeKind,
                            string.IsNullOrEmpty(snippet.CodeDelimiter) ? "$" : snippet.CodeDelimiter);

                    sb.Append(codeStartString);
                    sb.Append("        <![CDATA[\r\n");
                    sb.Append(snippet.Code + "\r\n");
                    sb.Append("        ]]>\r\n");
                    sb.Append("      </Code>\r\n");
                    sb.Append("    </Snippet>\r\n");
                    sb.Append("  </CodeSnippet>\r\n");
                    sb.Append("</CodeSnippets>\r\n");

                    resultString = sb.ToString();
                });
            }
            catch
            {
                resultString = string.Empty;
            }

            return resultString;
        }

        /// <summary>
        /// Write to snippet file. If the file doesn't exist, it is created in the path provided.
        /// Note that changes can only be written to the local data store (e.g. not the Documents library)
        /// </summary>
        /// <param name="path">The path to the file</param>
        /// <param name="snippet">The snippet to write</param>
        /// <returns>Returns true if the file was successfully written, false otherwise</returns>
        public async Task<bool> WriteSnippetAsync(string path, CodeSnippet snippet)
        {
            try
            {
                StorageFile file;
                if (await FileSystemUtils.FileExistsAsync(path)) file = await StorageFile.GetFileFromPathAsync(path);
                else
                {
                    var folder = await StorageFolder.GetFolderFromPathAsync(path);
                    file = await folder.CreateFileAsync(snippet.Filename);
                }

                return await WriteSnippetAsync(file, snippet);
            }
            catch(System.IO.FileNotFoundException)
            {
            }
            catch(Exception)
            {
            }

            return false;
        }

        /// <summary>
        /// Write the snippet to a new file (which will be created in the specified folder)
        /// </summary>
        /// <param name="folder">The folder to contain the file (this may be either the local data store or the documents library)</param>
        /// <param name="snippet">The snippet to write</param> 
        /// <returns>Returns true if the file was successfully written, false otherwise</returns>
        public async Task<bool> WriteSnippetAsync(StorageFolder folder, CodeSnippet snippet)
        {
            try
            {
                var file = await folder.CreateFileAsync(snippet.Filename);
                return await WriteSnippetAsync(file, snippet);
            }
            catch (System.IO.FileNotFoundException)
            {
            }
            catch (Exception)
            {
            }

            return false;
        }

        /// <summary>
        /// Write the snippet to a new file (which will be created in the specified folder)
        /// </summary>
        /// <param name="file">The StorageFile object for the new file (this may be either the local data store or the documents library)</param>
        /// <param name="snippet">The snippet to write</param> 
        /// <returns>Returns true if the file was successfully written, false otherwise</returns>
        public async Task<bool> WriteSnippetAsync(StorageFile file, CodeSnippet snippet)
        {
            bool result;

            try
            {
                // Store the path to the snippet and its filename
                snippet.Path = file.Path;
                snippet.Filename = file.Name;

                // Open a stream for writing the XML to
                var stream = await file.OpenAsync(FileAccessMode.ReadWrite);
                using (var outputStream = stream.GetOutputStreamAt(0))
                {
                    // Create a DataWriter for writing the XML data
                    var writer = new DataWriter(outputStream);

                    writer.WriteString("<?xml version=\"1.0\" encoding=\"utf-8\" ?> \r\n");
                    writer.WriteString("<CodeSnippets xmlns=\"http://schemas.microsoft.com/VisualStudio/2005/CodeSnippet\">\r\n");
                    writer.WriteString("  <CodeSnippet Format=\"1.0.0\">\r\n");
                    writer.WriteString("    <Header>\r\n");
                    writer.WriteString("      <Title>" + snippet.Title + "</Title>\r\n");
                    writer.WriteString("      <Author>" + snippet.Author + "</Author>\r\n");
                    writer.WriteString("      <Description>" + snippet.Description + "</Description>\r\n");
                    writer.WriteString("      <HelpUrl>" + snippet.HelpUrl + "</HelpUrl>\r\n");
                    writer.WriteString("      <Shortcut>" + snippet.Shortcut + "</Shortcut>\r\n\r\n");

                    writer.WriteString("      <SnippetTypes>\r\n");
                    writer.WriteString("        <SnippetType>" + snippet.SnippetType + "</SnippetType>\r\n");
                    writer.WriteString("      </SnippetTypes>\r\n\r\n");

                    if (snippet.Keywords != null && snippet.Keywords.Count > 0)
                    {
                        writer.WriteString("      <Keywords>\r\n");
                        foreach (var keyword in snippet.Keywords)
                            writer.WriteString("        <Keyword>" + keyword + "</Keyword>\r\n");
                        writer.WriteString("      </Keywords>\r\n");
                    }

                    writer.WriteString("    </Header>\r\n");
                    writer.WriteString("    <Snippet>\r\n\r\n");

                    if (String.Compare(snippet.CodeLanguage, "VB", StringComparison.Ordinal) == 0 && 
                        snippet.Assemblies != null && snippet.Assemblies.Count > 0)
                    {
                        writer.WriteString("      <References>\r\n");
                        foreach (var assembly in snippet.Assemblies)
                            writer.WriteString("        <Reference><Assembly>" + assembly + "</Assembly></Reference>\r\n");
                        writer.WriteString("      </References>\r\n\r\n");
                    }

                    if (snippet.Namespaces != null && snippet.Namespaces.Count > 0)
                    {
                        writer.WriteString("      <Imports>\r\n");
                        foreach (var ns in snippet.Namespaces)
                            writer.WriteString("        <Import><Namespace>" + ns + "</Namespace></Import>\r\n");
                        writer.WriteString("      </Imports>\r\n\r\n");
                    }

                    if (snippet.DeclarationList != null && snippet.DeclarationList.Count > 0)
                    {
                        writer.WriteString("      <Declarations>\r\n");
                        foreach (var dec in snippet.DeclarationList)
                        {
                            if (dec.DeclarationType == DeclarationTypes.Literal)
                                writer.WriteString(dec.Editable
                                                       ? "        <Literal Editable=\"true\">\r\n"
                                                       : "        <Literal Editable=\"false\">\r\n");
                            else
                                writer.WriteString(dec.Editable
                                                       ? "        <Object Editable=\"true\">\r\n"
                                                       : "        <Object Editable=\"false\">\r\n");

                            writer.WriteString("          <ID>" + dec.ID + "</ID>\r\n");
                            writer.WriteString("          <Default>" + dec.Default + "</Default>\r\n");
                            writer.WriteString("          <Function>" + dec.Function + "</Function>\r\n");
                            writer.WriteString("          <ToolTip>" + dec.ToolTip + "</ToolTip>\r\n");

                            if (dec.DeclarationType == DeclarationTypes.Object)
                            {
                                writer.WriteString("          <Type>" + dec.Type + "</Type>\r\n");
                                writer.WriteString("        </Object>\r\n");
                            }
                            else
                                writer.WriteString("        </Literal>\r\n");
                        }
                        writer.WriteString("      </Declarations>\r\n\r\n");
                    }

                    var codeStartString = String.Format("      <Code Language=\"{0}\" Kind=\"{1}\" Delimiter=\"{2}\">\r\n",
                        string.IsNullOrEmpty(snippet.CodeLanguageForFile) ? "C#" : snippet.CodeLanguageForFile,
                        string.IsNullOrEmpty(snippet.CodeKind) ? "method body" : snippet.CodeKind,
                        string.IsNullOrEmpty(snippet.CodeDelimiter) ? "$" : snippet.CodeDelimiter);

                    writer.WriteString(codeStartString);
                    writer.WriteString("        <![CDATA[\r\n");
                    writer.WriteString(snippet.Code + "\r\n");
                    writer.WriteString("        ]]>\r\n");
                    writer.WriteString("      </Code>\r\n");
                    writer.WriteString("    </Snippet>\r\n");
                    writer.WriteString("  </CodeSnippet>\r\n");
                    writer.WriteString("</CodeSnippets>\r\n");

                    // Commit the data in the buffer to disk
                    await writer.StoreAsync();

                    // Flush any buffers
                    await outputStream.FlushAsync();
                }

                result = true;
            }
            catch
            {
                result = false;
            }

            return result;
        }

        /// <summary>
        /// Reads and parses a snippet XML file
        /// </summary>
        /// <param name="file">A StorageFile object that specifies the file to parse</param>
        /// <returns>Returns a CodeSnippet containing information parsed from the snippet file</returns>
        private async Task<CodeSnippet> ReadSnippetXmlAsync(StorageFile file)
        {
            try
            {
                // Create a new snippet to hold values parsed from the snippet's XML file
                var snippet = new CodeSnippet();

                // Store the path to the snippet and its filename
                snippet.Path = file.Path;
                snippet.Filename = file.Name;

                // Read the xml
                var doc = await XmlDocument.LoadFromFileAsync(file);
                var xml = doc.GetXml();

                // Create an XDocument which we can use with LINQ-to-XML
                var xdoc = XDocument.Parse(xml);

                // Query the data
                var allNodes = from nodes in xdoc.DescendantNodes() select nodes;

                foreach (var xNode in allNodes)
                {
                    var xElement = xNode as XElement;
                    if (xElement == null || xElement.Name == null || xElement.Name.LocalName == null)
                        continue;

                    // Header parsing...
                    if (xElement.Name.LocalName.Equals("Header"))
                    {
                        // Enumerate all the header elements
                        var results = from element in xElement.Descendants() select element;
                        foreach (var element in results)
                        {
                            if (element == null || element.Name == null || element.Name.LocalName == null ||
                                element.Value == null)
                                continue;

                            if (element.Name.LocalName.Equals("Keyword"))
                                snippet.Keywords.Add(element.Value);
                            else
                                snippet[element.Name.LocalName] = element.Value;
                            // Asign XML elements to matching header properties in the snippet object
                        }

                        continue;
                    }

                    // Snippet parsing...
                    if (xElement.Name.LocalName.Equals("Snippet"))
                    {
                        // Enumerate all the snippet elements
                        var results = from element in xElement.Descendants() select element;
                        foreach (var element in results)
                        {
                            if (element == null || element.Name == null || element.Name.LocalName == null ||
                                element.Value == null)
                                continue;

                            if (element.Name.LocalName.Equals("Namespace"))
                                snippet.Namespaces.Add(element.Value);
                            else if (element.Name.LocalName.Equals("Assembly"))
                                snippet.Assemblies.Add(element.Value);
                            else if (element.Name.LocalName.Equals("Literal") || element.Name.LocalName.Equals("Object"))
                            {
                                var dec = new Declaration();
                                dec.DeclarationType = element.Name.LocalName.Equals("Literal")
                                                          ? DeclarationTypes.Literal
                                                          : DeclarationTypes.Object;
                                if (element.HasAttributes &&
                                    element.Attribute("Editable") != null &&
                                    element.Attribute("Editable").Value != null)
                                    dec.Editable = bool.Parse(element.Attribute("Editable").Value);

                                // Enumerate all the declaration elements
                                var decResults = from decElement in element.Descendants() select decElement;
                                foreach (var decElement in decResults)
                                {
                                    if (decElement == null || decElement.Name == null ||
                                        decElement.Name.LocalName == null || decElement.Value == null)
                                        continue;

                                    dec[decElement.Name.LocalName] = decElement.Value;
                                }
                                snippet.DeclarationList.Add(dec);
                            }
                            else if (element.Name.LocalName.Equals("Code"))
                            {
                                snippet["Code"] = element.Value;

                                if (element.HasAttributes)
                                {
                                    if (element.Attribute("Language") != null)
                                    {
                                        var lang = element.Attribute("Language").Value.ToLower();
                                        switch (lang)
                                        {
                                            case "vb":
                                                snippet["CodeLanguage"] = "VB";
                                                break;

                                            case "csharp":
                                                snippet["CodeLanguage"] = "C#";
                                                break;

                                            case "xml":
                                                snippet["CodeLanguage"] = "XML";
                                                break;

                                            case "cpp":
                                                snippet["CodeLanguage"] = "C++";
                                                break;

                                            case "javascript":
                                                snippet["CodeLanguage"] = "JavaScript";
                                                break;

                                            case "jscript":
                                                snippet["CodeLanguage"] = "JScript";
                                                break;

                                            case "sql":
                                                snippet["CodeLanguage"] = "SQL";
                                                break;

                                            case "html":
                                                snippet["CodeLanguage"] = "HTML";
                                                break;

                                            default:
                                                snippet["CodeLanguage"] = "Unknown";
                                                break;
                                        }
                                    }

                                    if (element.Attribute("Kind") != null)
                                        snippet["CodeKind"] = element.Attribute("Kind").Value;

                                    if (element.Attribute("Delimiter") != null)
                                        snippet["CodeDelimiter"] = element.Attribute("Delimiter").Value;
                                }
                            }
                            else
                                snippet[element.Name.LocalName] = element.Value;
                        }
                    }

                }

                return snippet;
            }
            catch
            {
            }

            return null;
        }

        /// <summary>
        /// Starts and orchestrates the process of recursively importing all snippet files from the specified folder
        /// (which is normally the Documents library) to the local data store. The structure of the imported folder tree
        /// is maintained at the destination
        /// </summary>
        /// <param name="fromFolder">The StorageFolder object specifying the folder to import files from</param>
        /// <returns>Returns the number of files copied, or -1 if an error occurred</returns>
        public async Task<int> ImportAllSnippetsFromDocumentsLibrary(StorageFolder fromFolder)
        {
            _importExportFileCount = 0;

            // Start the process of recursively getting snippets...
            try
            {
                OnLengthyOpStarting(EventArgs.Empty);
                await DoImportAllSnippetsRecursivelyAsync(fromFolder, fromFolder);
                OnLengthyOpHasEnded(EventArgs.Empty);
            }
            catch
            {
                _importExportFileCount = -1;
            }

            return _importExportFileCount;
        }

        /// <summary>
        /// Get all the snippet files in the specified folder and copy them to a local data folder of the same name.
        /// If the destination folder doesn't exist, we create it. If the file exists at the destination, we overwrite it.
        /// </summary>
        /// <param name="importRootFolder">The root folder from which to import files (normally the VS snippets folder in the documents library)</param>
        /// <param name="currentImportFolder">The current folder (e.g. a subfolder of the importRootFolder) from which to import files</param>
        private async Task DoImportAllSnippetsRecursivelyAsync(StorageFolder importRootFolder, StorageFolder currentImportFolder)
        {
            // Get the destination folder, creating it if necessary
            var pathToAppend = currentImportFolder.Path.Remove(0, importRootFolder.Path.Length);
            var pathToCopyTo = FileSystemUtils.GetRootSnippetFolderPath() + pathToAppend;

            // Ensure all elements of the path to copy to exist in our local data store...
            var destinationFolder = await FileSystemUtils.EnsureAllLocalDataPathElementsExistAsync(pathToCopyTo);

            // Get all the files we need to copy
            var files = await GetAllSnippetStorageFilesAsync(currentImportFolder, false);  
            if (files != null)
            {
                foreach (var file in files)
                {
                    try
                    {
                        file.CopyAsync(destinationFolder, file.Name, NameCollisionOption.ReplaceExisting);
                        _importExportFileCount++;
                    }
                    catch
                    {
                    }
                }
            }

            // Get all the folders in this folder...
            var subFolders = await currentImportFolder.GetFoldersAsync();
            foreach (var sub in subFolders)
                await DoImportAllSnippetsRecursivelyAsync(importRootFolder, sub);
        }

        /// <summary>
        /// Exports all snippet files from the local data store to the specified folder (e.g. the Visual Studio
        /// code snippets folder structure in the documents library)
        /// </summary>
        /// <param name="toFolder">The StorageFolder object specifying the folder to which files will be copied</param>
        /// <returns>Returns the number of files copied, or -1 if an error occurred</returns>
        public async Task<int> ExportAllSnippetsToDocumentsLibrary(StorageFolder toFolder)
        {
            _importExportFileCount = 0;

            // Start the process of recursively getting snippets...
            try
            {
                OnLengthyOpStarting(EventArgs.Empty);
                await DoExportAllSnippetsRecursivelyAsync(toFolder, await FileSystemUtils.GetRootSnippetStorageFolderAsync());
                OnLengthyOpHasEnded(EventArgs.Empty);
            }
            catch
            {
                _importExportFileCount = -1;
            }

            return _importExportFileCount;
        }

        /// <summary>
        /// Get all the snippet files in the specified (currentExportFolder) folder and copy them to a local data folder of the same name.
        /// If the destination folder doesn't exist, we create it. If the file exists at the destination, we overwrite it.
        /// </summary>
        /// <param name="currentTargetFolder">The folder to which we export files (e.g. the code snippets folder in the user's documents library)</param>
        /// <param name="currentExportFromFolder">The current folder from which we're exporting files (from the local data store)</param>
        private async Task DoExportAllSnippetsRecursivelyAsync(StorageFolder currentTargetFolder, StorageFolder currentExportFromFolder)
        {
            // Get the name of the destination folder...
            var subFolderName = currentExportFromFolder.Path;
            if (string.IsNullOrEmpty(subFolderName)) throw new Exception();

            if (subFolderName.EndsWith("\\")) subFolderName = subFolderName.TrimEnd('\\');
            
            var i = subFolderName.LastIndexOf("\\", System.StringComparison.Ordinal);
            if (i == -1) throw new Exception();
            subFolderName = subFolderName.Substring(i);
            
            if (subFolderName.StartsWith("\\")) subFolderName = subFolderName.TrimStart('\\');
            if (subFolderName.EndsWith("\\")) subFolderName = subFolderName.TrimEnd('\\');

            // If the current folder is not the snippet root folder, make sure it exists at the target location
            if (String.Compare(subFolderName, App.RootSnippetFolderName, StringComparison.Ordinal) != 0)
                currentTargetFolder = await currentTargetFolder.CreateFolderAsync(subFolderName, CreationCollisionOption.OpenIfExists);

            // Get all the files we need to copy
            var files = await GetAllSnippetStorageFilesAsync(currentExportFromFolder, false);
            if (files != null)
            {
                foreach (var file in files)
                {
                    try
                    {
                        file.CopyAsync(currentTargetFolder, file.Name, NameCollisionOption.ReplaceExisting);
                        _importExportFileCount++;
                    }
                    catch
                    {
                    }
                }
            }

            // Get all the folders in this folder...
            var exportFromSubFolders = await currentExportFromFolder.GetFoldersAsync();
            foreach (var exportFromSubFolder in exportFromSubFolders)
                await DoExportAllSnippetsRecursivelyAsync(currentTargetFolder, exportFromSubFolder);
        }
    }
}
