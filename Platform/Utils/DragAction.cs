using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Platform.Utils
{
    public static class DragAction
    {
        private static Point _dragPoint;

        public static readonly DependencyProperty EnableDeagProperty =
            DependencyProperty.RegisterAttached(
                "EnableDrag",
                typeof(bool),
                typeof(DragAction),
                new PropertyMetadata(false, OnDragMove));

        public static void SetEnableDrag(UIElement element, bool value)
            => element.SetValue(EnableDeagProperty, value);

        public static void GetEnableDrag(UIElement element)
            => element.GetValue(EnableDeagProperty);

        public static void OnDragMove(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UIElement element && (bool)e.NewValue)
            {
                element.PreviewMouseLeftButtonDown += (s, ev) =>
                {
                    _dragPoint = ev.GetPosition(null);
                };

                element.PreviewMouseMove += (s, ev) =>
                {
                    if (ev.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
                    {
                        var pos = ev.GetPosition(null);
                        var diff = _dragPoint - pos;

                        if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                            Math.Abs(diff.X) > SystemParameters.MinimumVerticalDragDistance)
                        {
                            var data = ((FrameworkElement)s).DataContext;

                            DragDrop.DoDragDrop((DependencyObject)s,
                                new DataObject("ComponentFormat", data),
                                DragDropEffects.Move);
                        }
                    }
                };
            }
        }
    }
}
