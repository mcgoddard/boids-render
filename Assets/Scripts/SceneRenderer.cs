using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
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

    private IPEndPoint endpoint = new IPEndPoint(IPAddress.Loopback, 4794);
    private UdpClient udp;
    private Dictionary<int, GameObject> objects;
    private Thread readThread;
    private bool running = true;

	// Use this for initialization
	void Start ()
    {
        objects = new Dictionary<int, GameObject>();
        udp = new UdpClient();
        readThread = new Thread(Listener);
        readThread.Start();
    }
	
	// Update is called once per frame
	void Update ()
    {
    }

    // Called during shutdown
    void OnDestroy()
    {
        running = false;
    }

    private void Listener()
    {
        while (running)
        {
            byte[] receiveBytes = udp.Receive(ref endpoint);
            string serialisedObj = System.Text.Encoding.Default.GetString(receiveBytes);
            Dictionary<string, object> deserialisedObj = JsonConvert.DeserializeObject<Dictionary<string, object>>(serialisedObj);
            int id = (int)deserialisedObj["id"];
            if (!objects.ContainsKey(id))
            {
                objects[id] = InitialiseBoid((string)deserialisedObj["colour"]);
            }
            Dictionary<string, float> newPos = (Dictionary<string, float>)deserialisedObj["position"];
            Vector3 newPosVec = new Vector3(newPos["x"], newPos["y"], newPos["z"]);
            objects[id].transform.position = newPosVec;
            Dictionary<string, float> newDir = (Dictionary<string, float>)deserialisedObj["direction"];
            objects[id].transform.rotation = Quaternion.Euler(newDir["x"], newDir["y"], newDir["z"]);
        }
    }

    private GameObject InitialiseBoid(string colour)
    {
        GameObject newBoid;
        if (colour.Equals("green"))
        {
            newBoid = GameObject.Instantiate(GreenBoid);
        }
        if (colour.Equals("blue"))
        {
            newBoid = GameObject.Instantiate(BlueBoid);
        }
        if (colour.Equals("red"))
        {
            newBoid = GameObject.Instantiate(RedBoid);
        }
        if (colour.Equals("orange"))
        {
            newBoid = GameObject.Instantiate(OrangeBoid);
        }
        if (colour.Equals("purple"))
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
