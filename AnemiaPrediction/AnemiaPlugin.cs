using PluginBase;
using System.Diagnostics;

namespace AnemiaPrediction;

public class AnemiaPlugin : IPlugin
{
    public string PluginName => "Anemia Prediction";

    private readonly ISerial _serialInterface;
    private readonly IUI _uiInterface;

    public AnemiaPlugin() {
        _serialInterface = new Serial();
        _uiInterface = new UI();
    }

    public ISerial GetSerialPlugins() => _serialInterface;
    public IUI GetUIPlugins() => _uiInterface;
}

public class Serial : ISerial
{
    public byte[] header { get; } = new byte[] { 0x10, 0x01, 0x10, 0x01 };
    public byte[] footer { get; } = new byte[] { 0x0D, 0x0A, 0x0D, 0x0A };

    public void ProcessReceivedBuffer ()
    {
        Debug.WriteLine($"\n{header[0]}\n{footer[0]}\n");
    }
}

public class UI : IUI
{
    public object GetPluginUI()
    {
        return new PluginUI();
    }
}
