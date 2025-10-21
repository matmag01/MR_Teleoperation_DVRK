# Mixed Reality Teleoperation of DVRK

## Introduction

This repository provides the codebase for Mixed Reality Teleoperation of the da Vinci Research Kit (dVRK) using HoloLens 2. More details about the dVRK can be found in the [official documentation](https://dvrk.readthedocs.io/main/). 

## Prerequisite

### Install dVRK software

Follow the instructions in the [dVRK software compilation guide](https://dvrk.readthedocs.io/main/pages/software/compilation/ros1.html). You have also to install [gscam](https://dvrk.readthedocs.io/main/pages/video/software/ros.html#using-gscam) for video streaming support.

### Install ROS - TCP Endpoint
Clone and build the Unity ROS–TCP Endpoint:
```
cd ~/catkin_ws/src
git clone https://github.com/Unity-Technologies/ROS-TCP-Endpoint.git
catkin build
```

### Configuration

- Navigate to the folder ```/catkin_ws/src/cisst-saw/sawIntuitiveResearchKit/share/socket-streamer``` and replace the files ```streamerECM.json```, ```streamerPSM1.json```, ```streamerPSM2.json```  with the file in the folder ```streamer_files``` from this repository.
- In all the previous file, the line:

```"ip": "xx.xxx.xx.xxx"```

Should be replace with the ip adress of your windows pc.
- Complete [camera calibration](https://github.com/matmag01/dvrk_camera_stereo_calibration) and [hand-eye registration](https://github.com/jhu-dvrk/dvrk_camera_registration)

## Commands
### Unity PC
Connect your Windows PC to the local network using Ethernet and disable the firewall of the PC
### ROScore
```
source ~/catkin_ws/devel/setup.bash
roscore
```
### Socket Streamer
```
rosrun dvrk_robot dvrk_console_json -j /home/mmagnan4/catkin_ws/devel/share/jhu-daVinci/console-SUJFixed-ECM-PSM1-PSM2.json -m /home/mmagnan4/catkin_ws/src/cisst-saw/sawIntuitiveResearchKit/share/socket-streamer/manager-socket-streamer-patient-cart.json -D
```
Push the button *POWER ON* and *HOME*. You should see the words *PSM1*, *PSM2*, *MTML*, *MTMR*, *ECM* becoming green.

### Camera
Clone git repository for GStream video pipeline:
```
cd ~/Desktop
git clone https://github.com/matmag01/GStream-Pipeline.git
```
Open the Endoscope hardware.
```
cd ~/Desktop/Gs-Stream-Video-Processing/
python img_pipeline.py -v -p --host *Your_IP*
```
- Activate the game mode on Unity

## Teleoperation
Follow the instruction on the screen to perform *pinch calibration*. To redo the calibration say the word *CALIBRATION*
### Endoscope Teleoperation
- Say the word *CAMERA*. When ECM teleoperation is on the quadrant becomes blue.
- With 2 hands grab the quadrant and move it
- To rotate the camera tilt your head
- To center the quadrant say the word *CENTER*
- To exit ECM Teleoperation say the word *FREEZE*
### Instrument Teleoperation
- Activate instrument teleoperation by doing pinch gesture (thumb and index tip) fpr 5 seconds
- Grab the virtual gripper to move the instrument
- Jaw angle is controlled through the distance between thumb tip and middle finger tip
- To clutch, do the pinch gesture (thumb and index tip close)
- To center the quadrant say the word *POSITION*
