﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class CoolDown
{
    private bool _flag;

    public bool Flag => _flag;

    // Method to set the flag to true for a specified duration (in milliseconds)
    public async Task SetFlagTrue(int durationInMilliseconds)
    {
        _flag = true;
        Console.WriteLine("Cool Down Set for " + durationInMilliseconds.ToString() + " ms");

        await Task.Delay(durationInMilliseconds);

        _flag = false;
        Console.WriteLine("CoolDown completed");
    }
}