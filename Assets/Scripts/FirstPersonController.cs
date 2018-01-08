using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FirstPersonController : MonoBehaviour {
    public volatile float forwardSpeed;
    public volatile float straffeSpeed;
    public Vector2 mouseInput;

    private const float sensitivity = 5.0f;

	// Use this for initialization
	void Start ()
    {
        Cursor.lockState = CursorLockMode.Locked;
	}
	
	// Update is called once per frame
	void Update ()
    {
        mouseInput = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
        forwardSpeed = Input.GetAxis("Vertical");
        straffeSpeed = Input.GetAxis("Horizontal");
        // Toggle cursor lock
        if (Input.GetKeyDown("l"))
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
            }
        }
        // Check for exit to menu
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SceneManager.LoadScene("menu", LoadSceneMode.Single);
        }
    }

    // Called during shutdown
    void OnDestroy()
    {
        Cursor.lockState = CursorLockMode.None;
    }
}
