﻿using scorecard;
using scorecard.lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class Target : BaseSingleDevice
{
    //his is to hold previous star color so that its not get duplicated
    protected HashSet<int> usedStarIndices1 = new HashSet<int>();
    private int starIndex = 18;
//    private var handler = udphandlers[0];
    public Target(GameConfig config, int starIndex) : base(config)
    {
        this.starIndex = starIndex;
        //if(istest)
        //    this.colors = new List<string> { ColorPaletteone.Pink, ColorPaletteone.Purple, ColorPaletteone.Navy, ColorPaletteone.Yellow, ColorPaletteone.Coral, ColorPaletteone.White, ColorPaletteone.Cyan };
    }
    protected override void Initialize()
    {
        var handler = udpHandlers[0];
        musicPlayer.PlayEffect("content/TargetIntro.wav");
        base.SendDataToDevice(config.NoofLedPerdevice == 1 ? ColorPaletteone.Silver : ColorPalette.SilverGrayWhite, starIndex);
        LoopAll(config.NoofLedPerdevice == 1 ? ColorPaletteone.NoColor : ColorPalette.noColor3,1);
        BlinkAllAsync(2);
    }

    protected override void OnStart()
    {
        //base.BlinkLights(new HashSet<int> { starIndex },2, handler);
        
        handler.BeginReceive(data => ReceiveCallback(data, handler));
       
    }

    protected override void OnIteration()
    {
        SendSameColorToAllDevice(config.NoofLedPerdevice == 1 ? ColorPaletteone.NoColor : ColorPalette.PinkCyanMagenta);
        BlinkAllAsync(1);
        SetColorsOfDevices();
       
    }

    private string GetStarColor()
    {
        int index;
        do
        {
            index = random.Next(gameColors.Count -1 );
        } while (usedStarIndices1.Contains(index));
        usedStarIndices1.Add(index);

        string starColor = gameColors[index];
        //handlerDevices[handler][starIndex] = starColor;
        return starColor;
    }





    int blinktime = 0;
    private void ReceiveCallback(byte[] receivedBytes, UdpHandler handler)
    {
        string receivedData = Encoding.UTF8.GetString(receivedBytes);
        LogData($"Received data from {this.handler.RemoteEndPoint}: {BitConverter.ToString(receivedBytes)}");

        List<int> positions = receivedData.Select((value, index) => new { value, index })
                                          .Where(x => x.value == 0x0A)
                                          .Select(x => x.index - 2)
                                          .Where(position => position >= 0)
                                          .ToList();

        LogData($"Touch detected: {string.Join(",", positions)}");

        foreach (int position in positions)
        {
            int actualPos = position / config.NoofLedPerdevice;
            if (activeIndices[handler].Contains(actualPos))
            {
                LogData("Color change detected");
                musicPlayer.PlayEffect("content/hit2.wav");
                ChnageColorToDevice(config.NoofLedPerdevice==1? ColorPaletteone.NoColor:ColorPalette.noColor3, actualPos, handler);
                handler.activeDevices.Remove(actualPos);
                base.Score = base.Score + 1;
                LogData($"Score updated: {Score}");
            }
        }
       
        if (activeIndices.Values.Where(x => x.Count > 0).Count() == 0)
        {
            MoveToNextIteration();
        }
        else
        {
            blinktime++;
            if (blinktime > 30)
            {
                BlinkLights(handler.activeDevices, 1, handler, ColorPaletteone.Blue);
                blinktime = 0;
            }
            handler.BeginReceive(data => ReceiveCallback(data, handler));
        }
       
    }

    private void SetColorsOfDevices()
    {
        handler.activeDevices.Clear();
        string starColor = GetStarColor();
        int numberOfStarColorDevices = (int)Math.Round(handler.DeviceList.Count() * 0.3);
        // HashSet<int> usedIndices = new HashSet<int> { starIndex };
       
        for (int i = 0; i < numberOfStarColorDevices; i++)
        {
            int index;
            do
            {
                index = random.Next(handler.DeviceList.Count());
            } while (handler.activeDevices.Contains(index));

            handler.DeviceList[index] = starColor;
            handler.activeDevices.Add(index);
        }
        //make sure star is colord
       

        for (int i = 0; i < handler.DeviceList.Count(); i++)
        {
            if (!handler.activeDevices.Contains(i))
            {
                string newColor;
                do
                {
                    newColor = gameColors[random.Next(gameColors.Count-1)];
                } while (newColor == starColor);

                handler.DeviceList[i] = newColor;
            }
        }
        handler.activeDevices.Remove(starIndex);

        handler.DeviceList[starIndex] = starColor;
        handler.SendColorsToUdp(handler.DeviceList);
      // handler.activeDevices = usedIndices;
        LogData($"Sending final colors: {string.Join(",", handler.DeviceList)}");
        LogData($"Sending star color: {handler.DeviceList[starIndex]}");
    }
}
