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
    private IICommunication communicationInterface;

    // public ISerial GetSerialPlugins(ISerialService service)
    // {
    //     _serialInterface = new Serial(service);
    //     return _serialInterface;
    // }
    
    public IUI GetUIPlugins(ISerialService service)
    {
        var settings = new Settings();

        communicationInterface = new Communication(service, settings);
        uiInterface = new UI(communicationInterface, settings);
        
        return uiInterface;
    }
}

public class UI : IUI
{
    private readonly IICommunication communication;
    private readonly Settings settings;
    public event EventHandler CloseRequested;

    public UI(IICommunication _communication, Settings _settings)
    {
        communication = _communication;
        settings = _settings;
    }

    public object GetPluginUI()
    {
        var view = new PluginUI();
        var viewModel = new ViewModel(communication, settings);

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