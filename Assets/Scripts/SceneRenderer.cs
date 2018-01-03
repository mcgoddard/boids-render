using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Newtonsoft.Json.Linq;
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

    // Use this for initialization
    void Start ()
    {
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
        sim = FFIBridge.newSim();
        //var cpuCount = Environment.ProcessorCount;
        //uint collectThreadNum = (uint)(cpuCount >= 2 ? cpuCount - 1 : 1);
        while (running)
        {
            // Step the engine forward once
            UIntPtr result = FFIBridge.step(sim);
            Dictionary<Int32, Boid> newState = new Dictionary<Int32, Boid>();
            //Task[] collectionTasks = new Task[collectThreadNum];
            //for (uint i = 0; i < collectThreadNum; i++)
            //{
            //    collectionTasks[i] = Task.Run(() => StateCollector(ref newState, (uint)result, i, collectThreadNum));
            //}
            //Task.WaitAll(collectionTasks);
            for (uint i = 0; i < (uint)result; i++)
            {
                Boid b = FFIBridge.getBoid(sim, (UIntPtr)i);
                newState.Add(b.id, b);
            }
            //UnityEngine.Debug.Log(String.Format("Number of states collected: {0}", newState.Count));
            states = newState;
            engineSteps++;
        }
        UnityEngine.Debug.Log("Closing state reader thread...");
    }

    private void StateCollector(ref Dictionary<Int32, Boid> newState, uint max, uint start, uint offset)
    {
        for (uint i = start; i < max; i += offset)
        {
            try
            {
                Boid b = FFIBridge.getBoid(sim, (UIntPtr)i);
                newState.Add(b.id, b);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.Log(String.Format("Collector task exited with exception: {0}", ex.Message));
            }
        }
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
}
