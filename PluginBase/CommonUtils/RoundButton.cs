using System.Windows;
using System.Windows.Controls;

namespace PluginBase.CommonUtils
{
    public class RoundButton : Button
    {
        public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register(nameof(CornerRadius), typeof(CornerRadius), typeof(RoundButton));
        public CornerRadius CornerRadius {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }
    }
}
