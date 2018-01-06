using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Runtime.InteropServices;
using UnityEngine;
using System.Threading.Tasks;
using System.Linq;

public class SceneRenderer : MonoBehaviour {
    public GameObject GreenBoid;
    public GameObject BlueBoid;
    public GameObject RedBoid;
    public GameObject OrangeBoid;
    public GameObject PurpleBoid;
    public GameObject YellowBoid;

    private float updateCounter = 0.0f;
    private int updatesCalled = 0;
    private Dictionary<Int32, GameObject> objects;
    private Thread stateReaderThread;
    private volatile Dictionary<Int32, Boid> states;
    private volatile bool running = true;
    private volatile int engineSteps = 0;
    private volatile UIntPtr sim;
    private volatile UIntPtr boidCount;

    // Use this for initialization
    void Start ()
    {
        boidCount = (UIntPtr)PlayerPrefs.GetInt(MenuHandler.countKey, MenuHandler.defaultCount);
        objects = new Dictionary<Int32, GameObject>();
        states = new Dictionary<Int32, Boid>();
        stateReaderThread = new Thread(StateReader);
        stateReaderThread.Start();
    }
	
	// Update is called once per frame
	void Update ()
    {
        // Benchmarking debug
        updateCounter += Time.deltaTime;
        updatesCalled += 1;
        if (updateCounter > 10.0f)
        {
            UnityEngine.Debug.Log(String.Format("Average frames processed per second: {0}", updatesCalled / 10.0));
            UnityEngine.Debug.Log(String.Format("Average engine steps per second: {0}", engineSteps / 10.0));
            updateCounter = 0;
            updatesCalled = 0;
            engineSteps = 0;
        }
        // Perform update
        var renderStates = states;
        foreach (var id in renderStates.Keys)
        {
            if (!objects.ContainsKey(id))
            {
                var newBoid = InitialiseBoid(renderStates[id].colour);
                objects.Add(id, newBoid);
            }
            objects[id].transform.position = renderStates[id].position;
            objects[id].transform.rotation = Quaternion.LookRotation(renderStates[id].direction, Vector3.up);
        }
    }

    private void StateReader()
    {
        sim = FFIBridge.newSim(boidCount);
        Stopwatch stopWatch = new Stopwatch();
        stopWatch.Start();
        float timeStep = 0;
        while (running)
        {
            // Step the engine forward once
            UIntPtr result = FFIBridge.step(sim, timeStep);
            Dictionary<Int32, Boid> newState = new Dictionary<Int32, Boid>();
            int gathererCount;
            if (Environment.ProcessorCount < 2)
            {
                gathererCount = 1;
            }
            else
            {
                gathererCount = Environment.ProcessorCount - 1;
            }
            var taskCount = (uint)result / gathererCount;
            Task<Dictionary<Int32, Boid>>[] gatheredStates = new Task<Dictionary<Int32, Boid>>[gathererCount];
            for (int i = 0; i < gathererCount; i++)
            {
                uint start = (uint)(i * taskCount);
                uint end = (uint)(taskCount * (i + 1));
                if (i == gathererCount - 1)
                {
                    end = (uint)result;
                }
                gatheredStates[i] = Task.Run(() => stateGatherer(sim, start, end));
            }
            Task.WaitAll(gatheredStates);
            newState = gatheredStates.Select(t => t.Result).SelectMany(dict => dict)
                         .ToDictionary(pair => pair.Key, pair => pair.Value);
            states = newState;
            engineSteps++;
            long elaspsedTime = stopWatch.ElapsedMilliseconds;
            if (elaspsedTime < 5)
            {
                Thread.Sleep((int)(5 - elaspsedTime));
            }
            timeStep = stopWatch.ElapsedMilliseconds / 1000.0f;
            stopWatch.Restart();
        }
        UnityEngine.Debug.Log("Closing state reader thread...");
    }

    // Called during shutdown
    void OnDestroy()
    {
        // Flag the program as shutting down, wait for the reader to exit, then free the Rust memory
        running = false;
        while (stateReaderThread.IsAlive) { }
        FFIBridge.destroySim(sim);
    }

    // Helper function to create a new boid GameObject in the correct colour
    private GameObject InitialiseBoid(BoidColourKind colour)
    {
        GameObject newBoid;
        switch (colour)
        {
            case BoidColourKind.Green:
                newBoid = GameObject.Instantiate(GreenBoid);
                break;
            case BoidColourKind.Blue:
                newBoid = GameObject.Instantiate(BlueBoid);
                break;
            case BoidColourKind.Red:
                newBoid = GameObject.Instantiate(RedBoid);
                break;
            case BoidColourKind.Orange:
                newBoid = GameObject.Instantiate(OrangeBoid);
                break;
            case BoidColourKind.Purple:
                newBoid = GameObject.Instantiate(PurpleBoid);
                break;
            case BoidColourKind.Yellow:
            default:
                newBoid = GameObject.Instantiate(YellowBoid);
                break;
        }
        return newBoid;
    }

    private Dictionary<Int32, Boid> stateGatherer(UIntPtr sim, uint start, uint end)
    {
        Dictionary<Int32, Boid> states = new Dictionary<int, Boid>();
        for (uint i = start; i < end; i++)
        {
            Boid b = FFIBridge.getBoid(sim, (UIntPtr)i);
            states.Add(b.id, b);
        }
        return states;
    }
}
