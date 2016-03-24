using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace WNSChat.Client.Utilities
{
    /// <summary>
    /// Not used, a better solution was using the DataType property of DataTemplates.
    /// See http://stackoverflow.com/questions/5644392/conditional-list-itemtemplate-or-datatemplate-in-wpf/5644414#5644414
    /// </summary>
    public class CustomDataTemplateSelector : DataTemplateSelector
    {
        /** A Dictionary that maps types to data templates */
        public Dictionary<Type, DataTemplate> DataTemplateMap { get; set; }

        /** Selects the DataTemplate based on the item's type. */
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            FrameworkElement element = container as FrameworkElement;

            if (element != null && item != null && this.DataTemplateMap != null)
            {
                //Return the DataTemplate for the type that is a superclass of item.GetType().  Since KVP is a value type, no null checks are needed
                return this.DataTemplateMap.FirstOrDefault(kvp => kvp.Key.IsAssignableFrom(item.GetType())).Value;
            }

            return null;
        }
    }
}
