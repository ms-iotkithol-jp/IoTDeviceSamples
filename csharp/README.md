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

After above command is executed, This application run as application described in [README.md in this repository](../README.md)

Received texts of command, desired properties and device method invocation from Azure IoT Hub are shown in console. 

## Feature of console inputs  
You can input commands to this application on the console. 
The console shows following text .
```shell
Please input command:
SHAKE:temp|PUT:[1-6]|SEND:message|UPLOAD:file-path|REPORTED:json-message|ROOM:temp|HUM:humidity|PHOTO:intervalSec|QUIT
```

you can input command from keyboard to invoke following features... 
- QUIT
    - order to terminate this application
- SHAKE:<i>temp</i>
    - order to change status of Thing from <b>PUT</b> to crazy <b>SHAKE</b>
    - <i>temp</i> part is the value for your hand temperature 
    - for example, your input is - "SHAKE:32.7", this means your hand is some cold. 
- PUT<i>:[1-6]</i>
    - order to change status of Thing from crazy <b>SHAKE</b> to <b>PUT</b> or change top face of Thing. 
    - ":[1-6]" part is optoinal
    - when you input - "PUT" , then the top side is <b>1</b>
    - when you want to specify which side shold be top, input with optional part 
    - "PUT:3" means that top side is <b>3</b> and available values are <b>1</b>, <b>2</b>, <b>3</b>, <b>4</b>, <b>5</b>, <b>6</b> because "Thing" is kind of dice. 
- SEND:<i>message</i>
    - order to send custom message to Azure IoT Hub.
    - <i>message</i> can be JSON format or normal text
    - When your input is - "SEND:Hello from simulator", then {"time":"2018...",message:"Hello from simulator"} will be send.
    - When your input text is started with '<b>{</b>' and ended with '<b>}</b>', then application recognize and serialize the text as JSON format. ex) in the case of <b>{"xyz":"hello","value":34}</b>, {"time":"2018...","message":{"xyz":"hello","value":34}} will be send.
- REPORTED:<i>json-message</i>
    - order to update Device Twin Reported Properties
    - the part of '<i>json-message</i>' is noticed straight
- ROOM:<i>temp</i>
    - order to change room temperature
    - <i>temp</i> part is the value for room temperature you want
- HUM:<i>humidity</i> 
    - order to change humidity around thing
    - <i>humidity</i> part is the value for humidity you want
- PHOTO:<i>intervalSec</i>
    - order to start taking picture by Webcam on PC and upload to IoT Hub at fixed time intervals
    - <i>intervalSec</i> part is the interval unit second you want.


## APPENDIX 
### Edit/build on Windows PC and publish assembly to Raspbian/Raspberry Pi 
When you want to edit or build this project for Raspberry Pi, please execute following command in integrated terminal in VS Code on Windows. 
```shell
dotnet publish -r linux-arm
```
By this command, publish folder and necessary assemblies are created under the bin/netcoreapp2.0/linux-arm. Please copy this fileset to Raspberry PI remotery then you can run it. 
```shell
cd publish/netcoreapp2.0/linux-arm
./csharp
```
