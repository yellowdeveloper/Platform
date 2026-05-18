using Platform.Model;
using Platform.Services;
using Platform.Utils;
using Platform.ViewModel;
using PluginBase.CommonUtils;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Platform.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            DebugLogger.LoggerInit(3);

            var mainViewModel = new MainViewModel();

            this.DataContext = mainViewModel;
        }

        private void HeaderView_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (IsDescendantOfButton(e.OriginalSource as DependencyObject))
            {
                return;
            }
            if (e.ClickCount == 2)
            {
                if (this.WindowState == WindowState.Normal)
                {
                    // this.WindowState = WindowState.Maximized;
                }
                else
                {
                    this.WindowState = WindowState.Normal;
                }

                e.Handled = true;
                return;
            }

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private bool IsDescendantOfButton(DependencyObject element)
        {
            while (element != null)
            {
                if (element is Button || element is ToggleButton)
                {
                    return true;
                }
                // 상위 요소로 이동
                element = VisualTreeHelper.GetParent(element);
            }
            return false;
        }
    }
}