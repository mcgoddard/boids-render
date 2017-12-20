using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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
    private Dictionary<long, GameObject> objects;
    private Thread readThread;
    private Process boidsProc;
    private bool running = true;

	// Use this for initialization
	void Start ()
    {
        newObjects = new Queue<Dictionary<string, object>>();
        objects = new Dictionary<long, GameObject>();
        udp = new UdpClient(4794);
        readThread = new Thread(Listener);
        readThread.Start();
        boidsProc = new Process();
        boidsProc.StartInfo.UseShellExecute = false;
        boidsProc.StartInfo.FileName = "rust-boids.exe";
        boidsProc.StartInfo.CreateNoWindow = true;
        boidsProc.Start();
    }
	
	// Update is called once per frame
	void Update ()
    {
        while (newObjects.Count > 0)
        {
            var newObject = newObjects.Dequeue();
            if (newObject != null && newObject.ContainsKey("id") && newObject["id"] != null)
            {
                var id = (long)newObject["id"];
                if (!objects.ContainsKey(id))
                {
                    objects.Add((long)newObject["id"], InitialiseBoid((string)newObject["colour"]));
                }
                var newPos = (IDictionary<string, JToken>)newObject["position"];
                var newDirection = (IDictionary<string, JToken>)newObject["direction"];
                try
                {
                    float x = newPos["x"].Value<float>();
                    float y = newPos["y"].Value<float>();
                    float z = newPos["z"].Value<float>();
                    Vector3 newPosVec = new Vector3(x, y, z);
                    objects[id].transform.position = newPosVec;
                    float xD = newDirection["x"].Value<float>();
                    float yD = newDirection["y"].Value<float>();
                    float zD = newDirection["z"].Value<float>();
                    objects[id].transform.rotation = Quaternion.Euler(xD, yD, zD);
                }
                catch (Exception) { }
            }
        }
    }

    // Called during shutdown
    void OnDestroy()
    {
        running = false;
        boidsProc.Kill();
    }

    private void Listener()
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
                newObjects.Enqueue(deserialisedObj);
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
        if (colour.Equals("Blue"))
        {
            newBoid = GameObject.Instantiate(BlueBoid);
        }
        if (colour.Equals("Red"))
        {
            newBoid = GameObject.Instantiate(RedBoid);
        }
        if (colour.Equals("Orange"))
        {
            newBoid = GameObject.Instantiate(OrangeBoid);
        }
        if (colour.Equals("Purple"))
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
