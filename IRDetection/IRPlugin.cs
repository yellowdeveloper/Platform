using PluginBase;
using System.Windows.Input;
using System.Diagnostics;
using PluginBase.CommonUtils;
using System.Runtime.InteropServices;
using OpenCvSharp;

namespace IRDetection;

public class IRPlugin : IPlugin
{
    public string PluginName => "Anemia Prediction";

    private IUI uiInterface;
    private IISerial serialInterface;

    // public ISerial GetSerialPlugins(ISerialService service)
    // {
    //     _serialInterface = new Serial(service);
    //     return _serialInterface;
    // }
    
    public IUI GetUIPlugins(ISerialService service)
    {
        var settings = new Settings();

        serialInterface = new Serial(service, settings);
        uiInterface = new UI(serialInterface, settings);
        
        return uiInterface;
    }
}

public class UI : IUI
{
    private readonly IISerial serial;
    private readonly Settings settings;
    public event EventHandler CloseRequested;

    public UI(IISerial _serial, Settings _settings)
    {
        serial = _serial;
        settings = _settings;
    }

    public object GetPluginUI()
    {
        var view = new PluginUI();
        var viewModel = new ViewModel(serial, settings);

        view.DataContext = viewModel;

        view.CloseRequested += (sender, e) =>
        {
            viewModel?.Dispose();
            this.CloseRequested?.Invoke(this, EventArgs.Empty);
        };

        return view;
    }

    ~UI()
    {
        DebugLogger.Log(3, $"[DEBUG] Disposing IRDetection UI Instance");
    }
}