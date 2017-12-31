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

public class SceneRenderer : MonoBehaviour {
    public GameObject GreenBoid;
    public GameObject BlueBoid;
    public GameObject RedBoid;
    public GameObject OrangeBoid;
    public GameObject PurpleBoid;
    public GameObject YellowBoid;

    private List<GameObject> objects;
    private Dictionary<long, Boid> states;
    private bool running = true;
    private float updateCounter = 0.0f;
    private int updatesCalled = 0;
    private UIntPtr sim;

    [StructLayout(LayoutKind.Sequential)]
    public struct Boid
    {
        public long id { get; set; }
        public string colour { get; set; }
        public Vector3 position { get; set; }
        public Vector3 direction { get; set; }
    }

    [DllImport("librustboidslib")]
    private static extern UIntPtr newSim();

    [DllImport("librustboidslib")]
    private static extern UIntPtr step(UIntPtr sim);

    [DllImport("librustboidslib")]
    private static extern void destroySim(UIntPtr sim);

	// Use this for initialization
	void Start ()
    {
        objects = new List<GameObject>();
        states = new Dictionary<long, Boid>();
        sim = newSim();
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
            updateCounter = 0;
            updatesCalled = 0;
        }
        // Step the engine forward once
        UIntPtr result = step(sim);
    }

    // Called during shutdown
    void OnDestroy()
    {
        running = false;
        destroySim(sim);
    }

    // Helper function to create a new boid GameObject in the correct colour
    private GameObject InitialiseBoid(string colour)
    {
        GameObject newBoid;
        if (colour.Equals("Green"))
        {
            newBoid = GameObject.Instantiate(GreenBoid);
        }
        else if (colour.Equals("Blue"))
        {
            newBoid = GameObject.Instantiate(BlueBoid);
        }
        else if (colour.Equals("Red"))
        {
            newBoid = GameObject.Instantiate(RedBoid);
        }
        else if (colour.Equals("Orange"))
        {
            newBoid = GameObject.Instantiate(OrangeBoid);
        }
        else if (colour.Equals("Purple"))
        {
            newBoid = GameObject.Instantiate(PurpleBoid);
        }
        else
        {
            newBoid = GameObject.Instantiate(YellowBoid);
        }
        return newBoid;
    }
}
