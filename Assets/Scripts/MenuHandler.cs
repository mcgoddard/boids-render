using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuHandler : MonoBehaviour
{
    public static readonly string countKey = "boidsCount";
    public static readonly int defaultCount = 500;

    public Text countField;
    public Image countFieldBackground;

    private int boidNum = defaultCount;

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
            PlayerPrefs.SetInt(countKey, boidNum);
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
