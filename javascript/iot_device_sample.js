// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

'use strict';

require('date-utils');

var Protocol = require('azure-iot-device-mqtt').Mqtt;
var Client = require('azure-iot-device').Client;
var ConnectionString = require('azure-iot-device').ConnectionString;
var Message = require('azure-iot-device').Message;

// String containing Hostname, Device Id & Device Key in the following formats:
//  "HostName=<iothub_host_name>;DeviceId=<device_id>;SharedAccessKey=<device_key>"
var connectionString = '[IoT Hub device connection string]';
var deviceId = ConnectionString.parse(connectionString).DeviceId;

// Sensors data
var temperature = 50;
var humidity = 50;
var externalTemperature = 55;

// Create IoT Hub client
var client = Client.fromConnectionString(connectionString, Protocol);

// Helper function to print results for an operation
function printErrorFor(op) {
    return function printError(err) {
        if (err) console.log(op + ' error: ' + err.toString());
    };
}

// Helper function to generate random number between min and max
function generateRandomIncrement() {
    return ((Math.random() * 2) - 1);
}

// Send device meta data
var deviceMetaData = {
    'ObjectType': 'DeviceInfo',
    'IsSimulatedDevice': 0,
    'Version': '1.0',
    'DeviceProperties': {
        'DeviceID': deviceId,
        'HubEnabledState': 1,
        'CreatedTime': '2015-09-21T20:28:55.5448990Z',
        'DeviceState': 'normal',
        'UpdatedTime': null,
        'Manufacturer': 'Contoso Inc.',
        'ModelNumber': 'MD-909',
        'SerialNumber': 'SER9090',
        'FirmwareVersion': '1.10',
        'Platform': 'node.js',
        'Processor': 'ARM',
        'InstalledRAM': '64 MB',
        'Latitude': 47.617025,
        'Longitude': -122.191285
    },
    'Commands': [{
        'Name': 'SetTemperature',
        'Parameters': [{
            'Name': 'Temperature',
            'Type': 'double'
        }]
    },
    {
        'Name': 'SetHumidity',
        'Parameters': [{
            'Name': 'Humidity',
            'Type': 'double'
        }]
    }]
};

function onGetDeviceLog(request, response) {
    printDeviceMethodRequest(request);

    // Implement actual logic here.

    // complete the response
    response.send(200, 'example payload', function (err) {
        if (!!err) {
            console.error('An error ocurred when sending a method response:\n' +
                err.toString());
        } else {
            console.log('Response to method \'' + request.methodName +
                '\' sent successfully.');
        }
    });
}

function onLockDoor(request, response) {
    printDeviceMethodRequest(request);

    // Implement actual logic here.

    // complete the response
    response.send(200, function (err) {
        if (!!err) {
            console.error('An error ocurred when sending a method response:\n' +
                err.toString());
        } else {
            console.log('Response to method \'' + request.methodName +
                '\' sent successfully.');
        }
    });
}

function onReboot(request, response) {
    response.send(200, 'Reboot started', function (err) {
        if (err) {
            console.error('An error occured when sending a method response:\n' + err.toString());
        } else {
            console.log('Response to method \'' + request.methodName + '\' sent successfully.');
        }
    });

    // Get device Twin
    client.getTwin(function (err, twin) {
        if (err) {
            console.error('could not get twin');
        } else {
            console.log('twin acquired');

            // Update the reported properties for this device through the 
            // twin.  This enables the back end app to query for all device that
            // have completed a reboot based on the lastReboot property.
            twin.properties.reported.update({
                iothubDM: {
                    reboot: {
                        startedRebootTime: new Date().toISOString(),
                    }
                }
            }, function (err) {
                if (err) console.error('Error updating twin');
                else console.log('Device reboot twin state reported')
            });
        }
    });
}

function printDeviceMethodRequest(request) {
    // print method name
    console.log('Received method call for method \'' + request.methodName + '\'');

    // if there's a payload just do a default console log on it
    if (!!(request.payload)) {
        console.log('Payload:\n' + request.payload);
    }
}

client.open(function (err) {
    if (err) {
        printErrorFor('open')(err);
    } else {
        console.log('Sending device metadata:\n' + JSON.stringify(deviceMetaData));
        client.sendEvent(new Message(JSON.stringify(deviceMetaData)), printErrorFor('send metadata'));

        // Create device Twin
        client.getTwin(function (err, twin) {
            if (err) {
                console.error('could not get twin');
            } else {
                console.log('twin created');

                // First, set up code to handle desired property changes.

                // Important note: when the twin is done being created, the desired
                // properties have already been retrieved from the service.  When we
                // add these handlers, there is a chance that the handlers will be called
                // almost immediately if the relevant properties have already been
                // retrieved from the service.  In this way "property change" could mean
                // "the property is changing from one value to another value", or it could
                // mean "the property is changing from being unset to being set."
                //
                // There are 4 examples here.  The app developer has the option to chose
                // which sample style to use (or mix and match).  All of the events
                // shown here will fire and it's up to the app developer to decide what
                // to listen for.
                //
                // If there is a collision between different handlers, it is up to the
                // app developer to resolve the collisions.  In other words, there is
                // nothing that stops the developer from writing two different handlers
                // that process the same properties at different levels.
                //

                // Usage example #1: receiving all patches with a single event handler.
                //
                // This code will output any properties that are received from the
                // service.
                //
                twin.on('properties.desired', function (delta) {
                    console.log('new desired properties received:');
                    console.log(JSON.stringify(delta));
                });

                // Usage example #2: receiving an event if anything under
                // properties.desired.climate changes
                //
                // This code will output desired min and max temperature every time
                // the service updates either one.
                //
                // For example (service API code):
                //  twin.properties.desired.update({
                //    climate : {
                //      minTemperature: 68,
                //      maxTemperature: 76
                //    }
                //  });
                //
                twin.on('properties.desired.climate', function (delta) {
                    //
                    // Notice that twin.properties.desired has already been updated before
                    // this function was called.
                    //
                    // If the delta contains a minTemp or a maxTemp, then update the
                    // hardware with the values stored in the twin.  The twin has all the
                    // same values as the delta, but the delta only has values that
                    // have changed.  When we need the "current values", we don't use the
                    // values stored in the delta because it's possible that the delta has
                    // one or the other, but not both.
                    //
                    if (delta.minTemperature || delta.maxTemperature) {
                        console.log('updating desired temp:');
                        console.log('min temp = ' + twin.properties.desired.climate.minTemperature);
                        console.log('max temp = ' + twin.properties.desired.climate.maxTemperature);
                    }
                });
                // Usage example #3: receiving an event for a single (scalar) property
                // value.  This event is only fired if the fanOn boolean value is part
                // of the patch.
                //
                // This code will output the new desired fan state whenever the service
                // updates it.
                //
                // For example (service API code):
                // twin.properties.desired.update({
                //   climate : {
                //      hvac : {
                //        systemControl : {
                //          fanOn : true
                //        }
                //      }
                //    }
                //  });
                twin.on('properties.desired.climate.hvac.sytemControl', function (fanOn) {
                    console.log('setting fan state to ' + fanOn);
                });

                // Usage example #4: handle add or delete operations.  The app developer
                // is responsible for inferring add/update/delete operations based on
                // the contents of the patch.
                //
                // This code will output the results of adding, updating, or deleting
                // modules.
                //
                // Add example (service API code):
                //  twin.properties.desired.update({
                //    modules : {
                //      wifi : { channel = 6, ssid = 'my_network' },
                //      climate : { id = 17, units = 'farenheit' }
                //    }
                //  });
                //
                // Update example (service API code):
                //  twin.properties.desired.update({
                //    modules : {
                //      wifi : { channel = 7, encryption = 'wpa', passphrase = 'foo' }
                //    }
                //  });
                //
                // Delete example (service API code):
                //  twin.properties.desired.update({
                //    modules : {
                //      climate = null
                //    }
                //  });
                //

                // To do this, first we have to keep track of "all modules that we know
                // about".
                var moduleList = {};

                // Then we use this internal list and compare it to the delta to know
                // if anything was added, removed, or updated.
                twin.on('properties.desired.modules', function (delta) {
                    Object.keys(delta).forEach(function (key) {

                        if (delta[key] === null && moduleList[key]) {
                            // If our patch contains a null value, but we have a record of
                            // this module, then this is a delete operation.
                            console.log('deleting module ' + key);
                            delete moduleList[key];

                        } else if (delta[key]) {
                            if (moduleList[key]) {
                                // Our patch contains a module, and we've seen this before.
                                // Must be an update operation.
                                console.log('updating module ' + key + ':');
                                console.log(JSON.stringify(delta[key]));
                                // Store the complete object instead of just the delta
                                moduleList[key] = twin.properties.desired.modules[key];

                            } else {
                                // Our patch contains a module, but we've never seen this
                                // before.  Must be an add operation.
                                console.log('adding module ' + key + ':');
                                console.log(JSON.stringify(delta[key]));
                                // Store the complete object instead of just the delta
                                moduleList[key] = twin.properties.desired.modules[key];
                            }
                        }
                    });
                });

                // create a patch to send to the hub
                var patch = {
                    firmwareVersion: '1.2.1',
                    weather: {
                        temperature: 72,
                        humidity: 17
                    }
                };

                // send the patch
                twin.properties.reported.update(patch, function (err) {
                    if (err) throw err;
                    console.log('twin state reported');
                });
            }
        });

        client.on('message', function (msg) {
            console.log('receive data: ' + msg.getData());

            try {
                var command = JSON.parse(msg.getData());
                if (command.Name === 'SetTemperature') {
                    temperature = command.Parameters.Temperature;
                    console.log('New temperature set to :' + temperature + 'F');
                }

                client.complete(msg, printErrorFor('complete'));
            }
            catch (err) {
                printErrorFor('parse received message')(err);
            }
        });

        // register handlers for all the method names we are interested in
        client.onDeviceMethod('getDeviceLog', onGetDeviceLog);
        client.onDeviceMethod('lockDoor', onLockDoor);
        client.onDeviceMethod('reboot', onReboot);

        // start event data send routing
        var sendInterval = setInterval(function () {
            temperature += generateRandomIncrement();
            externalTemperature += generateRandomIncrement();
            humidity += generateRandomIncrement();

            var dt = new Date();

            var data = JSON.stringify({
                'Temperature': temperature,
                'Humidity': humidity,
                'ExternalTemperature': externalTemperature,
                'time': dt.toFormat("YYYY-MM-DDTHH24:MI:SS")
            });

            console.log('Sending device event data:\n' + data);
            client.sendEvent(new Message(data), printErrorFor('send event'));
        }, 1000);

        client.on('error', function (err) {
            printErrorFor('client')(err);
            if (sendInterval) clearInterval(sendInterval);
            client.close(printErrorFor('client.close'));
        });
    }
});