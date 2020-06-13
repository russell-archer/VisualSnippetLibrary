using System;

namespace VisualSnippetLibrary.DataModel
{
    public class Declaration : CodeSnippetBase, IDeclaration
    {
        private DeclarationTypes _declarationType;
        private bool _editable;
        private string _id;
        private string _type;
        private string _toolTip;
        private string _default;
        private string _function;

        public DeclarationTypes DeclarationType
        {
            get { return _declarationType; }
            set { _declarationType = value; OnPropertyChanged(); OnPropertyChanged("DeclarationTypeString"); }
        }

        public bool Editable
        {
            get { return _editable; }
            set { _editable = value; OnPropertyChanged(); }
        }

        public string ID
        {
            get { return _id; }
            set { _id = value; OnPropertyChanged(); }
        }

        public string Type  // For Object declarations-only
        {
            get { return _type; }
            set { _type = value; OnPropertyChanged(); }
        }

        public string ToolTip
        {
            get { return _toolTip; }
            set { _toolTip = value; OnPropertyChanged(); }
        }

        public string Default
        {
            get { return _default; }
            set { _default = value; OnPropertyChanged(); }
        }

        public string Function
        {
            get { return _function; }
            set { _function = value; OnPropertyChanged(); }
        }

        public string DeclarationTypeString
        {
            get { return DeclarationType.ToString(); }
            set
            {
                if (value == null) return;

                DeclarationType = String.Compare(value.ToLower(), "literal", StringComparison.Ordinal) == 0 ? DeclarationTypes.Literal : DeclarationTypes.Object;
                OnPropertyChanged();
            }
        }

        public string FormattedDecString
        {
            get { return ToString(); }
        }

        public new string ToString()
        {
            return (string.IsNullOrEmpty(ID) ? "" : ID + " ") + 
                   (string.IsNullOrEmpty(ToolTip) ? DeclarationType.ToString() : DeclarationType.ToString() + " (" + ToolTip + ")");
        }
    }
}