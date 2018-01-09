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
    public GameObject Player;
    public GameObject Ground;
    public GameObject Wall;
    public GameObject Tree;

    private float updateCounter = 0.0f;
    private int updatesCalled = 0;
    private Dictionary<UInt64, GameObject> objects;
    private Thread stateReaderThread;
    private volatile Dictionary<UInt64, ReturnObj> states;
    private volatile bool running = true;
    private volatile int engineSteps = 0;
    private volatile UIntPtr sim;
    private volatile UIntPtr boidCount;
    private volatile FirstPersonController player;
    private UInt64 playerId;

    // Use this for initialization
    void Start ()
    {
        boidCount = (UIntPtr)PlayerPrefs.GetInt(MenuHandler.countKey, MenuHandler.defaultCount);
        objects = new Dictionary<UInt64, GameObject>();
        states = new Dictionary<UInt64, ReturnObj>();
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
            // Add the object if it doesn't exist
            if (!objects.ContainsKey(id))
            {
                GameObject newObject = InitialiseGameObject(renderStates[id]);
                objects.Add(id, newObject);
            }
            // Process the object's state
            ProcessObject(id, renderStates);
        }
    }

    // Function to be fun off of the main thread that will tight loop the fungine and collect the states back from it
    private void StateReader()
    {
        sim = FFIBridge.newSim(boidCount);
        Stopwatch stopWatch = new Stopwatch();
        stopWatch.Start();
        float timeStep = 0;
        while (running)
        {
            // Check for player input
            if (player != null)
            {
                FFIBridge.addMovement(sim, playerId, player.forwardSpeed, player.straffeSpeed, player.mouseInput);
            }
            // Step the engine forward once
            UIntPtr result = FFIBridge.step(sim, timeStep);
            Dictionary<UInt64, ReturnObj> newState = new Dictionary<UInt64, ReturnObj>();
            UInt64 gathererCount;
            if (Environment.ProcessorCount < 2)
            {
                gathererCount = 1;
            }
            else
            {
                gathererCount = (UInt64)Environment.ProcessorCount - 1;
            }
            var taskCount = (UInt64)result / gathererCount;
            Task<Dictionary<UInt64, ReturnObj>>[] gatheredStates = new Task<Dictionary<UInt64, ReturnObj>>[gathererCount];
            for (UInt64 i = 0; i < gathererCount; i++)
            {
                UInt64 start = (i * taskCount);
                UInt64 end = (taskCount * (i + 1));
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

    // Process an updated state from the fungine
    private void ProcessObject(UInt64 id, Dictionary<UInt64, ReturnObj> renderStates)
    {
        switch (renderStates[id].objType)
        {
            case ObjType.Player:
                objects[id].transform.position = renderStates[id].player.position;
                objects[id].transform.rotation = Quaternion.LookRotation(renderStates[id].player.direction, Vector3.up);
                break;
            case ObjType.Boid:
                objects[id].transform.position = renderStates[id].boid.position;
                objects[id].transform.rotation = Quaternion.LookRotation(renderStates[id].boid.direction, Vector3.up);
                break;
            case ObjType.NoObj:
            default:
                // Do nothing
                break;
        }
    }

    // Initialise a new Unity GameObject from the state from the fungine
    private GameObject InitialiseGameObject(ReturnObj obj)
    {
        GameObject newObject;
        switch (obj.objType)
        {
            case ObjType.Boid:
                newObject = InitialiseBoid(obj.boid.colour);
                break;
            case ObjType.Player:
                newObject = InitialisePlayer();
                playerId = obj.id;
                player = newObject.GetComponent<FirstPersonController>();
                break;
            case ObjType.NoObj:
            default:
                newObject = null;
                break;
        }
        return newObject;
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

    // Helper function to create a new player GameObject
    private GameObject InitialisePlayer()
    {
        return GameObject.Instantiate(Player);
    }

    // Gather object states from the fungine
    private Dictionary<UInt64, ReturnObj> stateGatherer(UIntPtr sim, UInt64 start, UInt64 end)
    {
        Dictionary<UInt64, ReturnObj> states = new Dictionary<UInt64, ReturnObj>();
        for (UInt64 i = start; i < end; i++)
        {
            ReturnObj b = FFIBridge.getObj(sim, (UIntPtr)i);
            states.Add(b.id, b);
        }
        return states;
    }
}
