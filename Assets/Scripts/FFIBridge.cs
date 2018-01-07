using System;
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
public struct BoidObj
{
    public UInt64 id;
    public Boid boid;
}

[StructLayout(LayoutKind.Sequential)]
public struct Boid
{
    public Vector3 position { get; set; }
    public Vector3 direction { get; set; }
    public BoidColourKind colour { get; set; }
    public Int32 id { get; set; }
}

public static class FFIBridge
{

    [DllImport("rustboidslib")]
    public static extern UIntPtr newSim500();

    [DllImport("rustboidslib")]
    public static extern UIntPtr newSim(UIntPtr boidCount);

    [DllImport("rustboidslib")]
    public static extern UIntPtr step(UIntPtr sim, float frameTime);

    [DllImport("rustboidslib")]
    public static extern BoidObj getBoid(UIntPtr sim, UIntPtr index);

    [DllImport("rustboidslib")]
    public static extern void destroySim(UIntPtr sim);
}
