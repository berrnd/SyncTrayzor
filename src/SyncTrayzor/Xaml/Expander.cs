using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SyncTrayzor.Xaml
{
    public class Expander : ContentControl
    {
        public object HeaderLeft
        {
            get { return (object)GetValue(HeaderLeftProperty); }
            set { SetValue(HeaderLeftProperty, value); }
        }

        public static readonly DependencyProperty HeaderLeftProperty =
            DependencyProperty.Register("HeaderLeft", typeof(object), typeof(Expander), new PropertyMetadata(null));

        

        public object HeaderRight
        {
            get { return (object)GetValue(HeaderRightProperty); }
            set { SetValue(HeaderRightProperty, value); }
        }

        public static readonly DependencyProperty HeaderRightProperty =
            DependencyProperty.Register("HeaderRight", typeof(object), typeof(Expander), new PropertyMetadata(null));

        
    }
}
