﻿using NAudio.Gui;
using NAudio.Utils;
using scorecard;
using scorecard.lib;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class FloorGame1 : BaseMultiDevice
{
    private int killerSpeedReduction = 200;
    private System.Threading.Timer gameTimer;
    private bool isReversed = false; // Track the direction of the killer line

    public FloorGame1(GameConfig config, int killerSpeedReduction) : base(config)
    {
        this.killerSpeedReduction = killerSpeedReduction;
    }

    protected void MakeSurroundingMap()
    {
        // Implementation for creating a surrounding map
    }

    protected override void Initialize()
    {
        AnimateColor(false);
        AnimateColor(true);
        BlinkAllAsync(4);
    }

    protected override void OnStart()
    {
        if (gameTimer == null)
        {
            gameTimer = new System.Threading.Timer(drawkillingline, null, 1000, 500000000); // Change target tiles every 10 seconds
        }

        foreach (var handler in udpHandlers)
        {
            handler.BeginReceive(data => ReceiveCallback(data, handler));
        }
    }

    private Dictionary<UdpHandler, List<int>> killerRowsDict = new Dictionary<UdpHandler, List<int>>();
    private List<int> obstaclePositions = new List<int>();

    protected override void OnIteration()
    {
        SendSameColorToAllDevice(ColorPaletteone.Red, true);
        targetColor = ColorPaletteone.Green;
        int totalTargets = 0;

        obstaclePositions.Clear();
        foreach (var handler in udpHandlers)
        {
            handler.activeDevicesGroup.Clear();
        }

        while (totalTargets < config.MaxPlayers)
        {
            if (totalTargets >= config.MaxPlayers)
                break;

            int origMain = random.Next(0, deviceMapping.Count - 1);

            while (!isValidpos(origMain))
            {
                origMain = random.Next(0, deviceMapping.Count - 1);
            }

            int nextPosition = 1;
            int nextRowAdd = config.columns;
            if ((origMain % config.columns == 0 && origMain != 0) || origMain == rows*config.columns)
            {
                nextPosition = -1;
            }
            if (deviceMapping.Count - origMain < config.columns)
            {
                nextRowAdd = -1 * nextRowAdd;
            }

            int mainRight = origMain + nextPosition;
            int mainBelow = origMain + nextRowAdd;
            int mainBelowRight = mainBelow + nextPosition;
            List<int> group = new List<int> { origMain, mainRight, mainBelow, mainBelowRight };
            obstaclePositions.AddRange(group);

            List<int> ActualGroup = new List<int>();

            foreach (var item in group)
            {
                if (base.deviceMapping.ContainsKey(item))
                    ActualGroup.Add(base.deviceMapping[item].deviceNo);
            }
            foreach (var item in group)
            {
                int actualHandlerPos = base.deviceMapping[item].deviceNo;
                base.deviceMapping[item].udpHandler.DeviceList[actualHandlerPos] = ColorPaletteone.Green;
                base.deviceMapping[item].udpHandler.activeDevicesGroup.Add(actualHandlerPos, ActualGroup);
                base.deviceMapping[item].isActive = true;
            }
            LogData($"Active devices filling active devices: {string.Join(",", obstaclePositions)}");
            totalTargets++;
        }
        SendColorToUdpAsync();
    }

    private bool isValidpos(int pos)
    {
        var device = base.deviceMapping[pos];
        int lastrowmax = device.udpHandler.DeviceList.Count;
        int lastrowmin = lastrowmax - config.columns;

        if (device.deviceNo <= lastrowmax && device.deviceNo > lastrowmin)
        {
            return false;
        }
        foreach (int x in obstaclePositions)
        {
            List<int> b = surroundingMap[x];
            if (b.Contains(pos))
            {
                return false;
            }
        }
        return true;
    }

    protected void drawkillingline(object state)
    {
        if (!isGameRunning)
        {
            gameTimer = null;
            return;
        }
        UdpHandler prevhandler=null;
        if (!isReversed)
        {
            for (int handlerCount = 0; handlerCount < udpHandlers.Count; handlerCount++)
            {
                UdpHandler handler = udpHandlers[handlerCount];
                if (prevhandler != null)
                {
                    prevhandler.SendColorsToUdp(prevhandler.DeviceList);
                }

                // Move the killer line from top to bottom
                for (int row = 0; row < handler.Rows; row++)
                {
                    MoveKillerLine(handler, row);
                }

                prevhandler = handler;
            }
        }
        else
        {
            for (int handlerCount = udpHandlers.Count-1; handlerCount >=0; handlerCount--)
            {
                UdpHandler handler = udpHandlers[handlerCount];
                if (prevhandler != null)
                {
                    prevhandler.SendColorsToUdp(prevhandler.DeviceList);
                }
                for (int row = handler.Rows - 1; row >= 0; row--)
                {
                    MoveKillerLine(handler, row);
                }

                prevhandler = handler;
            }
        }
        // Reverse the direction when the killer line reaches the end
        isReversed = !isReversed;

        if (isGameRunning)
        {
            drawkillingline(null);
        }
    }

    private void MoveKillerLine(UdpHandler handler, int row)
    {
        var colorList = new List<string>();
        var cl = handlerDevices[handler].Select(x => x).ToList();
        int rowNum = (row / handler.Rows) % 2 == 0 ? (row % handler.Rows) : handler.Rows - 1 - (row % handler.Rows);
        var blueLineDevices = new List<int>();

        for (int i = 0; i < config.columns; i++)
        {
            if (handler.activeDevices.Contains(rowNum * config.columns + i))
                continue;

            cl[rowNum * config.columns + i] = ColorPaletteone.Blue;
            blueLineDevices.Add(rowNum * config.columns + i);
        }

        if (!isGameRunning)
        {
            gameTimer = null;
            return;
        }
        killerRowsDict.Clear();
        handler.SendColorsToUdp(cl);
        killerRowsDict.Add(handler, blueLineDevices);
        LogData($"filling data handler row:{row} handler:{handler.name} active:{string.Join(",", handler.activeDevices)} blueline: {string.Join(",", blueLineDevices)}");

        Thread.Sleep(1000 - (base.level - 1) * killerSpeedReduction);
    }

    private void ReceiveCallback(byte[] receivedBytes, UdpHandler handler)
    {
        if (!isGameRunning)
            return;

        string receivedData = Encoding.UTF8.GetString(receivedBytes);
        var positions = receivedData
            .Select((value, index) => new { value, index })
            .Where(x => x.value == 0x0A)
            .Select(x => x.index - 2)
            .Where(position => position >= 0)
            .ToList();

        if (positions.Count > 0)
        {
            LogData($"Received data from {handler.RemoteEndPoint}: {BitConverter.ToString(receivedBytes)}");
            LogData($"Touch detected: {string.Join(",", positions)}");
            List<int> l2 = new List<int>();

            foreach (var position in positions)
            {
                if (handler.activeDevicesGroup.ContainsKey(position))
                {
                    l2.AddRange(handler.activeDevicesGroup[position]);
                }
            }
            if (l2.Count > 0)
            {
                ChnageColorToDevice(ColorPaletteone.NoColor, l2, handler);
                updateScore(Score + l2.Count / 4);
                foreach (var item in l2)
                {
                    handler.activeDevicesGroup.Remove(item);
                }
                LogData($"Score updated: {Score} active:{string.Join(",", handler.activeDevicesGroup)}");
            }
            else if (killerRowsDict.ContainsKey(handler) && positions.Any(x => killerRowsDict[handler].Contains(x)))
            {
                isGameRunning = false;
               // musicPlayer.PlayEffect("content/you_failed.mp3");
                LogData($"Game Failed : {Score} position:{string.Join(",", positions)} killerRow : {string.Join(",", killerRowsDict[handler])}");
                killerRowsDict[handler].Clear();
                base.Score--;
                TargetTimeElapsed(null);
                return;
            }
        }

        LogData($"{handler.name} processing received data");
        if (udpHandlers.Where(x => x.activeDevicesGroup.Count > 0).Count() == 0)
        {
            MoveToNextIteration();
        }
        else
        {
            handler.BeginReceive(data => ReceiveCallback(data, handler));
        }
    }
}