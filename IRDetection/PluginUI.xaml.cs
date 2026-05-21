using PluginBase.CommonUtils;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace IRDetection;

public partial class PluginUI : UserControl
{
    public event EventHandler CloseRequested;

    public PluginUI()
    {
        InitializeComponent();

        LogoImage.Source = PluginResourceLoader.GetImage("IRLogo.png");
        DoksanNPUImage.Source = PluginResourceLoader.GetImage("DoksanNPU.png");
        IRSensorImage.Source = PluginResourceLoader.GetImage("IRSensor.png");
        PCBImage.Source = PluginResourceLoader.GetImage("PCB.png");
        UserImage.Source = PluginResourceLoader.GetImage("User.png");
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        if (SettingsPanel.Visibility == Visibility.Visible)
        {
            SettingsPanel.Visibility = Visibility.Collapsed;
        }
        else
        {
            SettingsPanel.Visibility = Visibility.Visible;
        }
    }

    ~PluginUI()
    {
        DebugLogger.Log(3, $"[DEBUG] Disposing IRDetection UI Control Instance");
    }
}
