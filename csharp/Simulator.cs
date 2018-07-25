using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;

public class Simulator
{
    public string ConnectionString { get; set; }
    public int TelemetryCycleMSec { get; set; }
    public int UpdateIntervalMSec { get; set; }
    public double RoomTemperature { get; set; }

    public Thing thing { get; set; }

    public delegate void DesiredPropertiesUpdatedEventHandler(object sender, DesiredPropertiesUpdatedEventArgs e);
    public delegate void DeviceMethodInvokedEventHandler(object sender, DeviceMethodInvokedEventArgs e);
    public delegate void MessageReceivedEventHandler(object sender, MessageReceivedEventArgs e);

    public event DesiredPropertiesUpdatedEventHandler DesiredPropertiesUpdated;
    public event DeviceMethodInvokedEventHandler DeviceMethodInvoked;
    public event MessageReceivedEventHandler MessageReceived;

    DeviceClient deviceClient;

    public Simulator()
    {
        thing = new Thing();
    }

    public async Task Initialize()
    {
        deviceClient = DeviceClient.CreateFromConnectionString(ConnectionString, TransportType.Amqp);
        await deviceClient.SetDesiredPropertyUpdateCallbackAsync(DesiredPropUpdated, this);
        await deviceClient.SetMethodDefaultHandlerAsync(MethodInvoked, this);
        await deviceClient.OpenAsync();
        var twin = await deviceClient.GetTwinAsync();
        if (twin.Properties.Desired.Contains(updateIntervalCommand))
        {
            TelemetryCycleMSec = twin.Properties.Desired[updateIntervalCommand];
        }
        await ReceiveMessages();
    }

    public void Room(double temp)
    {
        lock (this)
        {
            RoomTemperature = temp;
            if (thing.Status == ThingStatus.STABLE && thing.TargetTemperature != RoomTemperature)
            {
                thing.TargetTemperature = RoomTemperature;
            }
        }
    }

    private async Task<MethodResponse> MethodInvoked(MethodRequest methodRequest, object userContext)
    {
        if (DeviceMethodInvoked != null)
        {
            DeviceMethodInvoked(deviceClient, new DeviceMethodInvokedEventArgs() { InvocaitonRequest = methodRequest });
        }
        var invocationResult = new
        {
            Status = "OK",
            InvokedTime = DateTime.Now
        };
        var invocationResultJson = Newtonsoft.Json.JsonConvert.SerializeObject(invocationResult);
        var response = new MethodResponse(System.Text.Encoding.UTF8.GetBytes(invocationResultJson), 200);
        return response;
    }

    string updateIntervalCommand = "telemetry-cycle-msec";

    private async Task DesiredPropUpdated(TwinCollection desiredProperties, object userContext)
    {
        if (DesiredPropertiesUpdated != null)
        {
            DesiredPropertiesUpdated(deviceClient, new DesiredPropertiesUpdatedEventArgs() { DesiredProperties = desiredProperties });
        }

        if (desiredProperties.Contains(updateIntervalCommand))
        {
            lock (this)
            {
                TelemetryCycleMSec = desiredProperties[updateIntervalCommand];
            }
        }
        var reportedProperties = new TwinCollection();
        reportedProperties["DateTimeLastDisredPropertyChangeReceived"] = DateTime.Now;

        await deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
    }

    public async Task SendEvent(string msg)
    {
        string jsonContent = "";
        if (msg.StartsWith("{") && msg.EndsWith("}"))
        {
            dynamic msgJson = Newtonsoft.Json.JsonConvert.DeserializeObject(msg);
            var packet = new
            {
                time = DateTime.Now,
                message = msgJson
            };
            jsonContent = Newtonsoft.Json.JsonConvert.SerializeObject(packet);
        }
        else
        {
            var packet = new
            {
                time = DateTime.Now,
                message = msg
            };
            jsonContent = Newtonsoft.Json.JsonConvert.SerializeObject(packet);
        }
        await deviceClient.SendEventAsync(new Message(System.Text.Encoding.UTF8.GetBytes(jsonContent)));
    }
    public async Task UpdateReportedProperties(string twinJson)
    {
        var reportedProps = new TwinCollection(twinJson);
        await deviceClient.UpdateReportedPropertiesAsync(reportedProps);
    }

    private async Task ReceiveMessages()
    {
        await Task.Factory.StartNew(async () =>
        {
            while (true)
            {
                var msg = await deviceClient.ReceiveAsync();
                if (msg != null)
                {
                    if (MessageReceived != null)
                    {
                        MessageReceived(deviceClient, new MessageReceivedEventArgs() { ReceivedMessage = msg });
                    }
                    await deviceClient.CompleteAsync(msg);
                }
                lock (this)
                {
                    if (!toContinue)
                    {
                        break;
                    }
                }

            }

        });
    }

    public async Task UploadFile(string blobName, string filePath)
    {
        using (var fs = File.Open(filePath, FileMode.Open))
        {
            await deviceClient.UploadToBlobAsync(blobName, fs);
        }
    }

    public bool toContinue;

    public async Task Start()
    {
        thing.TargetTemperature = RoomTemperature;
        thing.Initialize(UpdateIntervalMSec);
        toContinue = true;
        await Task.Factory.StartNew(async () =>
        {
            while (true)
            {
                var sr = thing.Read();
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(sr);
                var msg = new Message(System.Text.Encoding.UTF8.GetBytes(json));
                await deviceClient.SendEventAsync(msg);
                lock (this)
                {
                    if (!toContinue)
                    {
                        break;
                    }
                }
                int interval = 0;
                lock (this)
                {
                    interval = TelemetryCycleMSec;
                }
                await Task.Delay(interval);
            }
        });
    }
    public async Task Stop()
    {
        thing.Terminate();
    }

    public async Task Terminate()
    {
        await deviceClient.CloseAsync();
    }


    public class DesiredPropertiesUpdatedEventArgs : EventArgs
    {
        public TwinCollection DesiredProperties { get; set; }
    }
    public class DeviceMethodInvokedEventArgs : EventArgs
    {
        public MethodRequest InvocaitonRequest { get; set; }
    }
    public class MessageReceivedEventArgs : EventArgs
    {
        public Message ReceivedMessage { get; set; }
    }
}
