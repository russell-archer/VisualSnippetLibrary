using VisualSnippetLibrary.DataModel;
using Windows.UI.Xaml.Controls;

namespace VisualSnippetLibrary.UserControls
{
    public sealed partial class DeclarationItemFlyout : UserControl
    {
        public Declaration DeclarationItem { get; set; }

        public DeclarationItemFlyout()
        {
            this.InitializeComponent();
        }

        public DeclarationItemFlyout(Declaration declaration)
        {
            this.InitializeComponent();

            DeclarationItem = declaration;

            ComboDeclarationType.SelectedIndex = declaration.DeclarationType == DeclarationTypes.Literal ? 0 : 1;
            CheckBoxEditable.IsChecked = declaration.Editable;
        }

        private void ComboDeclarationType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboDeclarationType.SelectedIndex == -1)
                return;

            DeclarationItem.DeclarationTypeString = ComboDeclarationType.SelectedIndex == 0 ? "Literal" : "Object";
        }

        private void CheckBoxEditable_Unchecked(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            DeclarationItem.Editable = false;
        }

        private void CheckBoxEditable_Checked(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            DeclarationItem.Editable = true;
        }
    }
}
