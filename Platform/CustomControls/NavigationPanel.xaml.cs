using Platform.Model;
using Platform.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    /// NavigationPanel.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class NavigationPanel : UserControl
    {
        private Point dragPoint;
        private bool mouseInComponent;
        private bool isDragging;
        public NavigationPanel()
        {
            InitializeComponent();
        }
        private void CreateNewComponet(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is MainViewModel vm)
            {
                vm.CreateNewComponent(0);
                vm.IncreaseComponentNum();
            }
        }

        private void SwapComponents(object sender, DragEventArgs e) {
            if (this.DataContext is MainViewModel vm)
            {
                var dragged = e.Data.GetData("ComponentFormat") as PluginComponent;

                //int targetIndex = GetIndexFromPosition(e.GetPosition(ComponentStack));

                //vm.MoveItem(dragged, targetIndex);
            }
        }
    }
}
