using System;
using System.IO;
using System.Threading.Tasks;

namespace csharp {
    class Program {
        public static string connectionString = "";
        public static int updateLoopMSec = 500;
        public static int sendLoopMSec = 1000;
        public Simulator simulator;
        static void Main (string[] args) {
            string configFileName = "config.json";
            if (args.Length == 2) {
                if (args[0] == "--config") {
                    configFileName = args[1];
                }
            }
            var p = new Program ();
            p.DoWork (configFileName).Wait ();
        }

        void LoadConfig (string fileName) {
            using (var fs = File.Open (fileName, FileMode.Open)) {
                var buf = new byte[fs.Length];
                fs.Read (buf, 0, (int) fs.Length);
                var content = System.Text.Encoding.UTF8.GetString (buf);
                dynamic json = Newtonsoft.Json.JsonConvert.DeserializeObject (content);
                dynamic simConfig = json.SelectToken ("simulator");
                dynamic thingConfig = json.SelectToken ("thing");
                simulator.ConnectionString = simConfig.SelectToken ("connection-string");
                simulator.RoomTemperature = simConfig.SelectToken ("room-temperature");
                simulator.RoomHumidity = simConfig.SelectToken ("room-humidity");
                simulator.TelemetryCycleMSec = simConfig.SelectToken ("telemetry-cycle-msec");
                simulator.UpdateIntervalMSec = simConfig.SelectToken ("update-interval-msec");

                simulator.thing.FaceTop = faceList[thingConfig.SelectToken ("face-top") - 1];
                simulator.thing.MaxAccelValue = thingConfig.SelectToken ("max-accel-value");
                simulator.thing.AccelWhiteNoiseRate = thingConfig.SelectToken ("accelerometer-white-noise-rate");
                simulator.thing.TempWhiteNoiseRate = thingConfig.SelectToken ("temperature-white-noise-rate");
                simulator.thing.TempDeltaCoef = thingConfig.SelectToken ("temperature-delta-coef");
                simulator.thing.HumDeltaRound = thingConfig.SelectToken ("humidity-delta-round");
            }

        }
        async Task DoWork (string configFileName) {
            Console.WriteLine ("######################################################");
            Console.WriteLine ("# IoT Device Simulator by C# on .NET CORE");
            Console.WriteLine ("######################################################");
            Console.WriteLine ("");

            simulator = new Simulator () {
                ConnectionString = connectionString
            };

            LoadConfig (configFileName);

            await simulator.Initialize ();
            Console.WriteLine ("Connected to IoT Hub and simulation started");

            simulator.DesiredPropertiesUpdated += OnDesiredPropertiesUpdated;
            simulator.DeviceMethodInvoked += OnDeviceMethodInvoked;
            simulator.MessageReceived += OnMessageReceived;
            simulator.Start ().Wait ();

            Console.WriteLine ("You can change telemetry cycle via Desired Property - 'telemetry-cycle-msec'");
            Console.WriteLine ("");

            while (true) {
                Console.WriteLine ("Please input command:");
                Console.WriteLine ("SHAKE:temp|PUT:[1-6]|SEND:message|UPLOAD:file-path|REPORTED:json-message|ROOM:temperature|HUM:humidity|PHOTO:intervalSec|QUIT");
                string command = Console.ReadLine ();
                if (command.ToLower ().StartsWith ("quit")) {
                    break;
                }
                await ParseCommand (command);
            }

            await simulator.Stop ();
            await simulator.Terminate ();
        }

        ThingFace[] faceList = { ThingFace.F1, ThingFace.F2, ThingFace.F3, ThingFace.F4, ThingFace.F5, ThingFace.F6 };
        private async Task ParseCommand (string command) {
            if (command.ToLower ().StartsWith ("shake")) {
                var orderShake = command.Split (":");
                double fingerTemp = double.Parse (orderShake[1]);
                simulator.thing.Shake (fingerTemp);
                Console.WriteLine ("Shaking...");
            } else if (command.ToLower ().StartsWith ("put")) {
                if (command.IndexOf (":") > 0) {
                    var orderPut = command.Split (":");
                    var top = int.Parse (orderPut[1]);
                    if (top < 1 || top > faceList.Length) {
                        Console.WriteLine ("PUT's arg should be 1-6");
                    }
                    simulator.thing.Put (simulator.RoomTemperature, faceList[top - 1]);
                } else {
                    simulator.thing.Put (simulator.RoomTemperature);
                }
                Console.WriteLine ("Put on thing");
            } else if (command.ToLower ().StartsWith ("upload")) {
                var orderUploadFile = command.Substring (command.IndexOf (":") + 1);
                var fi = new FileInfo (orderUploadFile);
                await simulator.UploadFile (fi.Name, fi.FullName);
                Console.WriteLine ("Uploaded {0} as {1}", fi.FullName, fi.Name);
            } else if (command.ToLower ().StartsWith ("reported")) {
                var twinJson = command.Substring (command.IndexOf (":") + 1);
                await simulator.UpdateReportedProperties (twinJson);
                Console.WriteLine ("Reported via Reported Properties - {0}", twinJson);
            } else if (command.ToLower ().StartsWith ("send")) {
                var msg = command.Substring (command.IndexOf (":") + 1);
                try {
                    await simulator.SendEvent (msg);
                    Console.WriteLine ("Send done - '{0}'", msg);
                } catch (Exception ex) {
                    Console.WriteLine ("Error - {0}", ex.Message);
                }
            } else if (command.ToLower ().StartsWith ("room")) {
                var orderRoom = command.Split (":");
                var newTemp = double.Parse (orderRoom[1]);
                simulator.Room (newTemp);
                Console.WriteLine ("Room Temperature Updated by {0}", newTemp);
            } else if (command.ToLower ().StartsWith ("hum")) {
                var orderHum = command.Split (":");
                var newHum = double.Parse (orderHum[1]);
                simulator.Humidity (newHum);
                Console.WriteLine ("Humidity updated by {0}", newHum);
            } else if (command.ToLower ().StartsWith ("photo")) {
                var orderPhoto = command.Split (":");
                var intervalPhoto = int.Parse (orderPhoto[1]);
                if (simulatorPhotoTakingTask != null) {
                    if (intervalPhoto > 0) {
                        simulator.ChangeTakingPhotoInterval (intervalPhoto * 1000);
                    } else {
                        simulator.EndTakePicture (simulatorPhotoTakingTask, photoCancellingTokenSource);
                        simulatorPhotoTakingTask = null;
                    }
                } else {
                    photoCancellingTokenSource = new System.Threading.CancellationTokenSource ();
                    simulatorPhotoTakingTask = simulator.StartTakePicture (intervalPhoto * 1000, photoCancellingTokenSource);
                }
            }
        }
        private Task simulatorPhotoTakingTask;
        private System.Threading.CancellationTokenSource photoCancellingTokenSource;

        private void OnMessageReceived (object sender, Simulator.MessageReceivedEventArgs e) {
            Console.WriteLine ("[IoT Hub] Message Received - ");
            if (e.ReceivedMessage.Properties.Keys.Count > 0) {
                Console.WriteLine (" Properties:");
                foreach (var k in e.ReceivedMessage.Properties.Keys) {
                    var v = e.ReceivedMessage.Properties[k];
                    Console.WriteLine ("  {0}:{1}", k, v);
                }
            }
            Console.WriteLine (System.Text.Encoding.UTF8.GetString (e.ReceivedMessage.GetBytes ()));
        }

        private void OnDeviceMethodInvoked (object sender, Simulator.DeviceMethodInvokedEventArgs e) {
            Console.WriteLine ("[IoT Hub] Method Invoked - {0}({1})", e.InvocaitonRequest.Name, e.InvocaitonRequest.DataAsJson);
        }

        private void OnDesiredPropertiesUpdated (object sender, Simulator.DesiredPropertiesUpdatedEventArgs e) {
            Console.WriteLine ("[IoT Hub] Desired Properties Updated - {0}", e.DesiredProperties.ToJson ());
        }
    }
}