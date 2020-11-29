using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WebCam : MonoBehaviour {

  WebCamTexture cam;

  // neeeds to run with external plugin and a registry patch!
  // https://obsproject.com/forum/resources/obs-virtualcam.949/
  // builtin OBS v26 virtual cam doesnt work



  // Use this for initialization
  void Start () {

    WebCamDevice[] devices = WebCamTexture.devices;
    for (int i = 0; i < devices.Length; i++)
      Debug.Log(devices[i].name);
    
    //cam = new WebCamTexture("HD Pro Webcam C920", 800, 600, 60);
    //cam = new WebCamTexture("OBS Virtual Camera", 1600, 1200, 30); // not working with unity
    cam = new WebCamTexture("OBS-Camera", 800, 600, 30);

    GetComponent<Renderer>().material.SetTexture("_EmissionMap", cam);
    cam.Play();
  }
	
	// Update is called once per frame
	void Update () {
		
	}

  void OnDestroy()
  {
    if (cam == null) return; 
    if(cam.isPlaying) cam.Stop();
  }
}
