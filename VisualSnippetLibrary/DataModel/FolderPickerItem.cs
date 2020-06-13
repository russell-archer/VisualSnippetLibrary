using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace VisualSnippetLibrary.DataModel
{
    public class FolderPickerItem : IFolderPickerItem, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string _name;
        public string Name
        {
            get { return _name; }
            set { _name = value; OnPropertyChanged();}
        }

        private DateTime? _created;
        public DateTime? Created
        {
            get { return _created; }
            set { _created = value; OnPropertyChanged();}
        }

        private string _path;
        public string Path
        {
            get { return _path; }
            set { _path = value; OnPropertyChanged();}
        }

        private bool _isFolder;
        public bool IsFolder
        {
            get { return _isFolder; }
            set { _isFolder = value; OnPropertyChanged();}
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
