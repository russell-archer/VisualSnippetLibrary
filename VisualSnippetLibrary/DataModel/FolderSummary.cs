using System;
using Windows.UI.Xaml;

namespace VisualSnippetLibrary.DataModel
{
    public class FolderSummary : IFolderSummary
    {
        public string Path { get; set; }
        public int FileCount { get; set; }

        public string TopFolderName
        {
            get
            {
                try
                {
                    if (FileCount == -1) return "";

                    var pathParts = this.Path.Split('\\');
                    return pathParts[pathParts.Length - 1];
                }
                catch
                {
                    return "";
                }
            }
        }

        public string FileCountFormat
        {
            get
            {
                if(FileCount == -1) return "";
                else if (FileCount == 1) return "(1 snippet)";
                else if (FileCount > 0) return String.Format("({0} snippets)", FileCount.ToString());

                return "";
            }
        }

        public Visibility UpFolderButtonVisibility 
        {
            get 
            {
                return FileCount == -1 ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public Visibility FolderInfoVisibility
        {
            get
            {
                return FileCount > -1 ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public override string ToString()
        {
            if (FileCount == -1) return "";
            else if (FileCount > 0) return TopFolderName + " (" + FileCount.ToString() + ")";
            else return TopFolderName;
        }
    }
}
