using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ScreenShot : MonoBehaviour
{

  public int width;
  public int height;
  public int scale;

  // Use this for initialization
  void Start()
  {

  }


  void Update()
  {

    if (
      SteamVR_Controller.Input(1).GetPress(SteamVR_Controller.ButtonMask.Grip) &&  
      SteamVR_Controller.Input(2).GetPress(SteamVR_Controller.ButtonMask.Grip)
    ) 
    {
      takeScreenShot();
    } 

  }

  void takeScreenShot() 
  {
    int resWidthN = width * scale;
    int resHeightN = height * scale;
    RenderTexture rt = new RenderTexture(resWidthN, resHeightN, 24);
    Camera.main.targetTexture = rt;
    //QualitySettings.antiAliasing = 8; // not working

    TextureFormat tFormat;
    tFormat = TextureFormat.RGB24;


    Texture2D screenShot = new Texture2D(resWidthN, resHeightN, tFormat, false);
    Camera.main.Render();
    RenderTexture.active = rt;


    screenShot.ReadPixels(new Rect(0, 0, resWidthN, resHeightN), 0, 0);

    Camera.main.targetTexture = null;
    RenderTexture.active = null;

    byte[] bytes = screenShot.EncodeToPNG();
    string filename = ScreenShotName(resWidthN, resHeightN);

    System.IO.File.WriteAllBytes(filename, bytes);
    Debug.Log(string.Format("Took screenshot to: {0}", filename));

    //Application.OpenURL(filename);

  }

  public string ScreenShotName(int width, int height)
  {

    string strPath = "";
    string sep = "/";
    string saveDir = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments) + sep + "SoundStage" + sep + "ScreenShots";
    Directory.CreateDirectory(saveDir);

    strPath = string.Format("{0}/screen_{1}x{2}_{3}.png",
                         saveDir,
                         width*scale, height*scale,
                                   System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
    return strPath;
  }

}
