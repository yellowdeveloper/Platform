using PluginBase.CommonUtils;

namespace PluginBase;

// 여기부터 플러그인에서 정의할 인터페이스 (메인에서 사용)
public interface IPlugin
{
    string PluginName { get; }

    //ISerial GetSerialPlugins(ISerialService service);
    IUI GetUIPlugins(ISerialService serialService, ISPIService spiService);
}

public interface ISerialDeviceHandler
{
    byte[] header { get; }
    byte[] footer { get; }

    void SerialConnect(int id);
    void SerialDisconnect();
    void StackReceivedBuffer(byte[] dataBytes, int actuallyRead);
    void FindValidData();
    void ParseReceivedData();
}

public interface ISPIDeviceHandler
{
    void SPIConnect(int id);
    void SPIDisconnect();
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
    void SetDeviceHandler(ISerialDeviceHandler _serialHandler);
    void AttachSerialEventHandler();
    void DetachSerialEventHandler();
}

public interface ISerialService
{
    ISerialDevice GetDevice(int id);
    int[] GetDeviceList();
}

public interface ISPIDevice
{
    void SetDeviceHandler(ISPIDeviceHandler _serialHandler);
    void AttachSPIEventHandler();
    void DetachSPIEventHandler();
    void Send(SPIPacket packet, int _chunkSize);

}

public interface  ISPIService
{
    ISPIDevice GetDevice(int id);
    int[] GetDeviceList();
}