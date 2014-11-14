using UnityEngine;
using System.Collections;

public class GuiControl : MonoBehaviour {

    float Magnitude;
    float Height;
    float Speed;
    int Size;
    float Scale;
    public GameObject Ocean;
    Plane oceanPlane;

	// Use this for initialization
	void Start () {
        oceanPlane = Ocean.GetComponent<Plane>();
        Magnitude = oceanPlane.Magnitude;
        Height = oceanPlane.Height;
        Speed = oceanPlane.Speed;
        Size = oceanPlane.LengthWidth;
        Scale = oceanPlane.PlaneScale;

	}
	
	// Update is called once per frame
	void Update () {
	
	}

    void OnGUI()
    {
        if (GUI.Button(new Rect(10, 10, 32, 32), "-"))
            Magnitude -= 0.01f;
        if (GUI.Button(new Rect(42, 10, 32, 32), "+"))
            Magnitude += 0.01f;
        GUI.Box(new Rect(74, 10, 128, 32), "Magnitude = " + Magnitude);

        if (GUI.Button(new Rect(10, 42, 32, 32), "-"))
            Height -= 0.01f;
        if (GUI.Button(new Rect(42, 42 , 32, 32), "+"))
            Height += 0.01f;
        GUI.Box(new Rect(74, 42, 128, 32), "Height = " + Height);

        if (GUI.Button(new Rect(10, 74, 32, 32), "-"))
            Speed--;
        if (GUI.Button(new Rect(42, 74, 32, 32), "+"))
            Speed++;
        GUI.Box(new Rect(74, 74, 128, 32), "Speed = " + Speed);

        if (GUI.Button(new Rect(10, 106, 32, 32), "-"))
            Scale -= 0.1f;
        if (GUI.Button(new Rect(42, 106, 32, 32), "+"))
            Scale += 0.1f;
        GUI.Box(new Rect(74, 106, 128, 32), "Scale = " + Scale);

        if (GUI.Button(new Rect(10, 138, 32, 32), "-"))
            Size /= 2;
        if (GUI.Button(new Rect(42, 138, 32, 32), "+"))
            Size *= 2;
        GUI.Box(new Rect(74, 138, 128, 32), "Size = " + Size);

        if (Magnitude != oceanPlane.Magnitude)
            oceanPlane.Magnitude = Magnitude;
        if (Height != oceanPlane.Height)
            oceanPlane.Height = Height;
        if (Speed != oceanPlane.Speed)
            oceanPlane.Speed = Speed;
        if (Scale != oceanPlane.PlaneScale)
            oceanPlane.PlaneScale = Scale;
        if (Size != oceanPlane.LengthWidth)
            oceanPlane.LengthWidth = Size;
    }
}
