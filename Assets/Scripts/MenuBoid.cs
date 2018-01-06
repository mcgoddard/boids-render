using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuBoid : MonoBehaviour
{
    const float ROTATE = 90;

	// Use this for initialization
	void Start ()
    {
	}
	
	// Update is called once per frame
	void Update ()
    {
        transform.Rotate(0, ROTATE * Time.deltaTime, 0);
	}
}
