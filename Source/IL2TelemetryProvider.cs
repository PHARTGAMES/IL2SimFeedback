//MIT License
//
//Copyright(c) 2019 PHARTGAMES
//
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.
//
using SimFeedback.log;
using SimFeedback.telemetry;
using System;
using System.Diagnostics;
using System.Threading;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Numerics;
using NoiseFilters;

namespace IL2Telemetry
{
    /// <summary>
    /// IL2 Sturmovik Telemetry Provider
    /// </summary>
    public sealed class IL2TelemetryProvider : AbstractTelemetryProvider
    {
        private bool isStopped = true;                                  // flag to control the polling thread
        private Thread t;
        int PORTNUM = 4321;
        private IPEndPoint _senderIP;                   // IP address of the sender for the udp connection used by the worker thread


        /// <summary>
        /// Default constructor.
        /// Every TelemetryProvider needs a default constructor for dynamic loading.
        /// Make sure to call the underlying abstract class in the constructor.
        /// </summary>
        public IL2TelemetryProvider() : base()
        {
            Author = "PEZZALUCIFER";
            Version = "v1.1";
            BannerImage = @"img\banner_il2.png"; // Image shown on top of the profiles tab
            IconImage = @"img\il2.jpg";  // Icon used in the tree view for the profile
            TelemetryUpdateFrequency = 100;     // the update frequency in samples per second
        }

        /// <summary>
        /// Name of this TelemetryProvider.
        /// Used for dynamic loading and linking to the profile configuration.
        /// </summary>
        public override string Name { get { return "il2"; } }

        public override void Init(ILogger logger)
        {
            base.Init(logger);
            Log("Initializing IL2Telemetry");
        }

        /// <summary>
        /// A list of all telemetry names of this provider.
        /// </summary>
        /// <returns>List of all telemetry names</returns>
        public override string[] GetValueList()
        {
            return GetValueListByReflection(typeof(IL2API));
        }

        /// <summary>
        /// Start the polling thread
        /// </summary>
        public override void Start()
        {
            if (isStopped)
            {
                LogDebug("Starting IL2Telemetry");
                isStopped = false;
                t = new Thread(Run);
                t.Start();
            }
        }

        /// <summary>
        /// Stop the polling thread
        /// </summary>
        public override void Stop()
        {
            LogDebug("Stopping IL2Telemetry");
            isStopped = true;
            if (t != null) t.Join();
        }

        /// <summary>
        /// The thread funktion to poll the telemetry data and send TelemetryUpdated events.
        /// </summary>
        private void Run()
        {
            IL2API lastTelemetryData = new IL2API();
            lastTelemetryData.Reset();
            Session session = new Session();
            Stopwatch sw = new Stopwatch();
            sw.Start();

            UdpClient socket = new UdpClient();
            socket.ExclusiveAddressUse = false;
            socket.Client.Bind(new IPEndPoint(IPAddress.Any, PORTNUM));

            Log("Listener started (port: " + PORTNUM.ToString() + ") IL2TelemetryProvider.Thread");
            while (!isStopped)
            {
                try
                {
                    // get data from game, 
                    if (socket.Available == 0)
                    {
                        if (sw.ElapsedMilliseconds > 500)
                        {
                            IsRunning = false;
                            IsConnected = false;
                            Thread.Sleep(1000);
                        }
                        continue;
                    }
                    else
                    {
                        IsConnected = true;
                    }

                    Byte[] received = socket.Receive(ref _senderIP);


                    var alloc = GCHandle.Alloc(received, GCHandleType.Pinned);
                    IL2API telemetryData = (IL2API)Marshal.PtrToStructure(alloc.AddrOfPinnedObject(), typeof(IL2API));

                    // otherwise we are connected
                    IsConnected = true;

                    if (telemetryData.packetID == 0x494C0100)
                    {
                        IsRunning = true;

                        sw.Restart();

                        IL2API telemetryToSend = new IL2API();
                        telemetryToSend.Reset();

                        telemetryToSend.CopyFields(telemetryData);

                        telemetryToSend.roll = LoopAngle( telemetryData.roll * ( 180.0f / (float)Math.PI ), 90.0f );
                        telemetryToSend.pitch = telemetryData.pitch * (180.0f / (float)Math.PI );
                        telemetryToSend.yaw = telemetryData.yaw * (180.0f / (float)Math.PI );

                        TelemetryEventArgs args = new TelemetryEventArgs(
                            new IL2TelemetryInfo(telemetryToSend, lastTelemetryData));
                        RaiseEvent(OnTelemetryUpdate, args);

                        lastTelemetryData = telemetryToSend;
                    }
                    else if (sw.ElapsedMilliseconds > 500)
                    {
                        IsRunning = false;
                    }
                }
                catch (Exception e)
                {
                    LogError("IL2Telemetry Exception while processing data", e);
                    IsConnected = false;
                    IsRunning = false;
                    Thread.Sleep(1000);
                }

            }

            socket.Close();
            IsConnected = false;
            IsRunning = false;
        }
        private float LoopAngle( float angle, float minMag )
        {

            float absAngle = Math.Abs( angle );

            if ( absAngle <= minMag )
            {
                return angle;
            }

            float direction = angle / absAngle;

            //(180.0f * 1) - 135 = 45
            //(180.0f *-1) - -135 = -45
            float loopedAngle = ( 180.0f * direction ) - angle;

            return loopedAngle;
        }
    }
}
