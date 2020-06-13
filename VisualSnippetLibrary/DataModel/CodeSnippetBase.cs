using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using VisualSnippetLibrary.Properties;

namespace VisualSnippetLibrary.DataModel
{
    public class CodeSnippetBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Indexer for non-collection properties (strings, ints, etc.)
        /// </summary>
        /// <param name="name">The name of the property</param>
        /// <returns>The value of the property</returns>
        public string this[string name]
        {
            get { return PropertyGetter(name).ToString(); }
            set { PropertySetter(name, value); }
        }
        
        /// <summary>
        /// Gets the value of a scalar property by its name
        /// </summary>
        /// <param name="propertyName">Property name</param>
        /// <returns>The value of the property, or null if the property doesn't exist</returns>
        protected object PropertyGetter(string propertyName)
        {
            try
            {
                var typeInfo = this.GetType().GetTypeInfo();
                return (from property in typeInfo.DeclaredProperties
                        where property.Name.Equals(propertyName)
                        select property.GetValue(this)).FirstOrDefault();
            }
            catch
            {
            }

            return null;
        }

        /// <summary>
        /// Sets the value of a scalar property by its name
        /// </summary>
        /// <param name="propertyName">Property name</param>
        /// <param name="val">The value to assign</param>
        /// <returns>True if the value of the property was was set, false otherwise</returns>
        protected void PropertySetter(string propertyName, object val)
        {
            try
            {
                var typeInfo = this.GetType().GetTypeInfo();
                foreach (var property in typeInfo.DeclaredProperties)
                {
                    if (property.Name.Equals(propertyName))
                    {
                        property.SetValue(this, val);
                        OnPropertyChanged(propertyName);
                        return;
                    }
                }
            }
            catch
            {
            }
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}