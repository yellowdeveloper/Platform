namespace PluginBase;

public interface IPlugin
{
    string PluginName { get; }

    ISerial GetSerialPlugins();
    IUI GetUIPlugins();
}

public interface ISerial
{
    byte[] header { get; }
    byte[] footer { get; }
    void ProcessReceivedBuffer();
}

public interface IUI
{
    object GetPluginUI();
}