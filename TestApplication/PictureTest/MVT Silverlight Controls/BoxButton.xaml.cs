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
    public partial class BoxButton : UserControl
    {
        public BoxButton()
        {
            InitializeComponent();

            this.MouseEnter += new MouseEventHandler(BoxButton_MouseEnter);
            this.MouseLeave += new MouseEventHandler(BoxButton_MouseLeave);
            this.MouseLeftButtonUp += new MouseButtonEventHandler(BoxButton_MouseLeftButtonUp);

            this.Cursor = Cursors.Hand;
        }

        void BoxButton_MouseLeave(object sender, MouseEventArgs e)
        {
            st_leave.Begin();
        }

        void BoxButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            st_click.Begin();
        }

        void BoxButton_MouseEnter(object sender, MouseEventArgs e)
        {
            st_enter.Begin();
        }
    }
}
