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
            TextBlock tb = new TextBlock()
            {
                Text = "ㅇ New Component",
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 5, 0, 5)
            };

            Border overlay = new Border
            {
                CornerRadius = new CornerRadius(5),
                Style = (Style)FindResource("ComponentBorder"),
            };

            Grid component = new Grid
            {
                Width = ComponentStack.ActualWidth,
                Height = 30,
                Margin = new Thickness(0, 5, 0, 5),
                Cursor = Cursors.Hand,
            };
            component.Children.Add(tb);
            component.Children.Add(overlay); 

            ComponentStack.Children.Add(component);

            component.PreviewMouseLeftButtonDown += Component_PreviewMouseLeftButtonDown;
            component.PreviewMouseMove += Component_PreviewMouseMove;

            if (this.DataContext is MainViewModel vm)
            {
                vm.IncreaseComponentNum();
            }
        }

        private void Component_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            dragPoint = e.GetPosition(null);
            mouseInComponent = true;
            Debug.WriteLine($"Now Mouse Point is :: {dragPoint}");
        }

        private void Component_PreviewMouseMove(object sender, MouseEventArgs e) {
            if (e.LeftButton == MouseButtonState.Pressed && mouseInComponent) {
                Point mousePoint = e.GetPosition(null);
                Vector moveDist = dragPoint - mousePoint;

                if (Math.Abs(moveDist.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(moveDist.Y) > SystemParameters.MinimumVerticalDragDistance) {
                    isDragging = true;

                    Grid src = sender as Grid;

                    DataObject dragData = new DataObject("ComponentFormat", src);

                    Debug.WriteLine($"Mouse Point Update :: {mousePoint}");

                    DragDrop.DoDragDrop(src, dragData, DragDropEffects.Move);

                    mouseInComponent = false;
                    isDragging = false;
                }
            }
        }

        private void SwapComponents(object sender, DragEventArgs e) {

        }
    }
}
