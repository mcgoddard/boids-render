﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public enum BoidColourKind
{
    Green = 0,
    Blue,
    Red,
    Orange,
    Purple,
    Yellow
}

[StructLayout(LayoutKind.Sequential)]
public struct Boid
{
    public Vector3 position { get; set; }
    public Vector3 direction { get; set; }
    public BoidColourKind colour { get; set; }
    public Int32 id { get; set; }
}

public static class FFIBridge {

    [DllImport("rustboidslib")]
    public static extern UIntPtr newSim();

    [DllImport("rustboidslib")]
    public static extern UIntPtr step(UIntPtr sim);

    [DllImport("rustboidslib")]
    public static extern Boid getBoid(UIntPtr sim, UIntPtr index);

    [DllImport("rustboidslib")]
    public static extern void destroySim(UIntPtr sim);
}
