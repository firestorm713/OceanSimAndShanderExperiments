using UnityEngine;
using System.Collections;
using Cloo;
using System.IO;
using System;

public class ClooTest : MonoBehaviour {

	// Use this for initialization
	void Start () {
        int platformCount = ComputePlatform.Platforms.Count;
        Debug.Log("Found " + platformCount + " platforms");

        ComputePlatform pf = ComputePlatform.Platforms[0];

        ComputeContext ctx = new ComputeContext(ComputeDeviceTypes.Gpu, new ComputeContextPropertyList(pf), null, IntPtr.Zero);

        ComputeCommandQueue queue = new ComputeCommandQueue(ctx, ctx.Devices[0], ComputeCommandQueueFlags.None);

        

        for (var platformIndex = 0; platformIndex < platformCount; platformIndex++)
        {
            var platform = ComputePlatform.Platforms[platformIndex] as ComputePlatform;
            if (platform == null)
            {
                throw new System.Exception("Somehow got a ComputePlatform which isn't a ComputePlatform, aborting");
            }
            Debug.Log("Found platform: " + platform.Name);

            foreach (var device in platform.Devices)
            {
                Debug.Log("  Device: " + device.Name);
            }
        }
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
