using PluginBase;
using System.Windows.Input;
using System.Diagnostics;
using PluginBase.CommonUtils;
using System.Runtime.InteropServices;
using OpenCvSharp;

namespace IRDetection;

public enum ESendStatus
{
    NotReady,
    Idle,
    Sending,
    WaitingForResponse
} 

public class IRPlugin : IPlugin
{
    public string PluginName => "Infrared Detection";

    private IUI uiInterface;
    private IISerialDeviceHandler serialInterface;
    private IISPIDeviceHandler spiInterface;

    // public ISerial GetSerialPlugins(ISerialService service)
    // {
    //     _serialInterface = new Serial(service);
    //     return _serialInterface;
    // }

    public IUI GetUIPlugins(ISerialService serialService, ISPIService spiService)
    {
        var settings = new Settings();

        serialInterface = new SerialDeviceHandler(serialService, settings);
        spiInterface = new SPIDeviceHandler(spiService, settings);
        uiInterface = new UI(serialInterface, spiInterface, settings);
        
        return uiInterface;
    }
}

public class UI : IUI, IDisposable
{
    private readonly IISerialDeviceHandler serialDeviceHandler;
    private readonly IISPIDeviceHandler spiDeviceHandler;

    private readonly Settings settings;
    public event EventHandler CloseRequested;

    private ViewModel viewModel;

    public UI(IISerialDeviceHandler _serialDeviceHandler, IISPIDeviceHandler _spiDeviceHandler,  Settings _settings)
    {
        serialDeviceHandler = _serialDeviceHandler;
        spiDeviceHandler = _spiDeviceHandler;
        settings = _settings;
    }

    public object GetPluginUI()
    {
        var view = new PluginUI();

        viewModel = new ViewModel(serialDeviceHandler, spiDeviceHandler, settings);
        view.DataContext = viewModel;

        view.CloseRequested += (sender, e) =>
        {
            this.CloseRequested?.Invoke(this, EventArgs.Empty);
        };

        return view;
    }

    public void Dispose()
    {
        viewModel?.Dispose();
        DebugLogger.Log(3, $"[DEBUG] Disposing IRDetection UI & ViewModel");
    }

    ~UI()
    {
        DebugLogger.Log(3, $"[DEBUG] Disposing IRDetection UI Instance");
    }
}