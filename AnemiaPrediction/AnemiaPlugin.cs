using PluginBase;
using System.Windows.Input;
using System.Diagnostics;
using PluginBase.CommonUtils;

namespace AnemiaPrediction;

public class AnemiaPlugin : IPlugin
{
    public string PluginName => "Anemia Prediction";

    private IUI _uiInterface;
    private ISerial _serialInterface;

    // public ISerial GetSerialPlugins(ISerialService service)
    // {
    //     _serialInterface = new Serial(service);
    //     return _serialInterface;
    // }
    
    public IUI GetUIPlugins(ISerialService service)
    {
        _serialInterface = new Serial(service);
        _uiInterface = new UI(_serialInterface);
        return _uiInterface;
    }
}

public class Serial : ISerial
{
    private ISerialDevice device;
    private ISerialService service;

    public Serial(ISerialService _service)
    {
        service = _service;
    }

    public byte[] header { get; } = new byte[] { 0x10, 0x01, 0x10, 0x01 };
    public byte[] footer { get; } = new byte[] { 0x0D, 0x0A, 0x0D, 0x0A };

    public void PluginConnect(int id)
    {
        device = service.GetDevice(id);
        if (device == null)
        {
            DebugLogger.Log(1, $"[ERROR] Device doesn't exists.");
            return;
        }
        DebugLogger.Log(3, $"[DEBUG] Device No.{id} Plugin Connection Operated ...");

        device.SetDeviceHandler(this);
        device.AttachSerialEventHandler();
        
    }

    public void PluginDisconnect()
    {
        if (device == null) return;

        device.SetDeviceHandler(null);
        device.DetachSerialEventHandler();
    }

    public void ProcessReceivedBuffer (byte[] dataByte)
    {
        DebugLogger.Log(3, $"[DEBUG] HEDAER, FOOTER : 0x{header[0]:X2} 0x{header[1]:X2} | 0x{footer[0]:X2} 0x{footer[1]:X2}");
    }
}

public class UI : IUI
{
    private readonly ISerial serial;

    public UI(ISerial _serial)
    {
        serial = _serial;
    }

    public object GetPluginUI()
    {
        var view = new PluginUI();
        var viewModel = new ViewModel(serial);

        view.DataContext = viewModel;

        return view;
    }
}

internal class ViewModel
{
    private readonly ISerial serial;

    public ICommand ConnectCommand { get; }

    public ViewModel(ISerial _serial)
    {
        serial = _serial;
        
        ConnectCommand = new RelayCommand(param => 
        {
            serial.PluginConnect(1);
        });
    }
}
