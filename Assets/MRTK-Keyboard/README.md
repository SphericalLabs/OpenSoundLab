# MRTK-Keyboard

This is a standalone version of the NonNative Keyboard from the MRTK project https://github.com/microsoft/MixedRealityToolkit-Unity

You can simply add the files in this repo to your Assets root in your Unity Project and start working with it.

The prefab for the keyboard is inside MRTK\SDK\Experimental\NonNativeKeyboard\Prefabs and it contains an easy to use prefab where keys are simple Unity UI buttons so it will work with regular Unity Input. Simply place the prefab in your scene to start using it. For VR or 3D scenes you'll need to decide where to display it and position it.

You can use "Keyboard.Instance.PresentKeyboard(string)" to show the keyboard from code and retrieve the input by subscribing to the "OnTextSubmitted" event inside NonNativeKeyboard.cs. This event gets triggered when pressing "Enter" in the keyboard. There are other events you might want to use, and they are all inside the  NonNativeKeyboard.cs script.

Check the scene in the project to test the keyboard with your mouse.


![Keyboard](https://user-images.githubusercontent.com/13970424/190276369-394dda79-06bc-46b0-8aeb-13c1bf538a5e.gif)
