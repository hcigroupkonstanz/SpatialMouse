
<h1 align="center">
  SpatialMouse: A Hybrid Pointing Device for Seamless Interaction Across 2D and 3D Spaces
</h1>

<p align="center">
    <strong>
      <a href="https://doi.org/10.1145/3756884.3766047">Publication</a>
        •
      <a href="https://www.youtube.com/watch?v=vRS1W8I82Qw">Video</a>
        •
      <a href="https://kops.uni-konstanz.de/entities/publication/2caa3c3d-b49c-442c-b199-ba38d5cc916e">Hybrid Mixed Reality Input Devices</a>
    </strong>
</p>


![The SpatialMouse](/figures/teaser.jpg?raw=true) 


This is the code repository of the VRST'25 publication:

> Sebastian Hubenschmid, Johannes Zagermann, Robin Erb, Tiare Feuchtner, Jens Grubert, Markus Tatzgern, Dieter Schmalstieg, Harald Reiterer. 2025. SpatialMouse: A Hybrid Pointing Device for Seamless Interaction Across 2D and 3D Spaces. In *31st ACM Symposium on Virtual Reality Software and Technology (VRST ’25), November 12–14, 2025, Montreal, QC, Canada*. ACM, New York, NY, USA, 12 pages. https://doi.org/10.1145/3756884.3766047

The repository contains the following data:

## 3D Models
Contains the models for 3D printing the SpatialMouse prototype and our USB attachment for tracking a mouse using Optitrack.

## Firmware
Contains the firmware files for the internal SpatialMouse logic, which can be uploaded to the Arduino

## Study Prototype
Contains the Unity project of the prototype that was used for evaluating the SpatialMouse in a user study (see paper).

## Assembly Instructions

Our current version of the *SpatialMouse* uses the following components.

- 1x 3D-printed base structure (see 3D models)
- 1x Arduino Nano ESP32
- 1x Mouse Sensor (e.g., PMW 3389)
- 2x Push Buttons (e.g., 7x12x12mm DIP-4 Buttons)
- 1x Joystick Module (e.g., Debo Thumb Joy)
- 1x On/Off Switch (e.g., 3-Pin Pololu Rocker Switch)
- 1x 9V Battery
- 1x Spatial Tracker (e.g., HTC Vive Tracker)

Using the provided 3D files, the casing can be assembled as shown below.
While the bolts (provided with the 3D files) can be used to secure the individual parts, we recommend using hot glue to attach the first half of the grip to the baseplate. The other half of the grip should be removable to allow for easy access to the internal components of the *SpatialMouse*, such as the battery.

![Schematics](/figures/schematic.jpg?raw=true) 

To assemble the SpatialMouse, please refer to the wiring diagram below.
Once assembled and operational, the firmware can be uploaded to the *SpatialMouse* and the *SpatialMouse* can be paired as a Bluetooth device.

![Wiring](/figures/wires.jpeg?raw=true) 
![Side View](/figures/sm_inside.jpeg?raw=true) 
