using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuHandler : MonoBehaviour
{
    public Text countField;
    public Image countFieldBackground;

    private int boidNum = 500;

	// Use this for initialization
	void Start ()
    {
	}
	
	// Update is called once per frame
	void Update ()
    {
	}
    
    public void StartClick()
    {
        if (boidNum < 0 || boidNum > 10000)
        {
            countFieldBackground.color = Color.red;
        }
        else
        {
            PlayerPrefs.SetInt("boidsCount", boidNum);
            Debug.Log(String.Format("Starting with {0} boids", boidNum));
            SceneManager.LoadScene("boids-render", LoadSceneMode.Single);
        }
    }

    public void BoidsCountChanged()
    {
        string text = countField.text;
        int newCount;
        if (int.TryParse(text, out newCount))
        {
            countFieldBackground.color = Color.white;
            boidNum = newCount;
        }
        else
        {
            countFieldBackground.color = Color.red;
            countField.text = countField.ToString();
        }
    }
}
