﻿using NAudio.Wave;
using scorecard.lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Schema;

namespace scorecard
{
    public class BaseGameClimb : BaseGame
    {
        protected Dictionary<int, Mapping> deviceMapping;
        protected int rows = 0;
        protected Dictionary<int, List<int>> surroundingMap;
        private List<UdpHandlerWeTop> udpHandlersWeTop;
        public BaseGameClimb(GameConfig config) : base(config)
        {

            deviceMapping = new Dictionary<int, Mapping>();
            int k = 0;
            foreach (var handler in udpHandlersWeTop)
            {
                for (int i = 0; i < handler.DeviceList.Count; i++)
                {
                    deviceMapping.Add(k, new Mapping(handler, false, Resequencer(i)));
                    k += 1;
                }
            }
            foreach (var handler in udpHandlersWeTop)
            {
                rows += handler.Rows;
            }
            surroundingMap = SurroundingMap.CreateSurroundingTilesDictionary(config.columns, rows, 3);
            logger.Log($"surrounding map created {surroundingMap.Count}");
            logger.Log($"device mapping created {deviceMapping.Count}");
        }




        protected void AnimateColor(bool reverse)
        {

            foreach (var handler in udpHandlersWeTop)
            {
                for (int iterations = 0; iterations < handler.Rows; iterations++)
                {
                    for (int i = 0; i < handler.DeviceList.Count; i++)
                    {
                        handler.DeviceList[i] = ColorPaletteone.Red;
                    }

                    int row = (iterations / handler.Rows) % 2 == 0 ? (iterations % handler.Rows) : handler.Rows - 1 - (iterations % handler.Rows);

                    if (reverse)
                    {
                        row = handler.Rows - row - 1;
                    }

                    for (int i = 0; i < config.columns; i++)
                    {
                        handler.DeviceList[row * config.columns + i] = ColorPaletteone.Green;
                    }

                    handler.SendColorsToUdp(handler.DeviceList);
                    Thread.Sleep(75);
                }
            }
        }

        protected override async void StartAnimition()
        {
            AnimateGrowth(ColorPaletteone.Pink);
        }
        protected void AnimateGrowth(string color)
        {

            //            foreach (var handler inudpHandlersWeTop)
            {
                var handler =udpHandlersWeTop[0];
                int totalLights = handler.DeviceList.Count *udpHandlersWeTop.Count;
                List<int> unchanged = new List<int>(Enumerable.Range(0, handler.DeviceList.Count *udpHandlersWeTop.Count));
                for (double i = 0.00; i < totalLights && isGameRunning; i++)
                {
                    int random = new Random().Next(0, unchanged.Count);
                    int handlerIndex = unchanged[random] / handler.DeviceList.Count;
                    int position = unchanged[random] % handler.DeviceList.Count;
                    Console.WriteLine($"index {handlerIndex} position {position} random {random}");
                   udpHandlersWeTop[handlerIndex].DeviceList[unchanged[random] - handler.DeviceList.Count * handlerIndex] = color;
                    unchanged.RemoveAt(random);

                   udpHandlersWeTop[handlerIndex].SendColorsToUdp(udpHandlers[handlerIndex].DeviceList);
                    Thread.Sleep(Convert.ToInt32(38.00 - i / 6));
                }
            }
        }

        protected int Resequencer(int index)
        {
            if ((index / config.columns) % 2 == 0)
            {
                return index;
            }

            int columns = config.columns;
            int row = index / columns;
            int column = index % columns;
            int dest = (row + 1) * columns - 1 - column;
            return dest;
        }
        protected List<string> ResequencedPositions(List<string> colorList, UdpHandler handler)
        {
            int columns = config.columns;
            int rows = handler.Rows;
            var activeIndicesList = handler.activeDevices.ToList();

            // Find and process devices in odd rows
            var oddRowIndices = activeIndicesList
                .Where(index => (index / columns) % 2 != 0)
                .ToList();

            foreach (var orig in oddRowIndices)
            {
                int row = orig / columns;
                int column = orig % columns;
                int dest = (row + 1) * columns - 1 - column;

                // Swap colors
                (colorList[orig], colorList[dest]) = (colorList[dest], colorList[orig]);

                // Update active indices
                handler.activeDevices[handler.activeDevices.IndexOf(orig)] = dest;

            }

            LogData($"repalced following ones {string.Join(",", oddRowIndices)}");
            return colorList;
        }
        protected void SendColorToUdpAsync()
        {

            var tasks = new List<Task>();
            foreach (var handler in udpHandlersWeTop)
            {
                tasks.Add(handler.SendColorsToUdpAsync(handler.DeviceList));
            }
            Task.WhenAll(tasks);


        }


        protected List<string> ResequencedPositions1(List<string> colorList, UdpHandlerWeTop handler)
        {

            Console.WriteLine($" before resequencing  {string.Join(",", handler.activeDevices)}");
            //hold indices in temp list
            var actind = handler.activeDevices.Select(x => x).ToList();


            for (int i = 0; i < handler.Rows; i++)
            {
                if (i % 2 != 0)
                {
                    for (int j = 0; j < handler.columns; j++)
                    {
                        int orig = i * handler.columns + j;
                        int dest = (i + 1) * handler.columns - 1 - j;

                        if (actind.Contains(orig))
                        {
                            // Swap colors
                            (colorList[orig], colorList[dest]) = (colorList[dest], colorList[orig]);
                            handler.activeDevices.Remove(orig);
                            handler.activeDevices.Add(dest);
                        }
                    }
                }
            }
            // activeIndices[hangler] = activeIndicesp;
            Console.WriteLine(string.Join(",", handler.activeDevices));
            return colorList;
        }

    }
}