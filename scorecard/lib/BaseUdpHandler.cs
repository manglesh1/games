using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace scorecard.lib
{
    public abstract class BaseUdpHandler
    {
        protected UdpClient udpSender;
        protected UdpClient udpClientReceiver;
        protected string destinationIpAddress;
        protected int destinationPort;
        protected int sourcePort;
        public IPEndPoint RemoteEndPoint;

        // private Logger logger;
        protected bool receiving;
        public int columns;
        public int Rows;
        public string name;
        public List<int> activeDevices = new List<int>();
        public Dictionary<int, List<int>> activeDevicesGroup = new Dictionary<int, List<int>>();
        public List<string> DeviceList { get;  set; }

        public BaseUdpHandler(string ipAddress, int destPort, int srcPort, int receiverPort, int noofledPerdevice, int columns, string namep) 
        {

            // logger = new AsyncLogger(namep);
            destinationIpAddress = ipAddress;
            destinationPort = destPort;
            sourcePort = srcPort;


            this.name = namep;
            udpClientReceiver = new UdpClient(receiverPort);
            udpSender = new UdpClient(sourcePort);
            RemoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
            
            // this .Rows = rows;
            receiving = true;
        }
        public virtual List<string> ReceiveMessage(int noofledPerdevice)
        {
            try
            {
                byte[] t = udpClientReceiver.Receive(ref RemoteEndPoint);
                int o;
                int noofdevices = Math.DivRem((t.Length - 2), noofledPerdevice, out o);

                var l = new List<string>(noofdevices);
                logger.Log($"no of devices found:{noofdevices} :{name}");
                for (int i = 0; i < noofdevices; i++)
                {
                    l.Add(noofledPerdevice == 1 ? ColorPaletteone.NoColor : ColorPalette.noColor3);
                    //  deviceMap.Add(i, new Device { color = noofledPerdevice==1?ColorPaletteone.NoColor: ColorPalette.noColor3, isActive = false, sequence = i });
                }
                return l;
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode == SocketError.TimedOut)
                {
                    // Handle the timeout scenario (e.g., log the timeout or notify the user)
                    logger.LogError("Can not get signal from devices. Please check controller power. Timeout occurred while waiting for data.");
                }
                else
                {
                    // Handle other potential socket errors
                    throw;
                }
            }
            return new List<string>();
        }
        public void BeginReceive(Action<byte[]> receiveCallback)
        {
            try
            {
                if (udpClientReceiver == null)
                {
                    return;
                }

                udpClientReceiver.BeginReceive(ar =>
                {
                    try
                    {
                        if (udpClientReceiver?.Client == null || !receiving)
                        {
                            return;
                        }

                        byte[] receivedBytes = udpClientReceiver.EndReceive(ar, ref RemoteEndPoint);

                        // Check the length of the receivedBytes before processing
                        if (receivedBytes == null || receivedBytes.Length < 2) // Adjust the minimum length based on your protocol
                        {
                            LogData("Received data is too short to process.");
                            BeginReceive(receiveCallback);
                            return;
                        }

                        if (receiving)
                        {
                            receiveCallback(receivedBytes);
                            //BeginReceive(receiveCallback); // Continue receiving
                        }
                    }
                    catch (ObjectDisposedException)
                    {
                        LogData("UdpClient has been disposed. Stopping receive loop.");
                    }
                    catch (Exception ex)
                    {
                        LogData($"Error in BeginReceive callback: {ex.Message}");
                    }
                }, null);
            }
            catch (Exception ex)
            {
                LogData($"Exception in BeginReceive: {ex.Message}");
            }
        }

        public void StopReceive()
        {
            receiving = false;
        }
        public void StartReceive()
        {
            receiving = true;
        }
       
      

        protected byte[] HexStringToByteArray(string hex)
        {
            //  hex = hex.Replace(" ", "");
            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            return bytes;
        }

        protected void LogData(string message)
        {
            string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fef} {message}";
            logger.Log(logMessage + Environment.NewLine);
            Console.WriteLine(logMessage);
        }

        public void Close()
        {
            StopReceive();

            try
            {
                udpClientReceiver?.Close();
                udpSender?.Close();
            }
            catch (Exception ex)
            {
                LogData($"Error closing UdpClient: {ex.Message}");
            }
            finally
            {
                udpClientReceiver = null;
                udpSender = null;
            }
        }

        public virtual async void SendColorsToUdp(List<string> colorList)
        {

        }
        public virtual async Task SendColorsToUdpAsync(List<string> colorList)
        {

        }
        
        public void SendColorsToUdp1(List<string> colorList)
        {
            SendColorsToUdp(colorList);
        }
    }
   
}
