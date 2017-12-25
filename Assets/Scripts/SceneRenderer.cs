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
using UnityEngine;
using System.Threading.Collections;

public class SceneRenderer : MonoBehaviour {
    public GameObject GreenBoid;
    public GameObject BlueBoid;
    public GameObject RedBoid;
    public GameObject OrangeBoid;
    public GameObject PurpleBoid;
    public GameObject YellowBoid;

    private UdpClient udp;
    private ConcurrentQueue<Boid> newObjects;
    private ConcurrentQueue<byte[]> receivedBytes;
    private List<GameObject> objects;
    private Dictionary<long, Boid> states;
    private Thread readThread;
    private Thread stdoutThread;
    private Thread stderrThread;
    private Thread[] deserializers = new Thread[2];
    private Process boidsProc;
    private bool running = true;
    private float updateCounter = 0.0f;
    private int updatesReceived = 0;
    private int updatesCalled = 0;
    private int updatesDeserialized = 0;
    private class Boid 
    {
        public long id { get; set; }
        public string colour { get; set; }
        public Vector3 position { get; set; }
        public Vector3 direction { get; set; }
    }

	// Use this for initialization
	void Start ()
    {
        receivedBytes = new ConcurrentQueue<byte[]>();
        newObjects = new ConcurrentQueue<Boid>();
        objects = new List<GameObject>();
        states = new Dictionary<long, Boid>();
        udp = new UdpClient(4794);
        for (int i = 0; i < deserializers.Length; i++)
        {
            deserializers[i] = new Thread(Deserizer);
            deserializers[i].Start();
        }
        readThread = new Thread(UdpListener);
        readThread.Start();
        boidsProc = new Process();
        boidsProc.StartInfo.UseShellExecute = false;
        if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor)
        {
            boidsProc.StartInfo.FileName = "boids-builds/windows/rust-boids.exe";
        }
        else if (Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.OSXEditor)
        {
            boidsProc.StartInfo.FileName = "boids-builds/osx/rust-boids";
        }
        else
        {
            throw new Exception(String.Format("No boids build available for unknown platform: {0}", Application.platform));
        }
        boidsProc.StartInfo.CreateNoWindow = true;
        boidsProc.StartInfo.RedirectStandardOutput = true;
        boidsProc.StartInfo.RedirectStandardError = true;
        boidsProc.Start();
        stdoutThread = new Thread(StdoutListener);
        stdoutThread.Start();
        stderrThread = new Thread(StderrListener);
        stderrThread.Start();
    }
	
	// Update is called once per frame
	void Update ()
    {
        updateCounter += Time.deltaTime;
        updatesCalled += 1;
        if (updateCounter > 10.0f)
        {
            UnityEngine.Debug.Log(String.Format("Average states received per second: {0}", updatesReceived / 10.0));
            UnityEngine.Debug.Log(String.Format("Average states deserialized per second: {0}", updatesDeserialized / 10.0));
            UnityEngine.Debug.Log(String.Format("Average frames processed per second: {0}", updatesCalled / 10.0));
            updateCounter = 0.0f;
            updatesReceived = 0;
            updatesCalled = 0;
            updatesDeserialized = 0;
        }
        while (newObjects.Count > 0)
        {
            Boid newObject;
            if (newObjects.TryDequeue(out newObject))
            {
                var id = newObject.id;
                if (!states.ContainsKey(id))
                {
                    var newBoid = InitialiseBoid(newObject.colour);
                    newBoid.name = id.ToString();
                    objects.Add(newBoid);
                    states.Add(id, newObject);
                }
            }
        }
        foreach (var boid in objects)
        {
            long id = long.Parse(boid.name);
            var state = states[id];
            var newPos = state.position;
            var newDirection = state.direction;
            try
            {
                boid.transform.position = newPos;
                boid.transform.rotation = Quaternion.LookRotation(newDirection, Vector3.up);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.Log(String.Format("Exception caught processing boid:\n{0}", ex.Message));
            }
        }
    }

    // Called during shutdown
    void OnDestroy()
    {
        running = false;
        if (boidsProc != null && !boidsProc.HasExited)
        {
            boidsProc.Kill();
        }
    }

    private void UdpListener()
    {
        while (running)
        {
            IPEndPoint endpoint = new IPEndPoint(IPAddress.Loopback, 0);
            byte[] receiveBytes = udp.Receive(ref endpoint);
            if (receiveBytes != null && receiveBytes.Length > 0)
            {
                receivedBytes.Enqueue(receiveBytes);
                updatesReceived++;
            }
        }
    }

    private void Deserizer()
    {
        while (running)
        {
            try
            {
                byte[] receiveBytes;
                if (receivedBytes.TryDequeue(out receiveBytes))
                {
                    string serialisedObj = System.Text.Encoding.Default.GetString(receiveBytes);
                    if (!String.IsNullOrEmpty(serialisedObj))
                    {
                        var deserialisedObj = JsonConvert.DeserializeObject<Boid>(serialisedObj);
                        if (deserialisedObj != null)
                        {
                            long id = deserialisedObj.id;
                            if (states.ContainsKey(id))
                            {
                                states[id] = deserialisedObj;
                            }
                            else
                            {
                                newObjects.Enqueue(deserialisedObj);
                            }
                            updatesDeserialized++;
                        }
                    }
                }
            }
            catch (InvalidOperationException)
            {
                // Ignore (queue is empty)
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.Log(String.Format("Error in deserializer: {0}", ex.StackTrace));
            }
        }
    }

    private void StdoutListener()
    {
        var stdoutStream = boidsProc.StandardOutput;
        while (running)
        {
            var msg = stdoutStream.ReadLine();
            if (!String.IsNullOrEmpty(msg))
            {
                UnityEngine.Debug.Log(String.Format("rust-boids: stdout: {0}", msg));
            }
        }
    }

    private void StderrListener()
    {
        var stderrStream = boidsProc.StandardError;
        while (running)
        {
            var msg = stderrStream.ReadLine();
            if (!String.IsNullOrEmpty(msg))
            {
                UnityEngine.Debug.Log(String.Format("rust-boids: stderr: {0}", msg));
            }
        }
    }

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
