//  
// Copyright (c) 2017 Vulcan, Inc. All rights reserved.  
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.
//

using UnityEngine;
using UnityEngine.XR.WSA;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using HoloLensCameraStream;

// JULIA: Set up imports for ROS
using Unity.Robotics.ROSTCPConnector;
// using RosMessageTypes.UnityRoboticsDemo;
using RosMessageTypes.Sensor;
using RosMessageTypes.Std;
using RosMessageTypes.BuiltinInterfaces;



/// <summary>
/// This example gets the video frames at 30 fps and displays them on a Unity texture,
/// which is locked the User's gaze.
/// </summary>
public class VideoPanelApp : MonoBehaviour
{
    byte[] _latestImageBytes;
    HoloLensCameraStream.Resolution _resolution;

    //"Injected" objects.
    VideoPanel _videoPanelUI;
    VideoCapture _videoCapture;

    IntPtr _spatialCoordinateSystemPtr; 

#if ENABLE_WINMD_SUPPORT
    Windows.Perception.Spatial.SpatialCoordinateSystem unityWorldOrigin;
#endif

    public ConfigReader configReader;
    // JULIA: set up ROS variables
    ROSConnection ros;
    string RGBImageTopicName = "RGBImages";

    IEnumerator Start()
    {
        yield return new WaitUntil(() => configReader.FinishedReader);   


#if ENABLE_WINMD_SUPPORT
#if UNITY_2020_1_OR_NEWER // note: Unity 2021.2 and later not supported
        // _spatialCoordinateSystemPtr = UnityEngine.XR.WindowsMR.WindowsMREnvironment.OriginSpatialCoordinateSystem;
        unityWorldOrigin = Windows.Perception.Spatial.SpatialLocator.GetDefault().CreateStationaryFrameOfReferenceAtCurrentLocation().CoordinateSystem;
        _spatialCoordinateSystemPtr = Marshal.GetIUnknownForObject(unityWorldOrigin);

#else
        //Fetch a pointer to Unity's spatial coordinate system if you need pixel mapping
        _spatialCoordinateSystemPtr = UnityEngine.XR.WSA.WorldManager.GetNativeISpatialCoordinateSystemPtr();
#endif
#endif

        //Call this in Start() to ensure that the CameraStreamHelper is already "Awake".
        CameraStreamHelper.Instance.GetVideoCaptureAsync(OnVideoCaptureCreated);
        //You could also do this "shortcut":
        //CameraStreamManager.Instance.GetVideoCaptureAsync(v => videoCapture = v);

        _videoPanelUI = GameObject.FindObjectOfType<VideoPanel>();


        if (configReader.FinishedReader == true)
        {
            // JULIA: Start the ROS connection
            ros = ROSConnection.GetOrCreateInstance();
            ros.RegisterPublisher<ImageMsg>(RGBImageTopicName, 2); // queue size
            Debug.Log("Registered RGB Image publisher");
        }

    }

    private void OnDestroy()
    {
        if (_videoCapture != null)
        {
            _videoCapture.FrameSampleAcquired -= OnFrameSampleAcquired;
            _videoCapture.Dispose();
        }
    }

    void OnVideoCaptureCreated(VideoCapture videoCapture)
    {
        if (videoCapture == null)
        {
            Debug.LogError("Did not find a video capture object. You may not be using the HoloLens.");
            return;
        }
        
        this._videoCapture = videoCapture;

        Debug.Log("Requesting set spatial coordinate system");
        //Request the spatial coordinate ptr if you want fetch the camera and set it if you need to 
        CameraStreamHelper.Instance.SetNativeISpatialCoordinateSystemPtr(_spatialCoordinateSystemPtr);

        Debug.Log("Finished setting spatial coordinate system");

        _resolution = CameraStreamHelper.Instance.GetLowestResolution();
        float frameRate = CameraStreamHelper.Instance.GetHighestFrameRate(_resolution);
        videoCapture.FrameSampleAcquired += OnFrameSampleAcquired;

        //You don't need to set all of these params.
        //I'm just adding them to show you that they exist.
        CameraParameters cameraParams = new CameraParameters();
        // cameraParams.cameraResolutionHeight = _resolution.height;
        // cameraParams.cameraResolutionWidth = _resolution.width;
        cameraParams.cameraResolutionHeight = 504;
        cameraParams.cameraResolutionWidth = 896;
        cameraParams.frameRate = Mathf.RoundToInt(frameRate);
        cameraParams.pixelFormat = CapturePixelFormat.BGRA32;
        cameraParams.rotateImage180Degrees = true; //If your image is upside down, remove this line.
        cameraParams.enableHolograms = false;

        if (_videoPanelUI != null)
        {
            UnityEngine.WSA.Application.InvokeOnAppThread(() => { _videoPanelUI.SetResolution(_resolution.width, _resolution.height); }, false);
        }

        videoCapture.StartVideoModeAsync(cameraParams, OnVideoModeStarted);
    }

    void OnVideoModeStarted(VideoCaptureResult result)
    {
        if (result.success == false)
        {
            Debug.LogWarning("Could not start video mode.");
            return;
        }

        Debug.Log("Video capture started.");
    }

    void OnFrameSampleAcquired(VideoCaptureSample sample)
    {
        //When copying the bytes out of the buffer, you must supply a byte[] that is appropriately sized.
        //You can reuse this byte[] until you need to resize it (for whatever reason).
        if (_latestImageBytes == null || _latestImageBytes.Length < sample.dataLength)
        {
            _latestImageBytes = new byte[sample.dataLength];
        }
        sample.CopyRawImageDataIntoBuffer(_latestImageBytes);
        
        //If you need to get the cameraToWorld matrix for purposes of compositing you can do it like this
        float[] cameraToWorldMatrix;
        if (sample.TryGetCameraToWorldMatrix(out cameraToWorldMatrix) == false)
        {
            return;
        }

        //If you need to get the projection matrix for purposes of compositing you can do it like this
        float[] projectionMatrix;
        if (sample.TryGetProjectionMatrix(out projectionMatrix) == false)
        {
            return;
        }

        sample.Dispose();

        if (_videoPanelUI != null)
        {
            //This is where we actually use the image data
            UnityEngine.WSA.Application.InvokeOnAppThread(() =>
            {
                _videoPanelUI.SetBytes(_latestImageBytes);
            }, false);
        }

        // This is where we publish the image bytes to ROS
        Debug.Log("imagebyte length " + _latestImageBytes.Length);
        if (_latestImageBytes.Length > 0)
        {
            // JULIA: Get the Unix time
            // long unix_time = GetCurrentTimestampUnix();

            // double unity_time = Time.timeAsDouble;
            // float unity_time = Time.time;

            // uint unity_time_sec = (uint)unity_time;
            // uint unity_time_nano = (uint)((unity_time - (int)unity_time_sec) * 1e9);

            // JULIA: convert byte[] _latestImageBytes to a ROS message
            ImageMsg imageMsg = new ImageMsg(
                new HeaderMsg(
                    0,
                    new TimeMsg(), // new TimeMsg(unity_time_sec, unity_time_nano),
                    "DepthMap"
                ),
                (uint)_resolution.height, // height, 1278
                (uint)_resolution.width, // width, 2272
                "bgra8",
                1, // True
                (uint)_resolution.width * 4, // row length in bytes is just the row length
                _latestImageBytes
            );

            ros.Publish(RGBImageTopicName, imageMsg);
        }
    }

#if WINDOWS_UWP
    private long GetCurrentTimestampUnix()
    {
        // Get the current time, in order to create a PerceptionTimestamp. 
        Windows.Globalization.Calendar c = new Windows.Globalization.Calendar();
        Windows.Perception.PerceptionTimestamp ts = Windows.Perception.PerceptionTimestampHelper.FromHistoricalTargetTime(c.GetDateTime());
        return ts.TargetTime.ToUnixTimeMilliseconds();
        //return ts.SystemRelativeTargetTime.Ticks;
    }

#endif
}
