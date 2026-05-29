using PluginBase.CommonUtils;
using System.Windows;
using System.Windows.Controls;

namespace AnemiaPrediction;

public partial class PluginUI : UserControl
{
    public event EventHandler CloseRequested;

    public PluginUI()
    {
        InitializeComponent();

        LogoImage.Source = PluginResourceLoader.GetImage("NailLogo.png");
        DoksanNPUImage.Source = PluginResourceLoader.GetImage("DoksanNPU.png");
        CameraImage.Source = PluginResourceLoader.GetImage("Camera.png");
        PCBImage.Source = PluginResourceLoader.GetImage("PCB.png");
        UserImage.Source = PluginResourceLoader.GetImage("User.png");
    }
    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModel viewModel)
        {
            viewModel.GetDeviceNum();
        }

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
        DebugLogger.Log(3, $"[DEBUG] Disposing Object Detection UI Control Instance");
    }
}
