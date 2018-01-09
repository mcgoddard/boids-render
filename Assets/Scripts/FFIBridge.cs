using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public enum ObjType
{
    Boid,
    Player,
    NoObj
}

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
}

[StructLayout(LayoutKind.Sequential)]
public struct Player
{
    public Vector3 position { get; set; }
    public Vector3 direction { get; set; }
}

[StructLayout(LayoutKind.Sequential)]
public struct ReturnObj
{
    public UInt64 id { get; set; }
    public ObjType objType { get; set; }
    public Boid boid { get; set; }
    public Player player { get; set; }
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
    public static extern ReturnObj getObj(UIntPtr sim, UIntPtr index);

    [DllImport("rustboidslib")]
    public static extern void addMovement(UIntPtr sim, UInt64 id, float forward, float strafe, Vector2 mouse);

    [DllImport("rustboidslib")]
    public static extern void destroySim(UIntPtr sim);
}
