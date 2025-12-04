//  KZ-E02.NET Protocol V3.0

using iParkingv5.Objects.Events;
using QuanLyXe03.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using static Kztek.Object.InputTupe;
using Bdk = QuanLyXe03.Helpers.Bdk;

namespace iParkingv5.Controller.KztekDevices.KZE02NETController
{
    public class KzE02Net
    {
        public Thread? thread = null;
        public Bdk ControllerInfo { get; set; } = new Bdk();
        public bool IsBusy = false;
        public ManualResetEvent? stopEvent;

        public bool Running
        {
            get
            {
                if (thread != null)
                {
                    if (thread.Join(0) == false)
                        return true;

                    Free();
                }
                return false;
            }
        }

        public void DeleteCardEvent()
        {
            if (isTest)
            {
                return;
            }
            string comport = this.ControllerInfo.Comport;
            int baudrate = GetBaudrate(this.ControllerInfo.Baudrate);
            string cmd = "DeleteEvent?/";
            UdpTools.ExecuteCommand(comport, baudrate, cmd, 500);
        }

        public void PollingStart()
        {
            if (thread == null)
            {
                stopEvent = new ManualResetEvent(false);
                thread = new Thread(new ThreadStart(WorkerThread));
                thread.Start();
            }
        }

        public void PollingStop()
        {
            if (this.Running)
            {
                SignalToStop();
                while (thread != null && thread.IsAlive)
                {
                    if (stopEvent != null && WaitHandle.WaitAll(
                        (new ManualResetEvent[] { stopEvent }),
                        100,
                        true))
                    {
                        WaitForStop();
                        break;
                    }
                }
            }
        }

        public void SignalToStop()
        {
            if (thread != null && stopEvent != null)
            {
                stopEvent.Set();
            }
        }

        public void WaitForStop()
        {
            if (thread != null)
            {
                thread.Join();
                Free();
            }
        }

        private void Free()
        {
            thread = null;
            if (stopEvent != null)
            {
                stopEvent.Close();
                stopEvent = null;
            }
        }

        private bool isTest = false;

        public async void WorkerThread()
        {
            while (stopEvent != null)
            {
                if (stopEvent.WaitOne(0, true))
                {
                    return;
                }
                try
                {
                    string comport = this.ControllerInfo.Comport;
                    int baudrate = GetBaudrate(this.ControllerInfo.Baudrate);
                    string getEventCmd = "GetEvent?/";
                    this.IsBusy = true;
                    string response = string.Empty;

                    this.IsBusy = false;

                    await Task.Run(() =>
                    {
                        response = UdpTools.ExecuteCommand(comport, baudrate, getEventCmd, 500);
                    });

                    this.ControllerInfo.IsConnect = response != "";
                    OnRawEventMessage?.Invoke(response);

                    if (response != "" && (response.Contains("GetEvent?/")) && !response.Contains("NotEvent"))
                    {
                        string[] data = response.Split('/');
                        Dictionary<string, string> map = GetEventContent(data);
                        bool isCardEvent = response.Contains("Card");
                        if (isCardEvent)
                        {
                            CallCardEvent(this.ControllerInfo, map);
                        }
                        else
                        {
                            CallInputEvent(this.ControllerInfo, map);
                        }
                    }
                    await Task.Delay(100);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[KzE02Net] WorkerThread error: {ex.Message}");
                }
            }
        }

        private void CallInputEvent(Bdk controller, Dictionary<string, string> map)
        {
            InputEventArgs ie = new InputEventArgs
            {
                DeviceId = controller.Id,
                DeviceName = controller.Name,
                DeviceType = EmParkingControllerType.NomalController,
            };
            string str_inputName = map.ContainsKey("input") ? map["input"] : "";
            if (!string.IsNullOrEmpty(str_inputName))
            {
                string str_inputIndex = str_inputName.Replace("INPUT", "");
                ie.InputIndex = Regex.IsMatch(str_inputIndex, @"^\d+$") ? int.Parse(str_inputIndex) : -1;
            }
            if (ie.InputIndex == 1 || ie.InputIndex == 2)
            {
                ie.InputType = EmInputType.Exit;
            }
            else if (ie.InputIndex == 3 || ie.InputIndex == 4)
            {
                ie.InputType = EmInputType.Loop;
            }
            DeleteCardEvent();
        }

        private void CallCardEvent(Bdk controller, Dictionary<string, string> map)
        {
            try
            {
                CardEventArgs e = new CardEventArgs
                {
                    DeviceId = controller.Id,
                    DeviceName = controller.Name,
                    DeviceType = EmParkingControllerType.NomalController,
                };
                string cardNumberHEX = map.ContainsKey("card") ? map["card"] : "";
                e.PreferCard = cardNumberHEX;
                string str_readerIndex = map.ContainsKey("reader") ? map["reader"] : "";
                e.ReaderIndex = Regex.IsMatch(str_readerIndex, @"^\d+$") ? Convert.ToInt32(str_readerIndex) : 0;
                string cardState = map.ContainsKey("cardstate") ? map["cardstate"] : "";
                if (cardState == "R")
                {
                    string door = map.ContainsKey("door") ? map["door"] : "";
                    if (!string.IsNullOrEmpty(door))
                    {
                        if (door == "01")
                        {
                            e.Doors = "1";
                        }
                        if (door == "02")
                        {
                            e.Doors = "2";
                        }
                    }
                }
                else
                {
                    e.Doors = "";
                }
                DeleteCardEvent();
            }
            catch (Exception)
            {
                DeleteCardEvent();
            }
        }

        public async Task<bool> OpenDoor(int timeInMilisecond, int relayIndex)
        {
            string comport = this.ControllerInfo.Comport;
            int baudrate = GetBaudrate(this.ControllerInfo.Baudrate);
            string openRelayCmd = $"SetRelay?/Relay={relayIndex:D2}/State=ON";

            this.IsBusy = true;
            string response = string.Empty;
            await Task.Run(() =>
            {
                response = UdpTools.ExecuteCommand(comport, baudrate, openRelayCmd, 500);
            });
            this.IsBusy = false;

            if (response.Contains("OK"))
            {
                return true;
            }
            else if (response.Contains("ERR"))
            {
                return false;
            }
            return false;
        }

        public bool DeleteCard(string userId, string cardNumber, out string errorMessage, out int errorCode)
        {
            string comport = this.ControllerInfo.Comport;
            int baudrate = GetBaudrate(this.ControllerInfo.Baudrate);
            string deleteCMD = $"DeleteUser?/UserID={userId}";
            string response = UdpTools.ExecuteCommand(comport, baudrate, deleteCMD, 500);

            if (response.Contains("OK"))
            {
                errorMessage = string.Empty;
                errorCode = -1;
                return true;
            }
            errorCode = -1;
            errorMessage = "Device return false";
            return false;
        }

        public bool DownloadCard(string userId, string cardNumber, int lenCard, string pin, int timezoneId, string doors, out string errorMessage, out int errorCode)
        {
            string comport = this.ControllerInfo.Comport;
            int baudrate = GetBaudrate(this.ControllerInfo.Baudrate);
            string door = int.Parse(doors).ToString("00");

            if (string.IsNullOrEmpty(door))
            {
                errorCode = -1;
                errorMessage = "UNKNOWN";
                return false;
            }

            string downloadCMD = $"DownloadUser?/UserID={userId}/LenCard={lenCard}/Card={cardNumber}/Pin={pin}/Mode=0/TimeZone={timezoneId}/Door={door}";
            string response = UdpTools.ExecuteCommand(comport, baudrate, downloadCMD, 500);
            bool result = response.Contains("OK");
            errorCode = -1;
            errorMessage = result ? string.Empty : "Download failed";
            return result;
        }

        public async Task<string> GetMode()
        {
            string comport = this.ControllerInfo.Comport;
            int baudrate = GetBaudrate(this.ControllerInfo.Baudrate);
            string cmd = "GetMode?/";

            string response = string.Empty;
            await Task.Run(() =>
            {
                response = UdpTools.ExecuteCommand(comport, baudrate, cmd, 1000);
            });

            return response;
        }

        public async Task<bool> SetMode(int mode)
        {
            string comport = this.ControllerInfo.Comport;
            int baudrate = GetBaudrate(this.ControllerInfo.Baudrate);
            string cmd = $"SetMode?/Mode={mode}";

            string response = string.Empty;
            await Task.Run(() =>
            {
                response = UdpTools.ExecuteCommand(comport, baudrate, cmd, 1000);
            });

            return response.Contains("OK");
        }

        public async Task<string> GetFirmwareVersion()
        {
            string comport = this.ControllerInfo.Comport;
            int baudrate = GetBaudrate(this.ControllerInfo.Baudrate);
            string cmd = "GetFirmwareVersion?/";

            string response = string.Empty;
            await Task.Run(() =>
            {
                response = UdpTools.ExecuteCommand(comport, baudrate, cmd, 1000);
            });

            return response;
        }

        public async Task<string> GetDateTime()
        {
            string comport = this.ControllerInfo.Comport;
            int baudrate = GetBaudrate(this.ControllerInfo.Baudrate);
            string cmd = "GetDateTime?/";

            string response = string.Empty;
            await Task.Run(() =>
            {
                response = UdpTools.ExecuteCommand(comport, baudrate, cmd, 1000);
            });

            return response;
        }

        public async Task<bool> SetDateTime(DateTime dateTime)
        {
            string comport = this.ControllerInfo.Comport;
            int baudrate = GetBaudrate(this.ControllerInfo.Baudrate);
            string cmd = $"SetDateTime?/{dateTime:yyyyMMddHHmmss}";

            string response = string.Empty;
            await Task.Run(() =>
            {
                response = UdpTools.ExecuteCommand(comport, baudrate, cmd, 1000);
            });

            return response.Contains("OK");
        }

        public async Task<bool> SetRelayDelayTime(int timeMs)
        {
            string comport = this.ControllerInfo.Comport;
            int baudrate = GetBaudrate(this.ControllerInfo.Baudrate);
            string cmd = $"SetRelayDelayTime?/Time={timeMs}";

            string response = string.Empty;
            await Task.Run(() =>
            {
                response = UdpTools.ExecuteCommand(comport, baudrate, cmd, 1000);
            });

            return response.Contains("OK");
        }

        public async Task<string> GetInputState()
        {
            string comport = this.ControllerInfo.Comport;
            int baudrate = GetBaudrate(this.ControllerInfo.Baudrate);
            string cmd = "GetInputState?/";

            string response = string.Empty;
            await Task.Run(() =>
            {
                response = UdpTools.ExecuteCommand(comport, baudrate, cmd, 1000);
            });

            return response;
        }

        public int GetBaudrate(string baudRateStr)
        {
            int baudrate = 0;
            if (!string.IsNullOrEmpty(this.ControllerInfo.Baudrate))
            {
                try
                {
                    baudrate = int.Parse(this.ControllerInfo.Baudrate);
                }
                catch (Exception ex)
                {
                    string errorMessage = $"Controller {this.ControllerInfo.Comport} Got Baudrate Error: {ex.Message}";
                    throw new Exception(errorMessage);
                }
            }
            return baudrate;
        }

        public static Dictionary<string, string> GetEventContent(string[] datas)
        {
            Dictionary<string, string> output = new Dictionary<string, string>();
            foreach (string data in datas)
            {
                if (data.Contains("="))
                {
                    string[] subData = data.Split('=');
                    if (subData.Length >= 2)
                    {
                        output.Add(subData[0].ToLower().Trim(), subData[1].Trim());
                    }
                }
            }
            return output;
        }

        public event Action<string>? OnRawEventMessage;
    }
}