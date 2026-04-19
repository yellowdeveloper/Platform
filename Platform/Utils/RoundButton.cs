using System.Windows;
using System.Windows.Controls;

namespace Platform.Utils
{
    class RoundButton : Button
    {
        public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register(nameof(CornerRadius), typeof(CornerRadius), typeof(RoundButton));
        public CornerRadius CornerRadius {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }
    }
}
