using Platform.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Platform.CustomControls
{
    /// <summary>
    /// Header.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class Header : UserControl
    {
        public Header()
        {
            InitializeComponent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Window parentWindow = Window.GetWindow(this);

            if (parentWindow != null)
            {
                if (parentWindow?.DataContext is ViewModel.MainViewModel vm)
                {
                    // TODO :: Free All Resources
                }

                parentWindow.Close();
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            Window parentWindow = Window.GetWindow(this);

            if (parentWindow != null)
            {
                parentWindow.WindowState = WindowState.Minimized;
            }
        }

        private void SetupButton_Click(object sender, RoutedEventArgs e)
        {
            Window parentWindow = Window.GetWindow(this);

            if (parentWindow != null)
            {
                if (parentWindow?.DataContext is ViewModel.MainViewModel vm)
                {
                    vm.setupViewModel.Init();

                    SetupWindow setupWindow = new SetupWindow();
                    setupWindow.DataContext = vm.setupViewModel;
                    setupWindow.Owner = Application.Current.MainWindow;
                    setupWindow.ShowDialog();
                }
            }
        }
    }
}
