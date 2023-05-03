using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;

namespace SourceChord.GridExtra
{
    [TypeConverter(typeof(BreakPointsTypeConverter))]
    public class BreakPoints : DependencyObject
    {
        public double XS_SM
        {
            get { return (double)GetValue(XS_SMProperty); }
            set { SetValue(XS_SMProperty, value); }
        }
        // Using a DependencyProperty as the backing store for XS_SM.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty XS_SMProperty =
            DependencyProperty.Register("XS_SM", typeof(double), typeof(BreakPoints), new PropertyMetadata(768.0));

        public double SM_MD
        {
            get { return (double)GetValue(SM_MDProperty); }
            set { SetValue(SM_MDProperty, value); }
        }
        // Using a DependencyProperty as the backing store for SM_MD.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SM_MDProperty =
            DependencyProperty.Register("SM_MD", typeof(double), typeof(BreakPoints), new PropertyMetadata(992.0));


        public double MD_LG
        {
            get { return (double)GetValue(MD_LGProperty); }
            set { SetValue(MD_LGProperty, value); }
        }
        // Using a DependencyProperty as the backing store for MD_LG.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MD_LGProperty =
            DependencyProperty.Register("MD_LG", typeof(double), typeof(BreakPoints), new PropertyMetadata(1200.0));

        public BreakPoints()
        {
        }
    }
}
