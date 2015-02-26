using UnityEngine;
using System.Collections;
[ExecuteInEditMode]
public class WindDirController : MonoBehaviour 
{
    // public GameObject Ocean;
    // Ocean OceanScript;

    Camera cam;

    public float WindRotation = 0;

    public float WindSpeed = 50;

    public float OceanSize = 100;

    float horizontalpos;

	// Use this for initialization
	void Start () {
        horizontalpos = Screen.width - 200;
	}
	
	// Update is called once per frame
	void Update () {
        if (Input.GetMouseButton(0))
        {
            if (Input.mousePosition.x > (horizontalpos) && Input.mousePosition.y > (Screen.height - 200))
            {
                Vector2 Direction = (Input.mousePosition - transform.position ).normalized;
                //Debug.Log(Direction);
                //Debug.Log();
                WindRotation = Mathf.Atan2(Direction.y, Direction.x) * 180 / Mathf.PI - 90;
                transform.rotation = Quaternion.Euler(0, 0, WindRotation);
            }
        }
	}

    void OnGUI()
    {
        WindSpeed = GUI.HorizontalSlider(new Rect(horizontalpos, 220, 200, 20), WindSpeed, 0.01f, 10.0f);
        GUI.Box(new Rect(horizontalpos, 240, 200, 30), "Wind Speed = " + WindSpeed);
    }
}
