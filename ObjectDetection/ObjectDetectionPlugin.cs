using PluginBase;
using PluginBase.CommonUtils;

namespace ObjectDetection;

public enum ESendStatus
{
    NotReady,
    Idle,
    Sending,
    WaitingForResponse
}

public class ObjectDetection : IPlugin
{
    public string PluginName => "Object Detection";

    private IUI uiInterface;
    private IISerialDeviceHandler serialInterface;
    private IISPIDeviceHandler spiInterface;

    public IUI GetUIPlugins(ISerialService serialService, ISPIService spiService)
    {
        var settings = new Settings();

        serialInterface = new SerialDeviceHandler(serialService, settings);
        spiInterface = new SPIDeviceHandler(spiService, settings);
        uiInterface = new UI(serialInterface, spiInterface, settings);

        return uiInterface;
    }
}

public class UI : IUI
{
    private readonly IISerialDeviceHandler serialDeviceHandler;
    private readonly IISPIDeviceHandler spiDeviceHandler;

    private readonly Settings settings;
    public event EventHandler CloseRequested;

    public UI(IISerialDeviceHandler _serialDeviceHandler, IISPIDeviceHandler _spiDeviceHandler, Settings _settings)
    {
        serialDeviceHandler = _serialDeviceHandler;
        spiDeviceHandler = _spiDeviceHandler;
        settings = _settings;
    }

    public object GetPluginUI()
    {
        var view = new PluginUI();
        var viewModel = new ViewModel(serialDeviceHandler, spiDeviceHandler, settings);

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
        DebugLogger.Log(3, $"[DEBUG] Disposing ObjectDetection UI Instance");
    }
}
