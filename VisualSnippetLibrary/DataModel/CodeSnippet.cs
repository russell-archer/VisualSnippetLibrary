using System;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Threading.Tasks;

namespace VisualSnippetLibrary.DataModel
{
    /// <summary>
    /// The XML schema for snippets can be found here:
    /// http://msdn.microsoft.com/en-us/library/ms171418.aspx
    /// </summary>
    public class CodeSnippet : CodeSnippetBase, ICodeSnippet
    {
        private static readonly Uri BaseUri = new Uri("ms-appx:///");

        private string _filename;
        private string _path;
        private string _title;
        private string _description;
        private string _author;
        private string _helpUrl;
        private string _shortcut;
        private string _snippetType;
        private string _codeLanguage;
        private string _codeKind;
        private string _codeDelimiter;
        private string _code;

        private ObservableCollection<string> _keywords;
        private ObservableCollection<string> _assemblies;
        private ObservableCollection<string> _namespaces;
        private ObservableCollection<Declaration> _declarationList;

        // Choices...
        private readonly ObservableCollection<string> _codeLanguageChoices;
        private readonly ObservableCollection<string> _snippetTypeChoices;
        private readonly ObservableCollection<string> _codeKindChoices;

        // File info (not contained in XML)...
        public string Filename
        {
            get { return _filename; }
            set { _filename = value; OnPropertyChanged();}
        }

        public string Path
        {
            get { return _path; }
            set { _path = value; OnPropertyChanged();}
        }

        // Header...
        public string Title
        {
            get { return _title; }
            set { _title = value; OnPropertyChanged();}
        }

        public string Description
        {
            get { return _description; }
            set { _description = value; OnPropertyChanged();}
        }

        public string Author
        {
            get { return _author; }
            set { _author = value; OnPropertyChanged();}
        }

        public string HelpUrl
        {
            get { return _helpUrl; }
            set { _helpUrl = value; OnPropertyChanged();}
        }

        public string Shortcut
        {
            get { return _shortcut; }
            set { _shortcut = value; OnPropertyChanged();}
        }

        public string SnippetType
        {
            get { return _snippetType; }
            set { _snippetType = value; OnPropertyChanged();}
        }

        // Code...
        public string CodeLanguage
        {
            get { return _codeLanguage; }
            set 
            { 
                _codeLanguage = value; 
                OnPropertyChanged();
                OnPropertyChanged("IsVB"); 
                OnPropertyChanged("BackgroundImage"); 
                OnPropertyChanged("CodeLanguageForFile"); 
            }
        }

        public string CodeKind
        {
            get { return _codeKind; }
            set { _codeKind = value; OnPropertyChanged();}
        }

        public string CodeDelimiter
        {
            get { return _codeDelimiter; }
            set { _codeDelimiter = value; OnPropertyChanged();}
        }

        public string Code
        {
            get { return _code; }
            set { _code = value; OnPropertyChanged();}
        }

        // Snippet...
        public ObservableCollection<string> Keywords
        {
            get { return _keywords; }
            set { _keywords = value; OnPropertyChanged();}
        }

        public ObservableCollection<string> Assemblies
        {
            get { return _assemblies; }
            set { _assemblies = value; OnPropertyChanged();}
        }

        public ObservableCollection<string> Namespaces
        {
            get { return _namespaces; }
            set { _namespaces = value; OnPropertyChanged();}
        }

        public ObservableCollection<Declaration> DeclarationList
        {
            get { return _declarationList; }
            set { _declarationList = value; OnPropertyChanged();}
        }

        // Choices...
        public ObservableCollection<string> CodeLanguageChoices
        {
            get { return _codeLanguageChoices; }
        }

        public ObservableCollection<string> SnippetTypeChoices
        {
            get { return _snippetTypeChoices; }
        }

        public ObservableCollection<string> CodeKindChoices
        {
            get { return _codeKindChoices; }
        }

        public string BackgroundImage 
        { 
            get 
            {
                switch (CodeLanguage)
                {
                    case "C#":
                        return BaseUri + "Assets/CSharp.png";

                    case "C++":
                        return BaseUri + "Assets/CPP.png";

                    case "VB":
                    case "XML":
                    case "JavaScript":
                    case "JScript":
                    case "SQL":
                    case "HTML":
                        return BaseUri + "Assets/" + CodeLanguage + ".png";
                }

                return BaseUri + "Assets/Unknown.png";
            } 
        }

        public string CodeLanguageForFile
        {
            get
            {
                switch (CodeLanguage)
                {
                    case "C#":
                        return "CSharp";

                    case "C++":
                        return "CPP";

                    case "VB":
                    case "XML":
                    case "JavaScript":
                    case "JScript":
                    case "SQL":
                    case "HTML":
                        return CodeLanguage;
                }

                return "Unknown";
            }
        }

        public bool IsVB { get { return String.Compare(Code, "VB", StringComparison.Ordinal) == 0; } }

        public CodeSnippet()
        {
            Assemblies = new ObservableCollection<string>();
            Namespaces = new ObservableCollection<string>();
            DeclarationList = new ObservableCollection<Declaration>();
            Keywords = new ObservableCollection<string>();

            // Choice lists...
            _codeLanguageChoices = new ObservableCollection<string> { "C#", "C++", "VB", "XML", "JavaScript", "JScript", "SQL", "HTML" };
            _snippetTypeChoices = new ObservableCollection<string> { "SurroundsWith", "Expansion", "Refactoring" };
            _codeKindChoices = new ObservableCollection<string> { "method body", "method decl", "type decl", "file", "any" };
        }

        public static CodeSnippet Copy(CodeSnippet snippet)
        {
            if(snippet == null) throw new ArgumentNullException();

            var targetSnippet = new CodeSnippet();

            try
            {
                foreach (var assembly in snippet.Assemblies)
                    targetSnippet.Assemblies.Add(assembly);

                foreach (var ns in snippet.Namespaces)
                    targetSnippet.Namespaces.Add(ns);

                foreach (var dec in snippet.DeclarationList)
                    targetSnippet.DeclarationList.Add(dec);

                var properties = snippet.GetType().GetTypeInfo().DeclaredProperties;
                foreach (var propertyInfo in properties)
                {
                    // Skip copy the property if it's a collection or read-only (would have liked a more 
                    // generic test for any collection/array type but it doesn't seem to be possible...)
                    if (propertyInfo.PropertyType == typeof(ObservableCollection<string>) ||
                        propertyInfo.PropertyType == typeof(ObservableCollection<Declaration>) ||
                        !propertyInfo.CanWrite)
                        continue;

                    targetSnippet.GetType()
                                 .GetTypeInfo()
                                 .GetDeclaredProperty(propertyInfo.Name)
                                 .SetValue(targetSnippet, propertyInfo.GetValue(snippet));
                }
            }
            catch
            {
                targetSnippet = null;
            }

            return targetSnippet;
        }

        public static CodeSnippet CheckForNullStrings(CodeSnippet snippet)
        {
            if (snippet == null) throw new ArgumentNullException();

            try
            {
                var properties = snippet.GetType().GetTypeInfo().DeclaredProperties;
                foreach (var propertyInfo in properties)
                {
                    if (propertyInfo.PropertyType == typeof (String) &&
                        propertyInfo.CanWrite &&
                        propertyInfo.GetValue(snippet) == null)
                        snippet.GetType().GetTypeInfo().GetDeclaredProperty(propertyInfo.Name).SetValue(snippet, String.Empty);
                }
            }
            catch
            {
            }

            return snippet;
        }

        public async static Task<CodeSnippet> CreateNew()
        {
            string userName;

            try
            {
                userName = await Windows.System.UserProfile.UserInformation.GetDisplayNameAsync();
            }
            catch
            {
                userName = "Author Name";
            }

            var snippet = new CodeSnippet
            {
                Author = userName,
                Description = "Snippet Description",
                Code = "Add code here",
                CodeKind = "method body",
                CodeLanguage = "C#",
                CodeDelimiter = "$",
                SnippetType = "Expansion",
                Title = "New Snippet"
            };

            return snippet;
        }

        public static bool IsStringCodeLanguage(string text)
        {
            switch (text.ToLower())
            {
                case "c#":
                case "csharp":
                case "c++":
                case "cpp":
                case "vb":
                case "xml":
                case "javascript":
                case "jscript":
                case "sql":
                case "html":
                    return true;
            }
            return false;
        }

        public static string NormalizeCodeLanguage(string text)
        {
            switch (text.ToLower())
            {
                case "c#":
                case "csharp":
                    return "C#";

                case "c++":
                case "cpp":
                    return "C++";

                case "vb":
                    return "VB";

                case "xml":
                    return "XML";

                case "javascript":
                    return "JavaScript";

                case "jscript":
                    return "JScript";

                case "sql":
                    return "SQL";

                case "html":
                    return "HTML";
            }

            return null;
        }

        public new string ToString()
        {
            return Code;
        }
    }
}
