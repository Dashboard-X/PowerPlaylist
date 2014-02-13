using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace MVT
{
    public partial class MinimizeGlassButton : UserControl
    {
        public MinimizeGlassButton()
        {
            InitializeComponent();

            this.MouseEnter += new MouseEventHandler(MinimizeGlassButton_MouseEnter);
            this.MouseLeave += new MouseEventHandler(MinimizeGlassButton_MouseLeave);
            this.MouseLeftButtonUp += new MouseButtonEventHandler(MinimizeGlassButton_MouseLeftButtonUp);
        }

        void MinimizeGlassButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            OnClick.Begin();
        }

        void MinimizeGlassButton_MouseLeave(object sender, MouseEventArgs e)
        {
            OnMouseOut.Begin();
        }

        void MinimizeGlassButton_MouseEnter(object sender, MouseEventArgs e)
        {
            OnMouseOver.Begin();
        }
    }
}
