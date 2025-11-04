# Unity scene

- Enable the [Holographic Remoting](https://learn.microsoft.com/en-us/windows/mixed-reality/develop/native/holographic-remoting-overview) to stream holographic content to your HoloLens in real time.
- Change the left camera calibration matrix in  ```Hierarchy -> TipPositionregistration -> TipVisualNew.cs --> start function```

## Useful Note
- Result of Hand-Eye registration can be seen by selecting the flag in  ```img.cs```. Ensure the IP address in Unity ROS setting is correct, go to ```Robotics->ROS Settings->ROS IP Address``` and set the IP address of dVRK station. 
- In ```Assets --> Logs``` you can find a .csv file with the orientation of hand vs orientation of the instrument. The name of the file can be changed in ```Hierarchy -> Main -> StartingScript.cs```

## Citation

The interface and the organization of the scene are inspired from Ai et Al. (2024) and Chen et Al. (2023).
