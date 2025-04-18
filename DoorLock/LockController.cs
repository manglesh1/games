﻿using System;
using System.Configuration;
using System.IO.Ports;

public class LockController
{
    private SerialPort serialPort;
    private bool lockActive = true;

    public LockController(string portName)
    {
        // Initialize the SerialPort
        serialPort = new SerialPort(portName, 9600, Parity.None, 8, StopBits.One)
        {
            Handshake = Handshake.None
        };
        //HttpClient httpClient = new HttpClient(ConfigurationManager.AppSettings["server"]);
        //lockActive = await httpClient.GetAsync<bool>("config?configKey=DoorLock");

        try
        {
            serialPort.Open(); // Open the serial port
            lockActive = TestRelay();
        }
        catch (Exception ex)
        {
            lockActive = false;
        }
    }

    /// <summary>
    /// Sends an AT command to the relay controller and reads the response.
    /// </summary>
    private string? SendCommand(string command)
    {
        if (!lockActive) return null;
        if (!serialPort.IsOpen)
        {
            return null;
        }

        try
        {
            serialPort.Write(command.ToCharArray(), 0, command.Length); // Send the command
            string response = serialPort.ReadLine(); // Read the response
            //logger.Log($"Command: {command}, Response: {response}");
            return response;
        }
        catch (Exception ex)
        {
            return null;
        }
    }

    /// <summary>
    /// Turns the relay ON (closes the relay).
    /// </summary>
    public void TurnRelayOn()
    {
        if (!lockActive) return;
        SendCommand("AT+CH1=1");
    }

    /// <summary>
    /// Turns the relay OFF (opens the relay).
    /// </summary>
    public void TurnRelayOff()
    {
        if (!lockActive) return;
        SendCommand("AT+CH1=0");
    }

    /// <summary>
    /// Test the communication with the relay controller.
    /// </summary>
    public bool TestRelay()
    {
        string? response = SendCommand("AT");
        if (response != null && response.Contains("OK"))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Turns the relay ON and OFF in sequence.
    /// </summary>
    public void TurnOnAndOff()
    {
        for (int i = 0; i < 5; i++)
        {
            TurnRelayOn();
            System.Threading.Thread.Sleep(500); // Wait 500ms
            TurnRelayOff();
            System.Threading.Thread.Sleep(500); // Wait 500ms
        }
    }

    /// <summary>
    /// Closes the serial port when the object is disposed.
    /// </summary>
    public void Dispose()
    {
        if (serialPort != null && serialPort.IsOpen)
        {
            serialPort.Close();
        }
    }
}

