using Windows.UI.Xaml.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace VisualSnippetLibrary.UserControls
{
    public sealed partial class RawXmlFlyout : UserControl
    {
        public string Code { get; set; }

        public RawXmlFlyout()
        {
            this.InitializeComponent();
        }

        public RawXmlFlyout(string code, bool allowEditing = false)
        {
            this.InitializeComponent();

            Code = code;

            if (allowEditing)
                CodeEditor.IsReadOnly = false;
        }
    }
}
