using PluginBase.CommonUtils;
using System.Windows;
using System.Windows.Controls;

namespace IRDetection;

public partial class PluginUI : UserControl
{
    public event EventHandler CloseRequested;

    public PluginUI()
    {
        InitializeComponent();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    ~PluginUI()
    {
        DebugLogger.Log(3, $"[DEBUG] Disposing IRDetection UI Control Instance");
    }
}
