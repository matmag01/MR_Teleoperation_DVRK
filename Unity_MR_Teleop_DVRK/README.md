# Unity scene

- Ensure the IP address in Unity ROS setting is correct, go to ```Robotics->ROS Settings->ROS IP Address``` and set the IP address of dVRK station.
- Enable the [Holographic Remoting](https://learn.microsoft.com/en-us/windows/mixed-reality/develop/native/holographic-remoting-overview) to stream holographic content to your HoloLens in real time.
- Change the left camera calibration matrix in  ```Hierarchy -> TipPositionregistration -> TipVisualNew.cs```

## Useful Note
- Result of Hand-Eye registration can be seen by selecting the flag in  ```Hierarchy -> Video -> img.cs```
  
