# IoT Device Simulator by C# on .NET CORE 
## Preparation 
Install .NET Core - please refer [.NET Core Home page](https://www.microsoft.com/net/learn/get-started/windows) .  
Create Azure IoT Hub and regist one device for this simulator - [document](https://docs.microsoft.com/azure/iot-hub/quickstart-send-telemetry-dotnet#create-an-iot-hub) 
Edit connection-string's value in config.json by connection string for registed device. 

## Command Line 
Windows 
> \>cd IoTDeviceSimulator\csharp  
> \>dotnet run [--config <i>config file name</i>]  

Linux  
> $ cd IoTDeviceSimulator/csharp  
> $ dotnet run [--config <i>config file name</i>]  

This application use config.json as several settings. When you want to use other configuration, specify config file by --config option. 

After above command is executed, This application load configuration from ./config.json and start to acceleration and temperature of "thing" at the time interval specified by 'telemetry-cycle-msec' in config.json.
You can change this interval via Device Twin Desired Property as...  
{"telemetry-cycle-msec":500}  
Default interval is 1000 msec. 
