﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Thorlabs.MotionControl.DeviceManagerCLI;
using Thorlabs.MotionControl.KCube.InertialMotorCLI;

namespace KIM_Console_net_managed
{
    class Program
    {
        static void Main(string[] args)
        {
            // Uncomment this line (and SimulationManager.Instance.UninitializeSimulations() at the end on Main)
            // If you are using a simulated device
            // SimulationManager.Instance.InitializeSimulations();

            // Enter the serial number for your device
            string serialNo = "97000001";

            try
            {
                // Tell the device manager to get the list of all devices connected to the computer
                DeviceManagerCLI.BuildDeviceList();
            }
            catch (Exception ex)
            {
                // An error occurred - see ex for details
                Console.WriteLine("Exception raised by BuildDeviceList {0}", ex);
                Console.ReadKey();
                return;
            }

            // Get available KCube Inertial Motor and check our serial number is correct - by using the device prefix
            // (i.e. for serial number 97000123, the device prefix is 97)
            List<string> serialNumbers = DeviceManagerCLI.GetDeviceList(KCubeInertialMotor.DevicePrefix_KIM101);
            if (!serialNumbers.Contains(serialNo))
            {
                // The requested serial number is not a KIM101 or is not connected
                Console.WriteLine("{0} is not a valid serial number", serialNo);
                Console.ReadKey();
                return;
            }

            // Create the device - KIM
            KCubeInertialMotor device = KCubeInertialMotor.CreateKCubeInertialMotor(serialNo);
            if (device == null)
            {
                // An error occured
                Console.WriteLine("{0} is not a KCubeInertialMotor", serialNo);
                Console.ReadKey();
                return;
            }

            // Open a connection to the device.
            try
            {
                Console.WriteLine("Opening device {0}", serialNo);
                device.Connect(serialNo);
            }
            catch (Exception)
            {
                // Connection failed
                Console.WriteLine("Failed to open device {0}", serialNo);
                Console.ReadKey();
                return;
            }

            // Wait for the device settings to initialize - timeout 5000ms
            if (!device.IsSettingsInitialized())
            {
                try
                {
                    device.WaitForSettingsInitialized(5000);
                }
                catch (Exception)
                {
                    Console.WriteLine("Settings failed to initialize");
                }
            }

            // Display info about device
            DeviceInfo deviceInfo = device.GetDeviceInfo();
            Console.WriteLine("Device {0} = {1}", deviceInfo.SerialNumber, deviceInfo.Name);

            // Start the device polling
            // The polling loop requests regular status requests to the motor to ensure the program keeps track of the device. 
            device.StartPolling(250);
            // Needs a delay so that the current enabled state can be obtained
            Thread.Sleep(500);
            // Enable the channel otherwise any move is ignored 
            device.EnableDevice();
            // Needs a delay to give time for the device to be enabled
            Thread.Sleep(500);

            // Call GetInertialMotorConfiguration on the device to initialize the settings
            InertialMotorConfiguration InertialMotorConfiguration = device.GetInertialMotorConfiguration(serialNo);
            ThorlabsInertialMotorSettings currentDeviceSettings = ThorlabsInertialMotorSettings.GetSettings(InertialMotorConfiguration);

            // Set the 'Step' paramaters for the Inertia Motor and download to device
            currentDeviceSettings.Drive.Channel(InertialMotorStatus.MotorChannels.Channel1).StepRate = 500;
            currentDeviceSettings.Drive.Channel(InertialMotorStatus.MotorChannels.Channel1).StepAcceleration = 100000;
            device.SetSettings(currentDeviceSettings, true, true);

            // Zero the device
            device.SetPositionAs(InertialMotorStatus.MotorChannels.Channel1, 0);

            int position = 1000;
            Move_Method1(device, InertialMotorStatus.MotorChannels.Channel1, position);
            // or
            // Move_Method2(device, InertialMotorStatus.MotorChannels.Channel1, position);

            Decimal newPos = device.GetPosition(InertialMotorStatus.MotorChannels.Channel1);
            Console.WriteLine("Device Moved to {0}", newPos);

            // Tidy up and exit
            device.StopPolling();
            device.Disconnect(true);

            // Uncomment this line if you are using Simulations
            //SimulationManager.Instance.UninitializeSimulations();

            Console.ReadKey();
        }

        public static void Move_Method1(KCubeInertialMotor device, InertialMotorStatus.MotorChannels channel, int position)
        {
            try
            {
                Console.WriteLine("Moving Device to {0}", position);
                device.MoveTo(channel, position, 60000);
            }
            catch (Exception)
            {
                Console.WriteLine("Failed to move to position");
                Console.ReadKey();
                return;
            }
            Console.WriteLine("Device Moved");
        }

        private static bool _taskComplete;
        private static ulong _taskID;

        public static void CommandCompleteFunction(ulong taskID)
        {
            if ((_taskID > 0) && (_taskID == taskID))
            {
                _taskComplete = true;
            }
        }

        public static void Move_Method2(KCubeInertialMotor device, InertialMotorStatus.MotorChannels channel, int position)
        {
            Console.WriteLine("Moving Device to {0}", position);
            _taskComplete = false;
            _taskID = device.MoveTo(channel, position, CommandCompleteFunction);
            while (!_taskComplete)
            {
                Thread.Sleep(500);
                int actualPosition = device.GetPosition(channel);
                Console.WriteLine("Device Moving {0}", actualPosition);

                // will need some timeout functionality;
            }
            Console.WriteLine("Device Moved");
        }
    }
}
