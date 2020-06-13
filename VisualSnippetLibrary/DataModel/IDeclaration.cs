namespace VisualSnippetLibrary.DataModel
{
    public enum DeclarationTypes
    {
        Literal,
        Object
    };

    interface IDeclaration
    {
        DeclarationTypes DeclarationType { get; set; }
        bool Editable { get; set; }
        string ID { get; set; }
        string Type { get; set; }  // For Object declaration types only
        string ToolTip { get; set; }
        string Default { get; set; }
        string Function { get; set; }
    }
}