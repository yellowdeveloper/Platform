namespace PluginBase;

// 여기부터 플러그인에서 정의할 인터페이스 (메인에서 사용)
public interface IPlugin
{
    string PluginName { get; }

    //ISerial GetSerialPlugins(ISerialService service);
    IUI GetUIPlugins(ISerialService service);
}

public interface ISerial
{
    byte[] header { get; }
    byte[] footer { get; }
    void PluginConnect(int id);
    void PluginDisconnect();
    void StackReceivedBuffer(byte[] dataBytes, int actuallyRead);
    void FindValidData();
    void ParseReceivedData();
}

public interface IUI
{
    object GetPluginUI();
    event EventHandler CloseRequested;
}

public interface IAdd_Ons
{

}


// 여기부터 메인에서 정의할 인터페이스 (플러그인에서 사용)
public interface ISerialDevice
{
    public void SetDeviceHandler(ISerial _serialHandler);
    public void AttachSerialEventHandler();
    public void DetachSerialEventHandler();
}

public interface ISerialService
{
    public ISerialDevice GetDevice(int id);
}