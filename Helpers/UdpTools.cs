using QuanLyXe03.Helpers;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace QuanLyXe03.Helpers
{
    public class UdpTools
    {
        public const string STX = "\x02";  // Dùng char thay vì "02" để đúng byte
        public const string ETX = "\x03";

       
        public static bool Start_UDP_Server(string ipAddress, int Port, ref Socket UdpServer, ref IPEndPoint ipEpBroadcast)
        {
            try
            {
                UdpServer = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                ipEpBroadcast = new IPEndPoint(IPAddress.Parse(ipAddress), Port);
                UdpServer.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
                return true;
            }
            catch
            {
            }
            return false;
        }

        public static string ExecuteCommand_Ascii(string ip_Address, int port, string command, int delayTime = 500)
        {
            
            string ret = "";
            try
            {
                string viewraw = "";
                string[] message = null;
                Socket? UdpServer = null;
                IPEndPoint? ipEpBroadcast = null;
                if (NetWorkTools.IsPingSuccess(ip_Address, 50))
                {
                    if (Start_UDP_Server(ip_Address, port, ref UdpServer, ref ipEpBroadcast))
                    {
                        byte[] bData = Encoding.ASCII.GetBytes(command);
                        UdpServer.SendTo(bData, ipEpBroadcast);
                        UdpServer.ReceiveTimeout = 1000;
                        try
                        {
                            if (UdpServer.ReceiveBufferSize != 0)
                            {
                                byte[] bRebuff = new byte[UdpServer.ReceiveBufferSize];
                                int readLen = UdpServer.Receive(bRebuff);
                                byte checksum = 1;
                                for (int i = 1; i < readLen - 2; i++)
                                    checksum += bRebuff[i];
                                message = ByteUI.Get_Message(bRebuff, readLen, ref viewraw);
                                ByteUI.Get_Message(bData, bData.Length, ref viewraw);
                                ret = Encoding.UTF8.GetString(bRebuff, 0, readLen);
                            }
                        }
                        catch (Exception ex)
                        {
                            ret = ex.ToString();
                        }
                        finally
                        {
                            UdpServer.Close();
                            UdpServer.Dispose();
                            ipEpBroadcast = null;
                        }
                    }
                    else
                    {
                        ret = "ERROR: Socket Start Error";
                    }
                }
                else
                {
                    ret = "ERROR: Ping Error";
                }
            }
            catch (Exception ex)
            {
                return "ERROR: " + ex.ToString();
            }
            return ret;
        }

        

        public static string ExecuteCommand_UTF8(string comport, int baudrate, string command, int delayTime = 100)
        {
            
            string ret = "";
            try
            {
                string viewraw = "";
                string[] message = null;
                Socket UdpServer = null;
                IPEndPoint ipEpBroadcast = null;
                if (NetWorkTools.IsPingSuccess(comport, 500))
                {
                    if (Start_UDP_Server(comport, baudrate, ref UdpServer, ref ipEpBroadcast))
                    {
                        byte[] bData = Encoding.UTF8.GetBytes(command);
                        UdpServer.SendTo(bData, ipEpBroadcast);
                        UdpServer.ReceiveTimeout = 2000;
                        Thread.Sleep(150);
                        try
                        {
                            if (UdpServer.ReceiveBufferSize != 0)
                            {
                                byte[] bRebuff = new byte[UdpServer.ReceiveBufferSize];
                                int readLen = UdpServer.Receive(bRebuff);
                                byte checksum = 1;
                                for (int i = 1; i < readLen - 2; i++)
                                    checksum += bRebuff[i];
                                message = ByteUI.Get_Message(bRebuff, readLen, ref viewraw);
                                ByteUI.Get_Message(bData, bData.Length, ref viewraw);
                                ret = Encoding.UTF8.GetString(bRebuff, 0, readLen);
                            }
                        }
                        catch (Exception ex)
                        {
                            ret = ex.ToString();
                        }
                        finally
                        {
                            UdpServer.Close();
                            UdpServer = null;
                            ipEpBroadcast = null;
                        }
                    }
                    else
                    {
                        ret = "Socket Start Error";
                    }
                    Thread.Sleep(delayTime);
                }
                else
                {
                    ret = "Ping Error";
                }
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
            return ret;
        }



        // DÙNG CHO KZ-E02 
        public static string ExecuteCommand(string ip, int port, string command, int timeoutMs = 5000)
        {
            var data = Encoding.ASCII.GetBytes(command); // KHÔNG + STX + ETX

            using var client = new UdpClient();
            client.Client.ReceiveTimeout = timeoutMs;
            client.Client.SendTimeout = timeoutMs;

            var endpoint = new IPEndPoint(IPAddress.Parse(ip), port);

            try
            {
                Debug.WriteLine($"[UDP] Gửi → {ip}:{port} | {command}");
                client.Send(data, data.Length, endpoint);

                var result = client.Receive(ref endpoint);
                var response = Encoding.ASCII.GetString(result);

                Debug.WriteLine($"[UDP] Nhận ← {response}");
                return response; // Trả nguyên response (có echo lệnh)
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UDP] LỖI: {ex.Message}");
                return $"UDP Error: {ex.Message}";
            }
        }


        public static string ExecuteCommand(string comport, int baudrate, string command, int delayTime, string startStr, Encoding encodingType)
        {
            
            string ret = "";
            try
            {
                string viewraw = "";
                string[] message = null;
                Socket UdpServer = null;
                IPEndPoint ipEpBroadcast = null;
                if (Start_UDP_Server(comport, baudrate, ref UdpServer, ref ipEpBroadcast))
                {
                    byte[] bData = encodingType.GetBytes(command);
                    UdpServer.SendTo(bData, ipEpBroadcast);
                    UdpServer.ReceiveTimeout = 2000;
                    try
                    {
                        if (UdpServer.ReceiveBufferSize != 0)
                        {
                            byte[] bRebuff = new byte[UdpServer.ReceiveBufferSize];
                            int readLen = UdpServer.Receive(bRebuff);
                            byte checksum = 1;
                            for (int i = 1; i < readLen - 2; i++)
                                checksum += bRebuff[i];
                            message = ByteUI.Get_Message(bRebuff, readLen, ref viewraw);
                            if (IsSuccess(message[0], startStr))
                            {
                                ByteUI.Get_Message(bData, bData.Length, ref viewraw);
                                string s1 = Encoding.UTF8.GetString(bRebuff, 0, readLen);
                                ret = Encoding.UTF8.GetString(bRebuff, 1, readLen - 2);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ret = ex.ToString();
                    }
                    finally
                    {
                        UdpServer.Close();
                        UdpServer = null;
                        ipEpBroadcast = null;
                    }
                }
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
            finally
            {
                GC.Collect();
            }
            return ret;
        }

        public static bool IsSuccess(string response, string searchStr)
        {
            if (response.Contains(searchStr))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}