using NAudio.Wave;
using scorecard.lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Schema;
namespace scorecard
{
    public class BaseGameClimb : BaseGame
    {
        protected int rows = 0;
        protected UdpHandlerWeTop climbHandler;
        protected Dictionary<int, Mapping> deviceMapping;
        public BaseGameClimb(GameConfig config) : base(config)
        {
            if (climbHandler == null)
                climbHandler = new UdpHandlerWeTop(ipAddress: config.IpAddress, destPort: config.LocalPort, 
                    srcPort: config.RemotePort, receiverPort: config.SocketBReceiverPort, 
                    noofledPerdevice: config.NoofLedPerdevice, columns: config.columns, namep: "handler-1");
            if (udpHandlers.Count == 0)
            {
                udpHandlers.Add(new UdpHandler("169.254.255.7", 21, 17113, 30105, 1, 18, "floor-1"));
                udpHandlers.Add(new UdpHandler("169.254.255.7", 22, 17114, 30106, 1, 9, "floor-2"));
            }
            deviceMapping = new Dictionary<int, Mapping>();
            int k = 0;
            foreach (UdpHandler handler in udpHandlers)
            {
                for (int i = 0; i < handler.DeviceList.Count; i++)
                {
                    deviceMapping.Add(k, new Mapping((UdpHandler)handler, false, Resequencer(i, handler.columns)));
                    k += 1;
                }
                rows += handler.Rows;
            }
        }
        protected int Resequencer(int index, int columns)
        {
            if ((index / columns) % 2 == 0)
            {
                return index;
            }

            int row = index / columns;
            int column = index % columns;
            int dest = (row + 1) * columns - 1 - column;
            return dest;
        }
        protected void SendColorToUdpAsync()
        {

            var tasks = new List<Task>();
            foreach (var handler in udpHandlers)
            {
                tasks.Add(handler.SendColorsToUdpAsync(handler.DeviceList));
            }
            Task.WhenAll(tasks);


        }
        protected override void BlinkAllAsync(int nooftimes)
        {

            for (int i = 0; i < nooftimes; i++)
            {
                var tasks = new List<Task>();

                var colors = climbHandler.DeviceList.Select(x => ColorPaletteone.Yellow).ToList();
                climbHandler.SendColorsToUdp(colors);
                Thread.Sleep(100);
                climbHandler.SendColorsToUdp(climbHandler.DeviceList);
                Thread.Sleep(100);
            }
        }
        protected override void LoopAll(string basecolor, int frequency)
        {
            for (int i = 0; i < frequency; i++)
            {

                var deepCopiedList = climbHandler.DeviceList.Select(x => basecolor).ToList();
                var loopColor = gameColors[random.Next(gameColors.Count - 1)];
                for (int j = 0; j < climbHandler.DeviceList.Count; j++)
                {
                    deepCopiedList[j] = loopColor;
                    climbHandler.SendColorsToUdp(deepCopiedList);
                    Thread.Sleep(100);
                    deepCopiedList[j] = basecolor;
                    climbHandler.SendColorsToUdp(deepCopiedList);
                    Thread.Sleep(100);
                }

                LogData($"LoopAll: {string.Join(",", deepCopiedList)}");
            }

        }
        protected void SendColorToUdp(List<string> colorList)
        {
            climbHandler.SendColorsToUdp(colorList);
        }
        public override void BlinkLights(List<int> lightIndex, int repeation, string Color)
        {
            for (int j = 0; j < repeation; j++)
            {
                climbHandler.SendColorsToUdp(climbHandler.DeviceList.Select((x, i) => lightIndex.Contains(i) ? Color : x).ToList());
                Thread.Sleep(100);
                climbHandler.SendColorsToUdp(climbHandler.DeviceList);
            }
        }
        protected static byte[] ProcessByteArray(byte[] input)
        {
            // Convert the input to a list for easier manipulation
            List<byte> byteList = input.ToList();
            List<byte> result = new List<byte>();
            int ct = byteList.Count;
            // Iterate through the list to remove patterns
            for (int i = 2; i < ct; i++)
            {
                if (byteList.Count < i)
                    break;
                if (byteList[i] == 0x88 || byteList[i] == 0x01 || byteList[i] == 0x02 || byteList[i] == 0x03 || byteList[i] == 0xBA)
                {

                }
                else
                {
                    result.Add(byteList[i]);
                }

            }

            // Convert the list back to a byte array
            return result.ToArray();
        }
        protected override void RunGameInSequence()
        {
            Status = GameStatus.Running;
            climbHandler.StartReceive();
            foreach(var handler in udpHandlers)
            {
                handler.StartReceive();
            }

            remainingTime = IterationTime; // Initialize remaining time
            OnIteration();
            isGameRunning = true;

            if (config.timerPointLoss)
                iterationTimer = new Timer(UpdateRemainingTime, null, 1000, 1000); // Update every second
            OnStart();
        }
        override protected void IterationLost(object state)
        {
            isGameRunning = false;
            climbHandler.StopReceive();
            foreach (var handler in udpHandlers)
            {
                handler.StopReceive();
            }

            if (!config.timerPointLoss && state == null)
            {
                IterationWon();
                return;
            }

            LogData($"iteration failed within {IterationTime} second");
            if (config.timerPointLoss)
                iterationTimer.Dispose();
            LifeLine = LifeLine - 1;
            Status = $"{GameStatus.Running} : Lost Lifeline {LifeLine}";
            if (lifeLine <= 0)
            {
                climbHandler.StopReceive();
                //TexttoSpeech: Oh no! You’ve lost all your lives. Game over! 🎮
                musicPlayer.Announcement("content/voicelines/GameOver.mp3", false);
                LogData("GAME OVER");
                Status = GameStatus.Completed;
            }
            else
            {

                musicPlayer.Announcement($"content/voicelines/lives_left_{LifeLine}.mp3");
                //iterations = iterations + 1;
                RunGameInSequence();
            }

        }
    }
}