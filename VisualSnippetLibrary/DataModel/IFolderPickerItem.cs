using System;

namespace VisualSnippetLibrary.DataModel
{
    interface IFolderPickerItem
    {
        string Name { get; set; }
        DateTime? Created { get; set; }
    }
}
