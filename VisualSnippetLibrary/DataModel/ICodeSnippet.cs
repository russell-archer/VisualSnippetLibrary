using System.Collections.ObjectModel;

namespace VisualSnippetLibrary.DataModel
{
    interface ICodeSnippet
    {
        // File info (not contained in XML)...
        string Filename { get; set; }  // Filename (no extension)
        string Path { get; set; }  // Full file path

        // Header...
        string Title { get; set; }
        string Description { get; set; }
        string Author { get; set; }
        string HelpUrl { get; set; }
        string Shortcut { get; set; }
        string SnippetType { get; set; }
        ObservableCollection<string> Keywords { get; set; }

        // Snippet...
        ObservableCollection<string> Assemblies { get; set; }  // VB-only
        ObservableCollection<string> Namespaces { get; set; }
        ObservableCollection<Declaration> DeclarationList { get; set; }

        // Code...
        string CodeLanguage { get; set; }
        string CodeKind { get; set; }
        string CodeDelimiter { get; set; }
        string Code { get; set; }

        // UI (tile background)
        string BackgroundImage { get; }
    
        // Override the ToString() method
        string ToString();  
    }
}
