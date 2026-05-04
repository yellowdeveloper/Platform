using Platform.Model;
using Platform.Services;
using Platform.ViewModel;
using PluginBase;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
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
    /// MainPanel.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainPanel : UserControl
    {
        public MainPanel()
        {
            InitializeComponent();
        }

        private void DoDrop(object sender, DragEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                var dragged = e.Data.GetData("ComponentFormat") as PluginComponent;

                vm.PluginDropped(dragged);
            }
        }
    }
}
