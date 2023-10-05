# HoloLens2 Interface to ROS

[[arxiv](https://arxiv.org/abs/2310.02392)] [[youtube](https://www.youtube.com/watch?v=H3IA5FXnFX8)]

<iframe width="560" height="315" src="https://www.youtube-nocookie.com/embed/H3IA5FXnFX8?si=EhYMZsKOROOBdb4-" title="YouTube video player" frameborder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share" allowfullscreen></iframe>

A modified App based on based on [HoloLens2ForCV](https://github.com/microsoft/HoloLens2ForCV) and [UnityRoboticsHub](https://github.com/Unity-Technologies/Unity-Robotics-Hub).




## Note:
- The reconstructed point cloud still has the offset problem as is described [here](https://github.com/microsoft/HoloLens2ForCV/issues/12) for object beyond 1m.
- To visualize the depth image, you need a grayscale shader applied to your preview plane. Example: [grayscale shader](https://github.com/qian256/HoloLensARToolKit/blob/master/HoloLensARToolKit/Assets/Sample/Grayscale.shader).
- For point cloud, current implementation only returns the reconstructed point cloud as a float array (in the format of x,y,z,x,y,z,...). If you want to visualize it, I find [this project](https://github.com/MarekKowalski/LiveScan3D-Hololens) is a good example.
- This project is mainly to show how to use Reseach Mode in Unity. I only provided implementation on AHAT camera image visualization and point cloud reconstruction (based on depth map of AHAT camera), two front spatial camera. The long-throw depth sensor and IMU sensor are also available thanks to @HoloAdventure. Feel free to modify the code according to your own need.
- Only one of the short-throw(AHAT) and long-throw depth sensor should be enabled at the same time.
- If you need a sample project to get started, you can refer to UnitySample folder.
