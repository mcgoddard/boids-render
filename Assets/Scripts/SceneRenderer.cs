using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class SceneRenderer : MonoBehaviour {
    public GameObject GreenBoid;
    public GameObject BlueBoid;
    public GameObject RedBoid;
    public GameObject OrangeBoid;
    public GameObject PurpleBoid;
    public GameObject YellowBoid;

    private UdpClient udp;
    private Queue<Dictionary<string, object>> newObjects;
    private List<GameObject> objects;
    private Dictionary<long, Dictionary<string, object>> states;
    private Thread readThread;
    private Thread stdoutThread;
    private Process boidsProc;
    private bool running = true;
    private float updateCounter = 0.0f;
    private int updatesReceived = 0;

	// Use this for initialization
	void Start ()
    {
        newObjects = new Queue<Dictionary<string, object>>();
        objects = new List<GameObject>();
        states = new Dictionary<long, Dictionary<string, object>>();
        udp = new UdpClient(4794);
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
        boidsProc.Start();
        stdoutThread = new Thread(StdoutListener);
        stdoutThread.Start();
    }
	
	// Update is called once per frame
	void Update ()
    {
        updateCounter += Time.deltaTime;
        if (updateCounter > 10.0f)
        {
            UnityEngine.Debug.Log(String.Format("Average updates per second: {0}", updatesReceived / 10.0));
            updateCounter = 0.0f;
            updatesReceived = 0;
        }
        while (newObjects.Count > 0)
        {
            var newObject = newObjects.Dequeue();
            if (newObject != null && newObject.ContainsKey("id") && newObject["id"] != null)
            {
                var id = (long)newObject["id"];
                if (!states.ContainsKey(id))
                {
                    var newBoid = InitialiseBoid((string)newObject["colour"]);
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
            var newPos = (IDictionary<string, JToken>)state["position"];
            var newDirection = (IDictionary<string, JToken>)state["direction"];
            try
            {
                float x = newPos["x"].Value<float>();
                float y = newPos["y"].Value<float>();
                float z = newPos["z"].Value<float>();
                Vector3 newPosVec = new Vector3(x, y, z);
                boid.transform.position = newPosVec;
                float xD = newDirection["x"].Value<float>();
                float yD = newDirection["y"].Value<float>();
                float zD = newDirection["z"].Value<float>();
                Vector3 newDirectionVec = new Vector3(xD, yD, zD);
                boid.transform.rotation = Quaternion.LookRotation(newDirectionVec, Vector3.up);
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
            string serialisedObj = System.Text.Encoding.Default.GetString(receiveBytes);
            Dictionary<string, object> deserialisedObj = JsonConvert.DeserializeObject<Dictionary<string, object>>(serialisedObj);
            if (deserialisedObj.ContainsKey("id"))
            {
                long id = (long)deserialisedObj["id"];
                if (states.ContainsKey(id))
                {
                    states[id] = deserialisedObj;
                }
                else
                {
                    newObjects.Enqueue(deserialisedObj);
                }
                updatesReceived++;
            }
        }
    }

    private void StdoutListener()
    {
        var stdoutStream = boidsProc.StandardOutput;
        while (running)
        {
            var msg = String.Format("rust-boids: {0}", stdoutStream.ReadLine());
            UnityEngine.Debug.Log(msg);
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
