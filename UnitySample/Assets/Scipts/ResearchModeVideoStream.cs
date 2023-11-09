using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Runtime.InteropServices;

// JULIA: Set up imports for ROS
using Unity.Robotics.ROSTCPConnector;
// using RosMessageTypes.UnityRoboticsDemo;
using RosMessageTypes.Sensor;
using RosMessageTypes.Std;
using RosMessageTypes.BuiltinInterfaces;

#if ENABLE_WINMD_SUPPORT
using HL2UnityPlugin;
#endif

public class ResearchModeVideoStream : MonoBehaviour
{
#if ENABLE_WINMD_SUPPORT
    HL2ResearchMode researchMode;
#endif

    enum DepthSensorMode
    {
        ShortThrow,
        LongThrow,
        None
    };
    [SerializeField] DepthSensorMode depthSensorMode = DepthSensorMode.LongThrow;
    [SerializeField] bool enablePointCloud = true;

    TCPClient tcpClient;

    // JULIA: set up ROS variables
    ROSConnection ros;
    string LFImageTopicName = "LFImages";
    string RFImageTopicName = "RFImages";
    string LLImageTopicName = "LLImages";
    string RRImageTopicName = "RRImages";
    string DepthTopicName = "DepthPC";
    private float timeElapsed;

    bool publishingImages = true;

    public GameObject depthPreviewPlane = null;
    private Material depthMediaMaterial = null;
    private Texture2D depthMediaTexture = null;
    private byte[] depthFrameData = null;

    public GameObject shortAbImagePreviewPlane = null;
    private Material shortAbImageMediaMaterial = null;
    private Texture2D shortAbImageMediaTexture = null;
    private byte[] shortAbImageFrameData = null;

    public GameObject longDepthPreviewPlane = null;
    private Material longDepthMediaMaterial = null;
    private Texture2D longDepthMediaTexture = null;
    private byte[] longDepthFrameData = null;

    public GameObject longAbImagePreviewPlane = null;
    private Material longAbImageMediaMaterial = null;
    private Texture2D longAbImageMediaTexture = null;
    private byte[] longAbImageFrameData = null;

    public GameObject LFPreviewPlane = null;
    private Material LFMediaMaterial = null;
    private Texture2D LFMediaTexture = null;
    private byte[] LFFrameData = null;

    public GameObject RFPreviewPlane = null;
    private Material RFMediaMaterial = null;
    private Texture2D RFMediaTexture = null;
    private byte[] RFFrameData = null;

    public GameObject LLPreviewPlane = null;
    private Material LLMediaMaterial = null;
    private Texture2D LLMediaTexture = null;
    private byte[] LLFrameData = null;

    public GameObject RRPreviewPlane = null;
    private Material RRMediaMaterial = null;
    private Texture2D RRMediaTexture = null;
    private byte[] RRFrameData = null;

    public UnityEngine.UI.Text text;

    public GameObject pointCloudRendererGo;
    public Color pointColor = Color.white;
    // private PointCloudRenderer pointCloudRenderer;
#if ENABLE_WINMD_SUPPORT
    Windows.Perception.Spatial.SpatialCoordinateSystem unityWorldOrigin;
#endif

    public ConfigReader configReader;

    private void Awake()
    {
#if ENABLE_WINMD_SUPPORT
#if UNITY_2020_1_OR_NEWER // note: Unity 2021.2 and later not supported
        // IntPtr WorldOriginPtr = UnityEngine.XR.WindowsMR.WindowsMREnvironment.OriginSpatialCoordinateSystem;
        // unityWorldOrigin = Marshal.GetObjectForIUnknown(WorldOriginPtr) as Windows.Perception.Spatial.SpatialCoordinateSystem;
        unityWorldOrigin = Windows.Perception.Spatial.SpatialLocator.GetDefault().CreateStationaryFrameOfReferenceAtCurrentLocation().CoordinateSystem;

        // var perceptionTimestamp = GetCurrentTimestamp();
        // unityWorldOrigin = Windows.Perception.Spatial.SpatialLocator.GetDefault().CreateAttachedFrameOfReferenceAtCurrentHeading().GetStationaryCoordinateSystemAtTimestamp(perceptionTimestamp);
        // Debug.Log("Setting world origin to attached frame");

#else
        IntPtr WorldOriginPtr = UnityEngine.XR.WSA.WorldManager.GetNativeISpatialCoordinateSystemPtr();
        unityWorldOrigin = Marshal.GetObjectForIUnknown(WorldOriginPtr) as Windows.Perception.Spatial.SpatialCoordinateSystem;
#endif
#endif
    }

    IEnumerator Start()
    {
        yield return new WaitUntil(() => configReader.FinishedReader);   
        // yield return new WaitForSeconds(3f); // Wait 5 seconds to establish all the ROS connections
 
        if (configReader.FinishedReader == true)
        {
            // JULIA: Start the ROS connection
            ros = ROSConnection.GetOrCreateInstance();
            Debug.Log("VideoStreamer is connecting to " + ros.RosIPAddress);
            Debug.Log("VideoStreamer Port " + ros.RosPort);

            ros.RegisterPublisher<ImageMsg>(LFImageTopicName);
            // ros.RegisterPublisher<ImageMsg>(RFImageTopicName);
            // ros.RegisterPublisher<ImageMsg>(LLImageTopicName);
            ros.RegisterPublisher<ImageMsg>(RRImageTopicName);
            ros.RegisterPublisher<PointCloud2Msg>(DepthTopicName);

            Debug.Log("Registered Image Publishers");
        }



        // if (depthSensorMode == DepthSensorMode.ShortThrow)
        // {
        //     if (depthPreviewPlane != null)
        //     {
        //         depthMediaMaterial = depthPreviewPlane.GetComponent<MeshRenderer>().material;
        //         depthMediaTexture = new Texture2D(512, 512, TextureFormat.Alpha8, false);
        //         depthMediaMaterial.mainTexture = depthMediaTexture;
        //     }

        //     if (shortAbImagePreviewPlane != null)
        //     {
        //         shortAbImageMediaMaterial = shortAbImagePreviewPlane.GetComponent<MeshRenderer>().material;
        //         shortAbImageMediaTexture = new Texture2D(512, 512, TextureFormat.Alpha8, false);
        //         shortAbImageMediaMaterial.mainTexture = shortAbImageMediaTexture;
        //     }
        //     longDepthPreviewPlane.SetActive(false);
        //     longAbImagePreviewPlane.SetActive(false);
        // }
        
        if (depthSensorMode == DepthSensorMode.LongThrow)
        {
            if (longDepthPreviewPlane != null)
            {
                longDepthMediaMaterial = longDepthPreviewPlane.GetComponent<MeshRenderer>().material;
                longDepthMediaTexture = new Texture2D(320, 288, TextureFormat.Alpha8, false);
                longDepthMediaMaterial.mainTexture = longDepthMediaTexture;
            }

            if (longAbImagePreviewPlane != null)
            {
                longAbImageMediaMaterial = longAbImagePreviewPlane.GetComponent<MeshRenderer>().material;
                longAbImageMediaTexture = new Texture2D(320, 288, TextureFormat.Alpha8, false);
                longAbImageMediaMaterial.mainTexture = longAbImageMediaTexture;
            }
            depthPreviewPlane.SetActive(false);
            shortAbImagePreviewPlane.SetActive(false);
        }
        

        if (LFPreviewPlane != null)
        {
            LFMediaMaterial = LFPreviewPlane.GetComponent<MeshRenderer>().material;
            LFMediaTexture = new Texture2D(640, 480, TextureFormat.Alpha8, false);
            LFMediaMaterial.mainTexture = LFMediaTexture;
        }

        // if (RFPreviewPlane != null)
        // {
        //     RFMediaMaterial = RFPreviewPlane.GetComponent<MeshRenderer>().material;
        //     RFMediaTexture = new Texture2D(640, 480, TextureFormat.Alpha8, false);
        //     RFMediaMaterial.mainTexture = RFMediaTexture;
        // }

        // if (LLPreviewPlane != null)
        // {
        //     LLMediaMaterial = LLPreviewPlane.GetComponent<MeshRenderer>().material;
        //     LLMediaTexture = new Texture2D(640, 480, TextureFormat.Alpha8, false);
        //     LLMediaMaterial.mainTexture = LLMediaTexture;
        // }

        if (RRPreviewPlane != null)
        {
            RRMediaMaterial = RRPreviewPlane.GetComponent<MeshRenderer>().material;
            RRMediaTexture = new Texture2D(640, 480, TextureFormat.Alpha8, false);
            RRMediaMaterial.mainTexture = RRMediaTexture;
        }

        if (pointCloudRendererGo != null)
        {
            // pointCloudRenderer = pointCloudRendererGo.GetComponent<PointCloudRenderer>();
        }

        tcpClient = GetComponent<TCPClient>();

#if ENABLE_WINMD_SUPPORT
        researchMode = new HL2ResearchMode();

        // Depth sensor should be initialized in only one mode
        if (depthSensorMode == DepthSensorMode.LongThrow) researchMode.InitializeLongDepthSensor();
        else if (depthSensorMode == DepthSensorMode.ShortThrow) researchMode.InitializeDepthSensor();
        
        researchMode.InitializeSpatialCamerasFront();
        researchMode.SetReferenceCoordinateSystem(unityWorldOrigin);
        researchMode.SetPointCloudDepthOffset(0);

        // Depth sensor should be initialized in only one mode
        if (depthSensorMode == DepthSensorMode.LongThrow) researchMode.StartLongDepthSensorLoop(enablePointCloud);
        else if (depthSensorMode == DepthSensorMode.ShortThrow) researchMode.StartDepthSensorLoop(enablePointCloud);

        researchMode.StartSpatialCamerasFrontLoop();

        // PRINTING LF RF DEPTH EXTRINSICS
        researchMode.PrintLongDepthExtrinsics();
        researchMode.PrintLFExtrinsics();
        researchMode.PrintRFExtrinsics();
        researchMode.PrintLLExtrinsics();
        researchMode.PrintRRExtrinsics();

        researchMode.PrintRigNodeInCoordSystem(unityWorldOrigin);
#endif
    }

    long getTicksSince1970(long ts)
    {
        // ts is ticks since 0001
        // 62135596800 0000000 is ticks between 0001 and 1970 (seconds between is 62135596800 )
        long offset = (long)621355968000000000;
        return ts - offset;
    }

    bool startRealtimePreview = true;
    void LateUpdate()
    {
#if ENABLE_WINMD_SUPPORT
        // var perceptionTimestamp = GetCurrentTimestamp();
        // unityWorldOrigin = Windows.Perception.Spatial.SpatialLocator.GetDefault().CreateAttachedFrameOfReferenceAtCurrentHeading().GetStationaryCoordinateSystemAtTimestamp(perceptionTimestamp);
        // researchMode.SetReferenceCoordinateSystem(unityWorldOrigin);


        // // update depth map texture
        // if (depthSensorMode == DepthSensorMode.ShortThrow && startRealtimePreview && 
        //     depthPreviewPlane != null && researchMode.DepthMapTextureUpdated())
        // {
        //     byte[] frameTexture = researchMode.GetDepthMapTextureBuffer();
        //     if (frameTexture.Length > 0)
        //     {
        //         if (depthFrameData == null)
        //         {
        //             depthFrameData = frameTexture;
        //         }
        //         else
        //         {
        //             System.Buffer.BlockCopy(frameTexture, 0, depthFrameData, 0, depthFrameData.Length);
        //         }

        //         depthMediaTexture.LoadRawTextureData(depthFrameData);
        //         depthMediaTexture.Apply();
        //     }
        // }

        // // update short-throw AbImage texture
        // if (depthSensorMode == DepthSensorMode.ShortThrow && startRealtimePreview && 
        //     shortAbImagePreviewPlane != null && researchMode.ShortAbImageTextureUpdated())
        // {
        //     byte[] frameTexture = researchMode.GetShortAbImageTextureBuffer();
        //     if (frameTexture.Length > 0)
        //     {
        //         if (shortAbImageFrameData == null)
        //         {
        //             shortAbImageFrameData = frameTexture;
        //         }
        //         else
        //         {
        //             System.Buffer.BlockCopy(frameTexture, 0, shortAbImageFrameData, 0, shortAbImageFrameData.Length);
        //         }

        //         shortAbImageMediaTexture.LoadRawTextureData(shortAbImageFrameData);
        //         shortAbImageMediaTexture.Apply();
        //     }
        // }

        // update long depth map texture
        if (depthSensorMode == DepthSensorMode.LongThrow && startRealtimePreview && 
            longDepthPreviewPlane != null && researchMode.LongDepthMapTextureUpdated())
        {
            byte[] frameTexture = researchMode.GetLongDepthMapTextureBuffer();
            Debug.Log("Depth Image frameTexture " + frameTexture.Length);
            if (frameTexture.Length > 0)
            {
                if (longDepthFrameData == null)
                {
                    longDepthFrameData = frameTexture;
                }
                else
                {
                    System.Buffer.BlockCopy(frameTexture, 0, longDepthFrameData, 0, longDepthFrameData.Length);
                }

                longDepthMediaTexture.LoadRawTextureData(longDepthFrameData);
                longDepthMediaTexture.Apply();
            }
        }

        // update long-throw AbImage texture
        if (depthSensorMode == DepthSensorMode.LongThrow && startRealtimePreview &&
            longAbImagePreviewPlane != null && researchMode.LongAbImageTextureUpdated())
        {
            byte[] frameTexture = researchMode.GetLongAbImageTextureBuffer();
            if (frameTexture.Length > 0)
            {
                if (longAbImageFrameData == null)
                {
                    longAbImageFrameData = frameTexture;
                }
                else
                {
                    System.Buffer.BlockCopy(frameTexture, 0, longAbImageFrameData, 0, longAbImageFrameData.Length);
                }

                longAbImageMediaTexture.LoadRawTextureData(longAbImageFrameData);
                longAbImageMediaTexture.Apply();
            }
        }

        // update LF camera texture
        if (researchMode.LFImageUpdated() && publishingImages)
        {
            long ts;
            byte[] frameTexture = researchMode.GetLFCameraBuffer(out ts);
            if (frameTexture.Length > 0)
            {
                if (startRealtimePreview && LFPreviewPlane != null)
                {
                    if (LFFrameData == null)
                    {
                        LFFrameData = frameTexture;
                    }
                    else
                    {
                        System.Buffer.BlockCopy(frameTexture, 0, LFFrameData, 0, LFFrameData.Length);
                    }

                    LFMediaTexture.LoadRawTextureData(LFFrameData);
                    LFMediaTexture.Apply();
                }
                // // JULIA: Get the Unity time
                // double unity_time = Time.timeAsDouble;
                // // float unity_time = Time.time;

                // uint unity_time_sec = (uint)unity_time;
                // uint unity_time_nano = (uint)((unity_time - (int)unity_time_sec) * 1e9);

                HeaderMsg header = new HeaderMsg(
                    0,
                    new TimeMsg(),
                    "DepthMap"
                );

                // JULIA: I'm assuming ticks is hundreds of nanoseconds
                // get nanoseconds since last second
                header.stamp.nanosec = (uint) (ts % TimeSpan.TicksPerSecond) * 100;
                ulong seconds = (ulong) (ts / TimeSpan.TicksPerSecond);
                ulong secondsSince1970 = seconds - 62135596800 ; // 62135596800  is the number of seconds between 0001 and 1970
                header.stamp.sec = (uint) secondsSince1970;
                // long ticksSinceLinux = getTicksSince1970(ts); // praying that this fits in a uint                
                // header.stamp.sec = (uint)(ticksSinceLinux/TimeSpan.TicksPerSecond); // Just the number of seconds
                // header.stamp.nanosec = (uint)( (ticksSinceLinux) - (header.stamp.sec*TimeSpan.TicksPerSecond) ) * 100; // Number of ns with the seconds subtracted
                
                // JULIA: add passing byte[] frameTexture to a ROS message
                ImageMsg imageMsg = new ImageMsg(
                    header,
                    480,
                    640,
                    "mono8",
                    1, // True
                    640, // row length in bytes is just the row length
                    frameTexture
                );

                ros.Publish(LFImageTopicName, imageMsg);
                timeElapsed = 0;
            }
        }

        // // update RF camera texture
        // if (startRealtimePreview && RFPreviewPlane != null && researchMode.RFImageUpdated())
        // {
        //     long ts;
        //     byte[] frameTexture = researchMode.GetRFCameraBuffer(out ts);
        //     if (frameTexture.Length > 0)
        //     {
        //         if (RFFrameData == null)
        //         {
        //             RFFrameData = frameTexture;
        //         }
        //         else
        //         {
        //             System.Buffer.BlockCopy(frameTexture, 0, RFFrameData, 0, RFFrameData.Length);
        //         }

        //         RFMediaTexture.LoadRawTextureData(RFFrameData);
        //         RFMediaTexture.Apply();

        //         // JULIA: Get the Unity time
        //         double unity_time = Time.timeAsDouble;
        //         // float unity_time = Time.time;

        //         uint unity_time_sec = (uint)unity_time;
        //         uint unity_time_nano = (uint)((unity_time - (int)unity_time_sec) * 1e9);

        //         // JULIA: add passing byte[] frameTexture to a ROS message
        //         ImageMsg imageMsg = new ImageMsg(
        //             new HeaderMsg(
        //                 0,
        //                 new TimeMsg(unity_time_sec, unity_time_nano),
        //                 "DepthMap"
        //             ),
        //             480,
        //             640,
        //             "mono8",
        //             1, // True
        //             640, // row length in bytes is just the row length
        //             frameTexture
        //         );

        //         ros.Publish(RFImageTopicName, imageMsg);
        //         timeElapsed = 0;
        //     }
        // }





        // // update LL camera texture
        // // if (startRealtimePreview && LLPreviewPlane != null && researchMode.LLImageUpdated())
        // if (researchMode.LLImageUpdated())
        // {
        //     long ts;
        //     byte[] frameTexture = researchMode.GetLLCameraBuffer(out ts);
        //     if (frameTexture.Length > 0)
        //     {
        //         // if (LLFrameData == null)
        //         // {
        //         //     LLFrameData = frameTexture;
        //         // }
        //         // else
        //         // {
        //         //     System.Buffer.BlockCopy(frameTexture, 0, LLFrameData, 0, LLFrameData.Length);
        //         // }

        //         // LLMediaTexture.LoadRawTextureData(LLFrameData);
        //         // LLMediaTexture.Apply();

        //         // JULIA: Get the Unity time
        //         double unity_time = Time.timeAsDouble;
        //         // float unity_time = Time.time;

        //         uint unity_time_sec = (uint)unity_time;
        //         uint unity_time_nano = (uint)((unity_time - (int)unity_time_sec) * 1e9);


        //         // JULIA: add passing byte[] frameTexture to a ROS message
        //         ImageMsg imageMsg = new ImageMsg(
        //             new HeaderMsg(
        //                 0,
        //                 new TimeMsg(unity_time_sec, unity_time_nano),
        //                 "DepthMap"
        //             ),
        //             480,
        //             640,
        //             "mono8",
        //             1, // True
        //             640, // row length in bytes is just the row length
        //             frameTexture
        //         );

        //         ros.Publish(LLImageTopicName, imageMsg);
        //         timeElapsed = 0;
        //     }
        // }

        // update RR camera texture
        if (researchMode.RRImageUpdated() && publishingImages)
        {
            long ts; // Should already be hundreds of nanoseconds
            byte[] frameTexture = researchMode.GetRRCameraBuffer(out ts);
            if (frameTexture.Length > 0)
            {
                if (startRealtimePreview && RRPreviewPlane != null)
                {
                    if (RRFrameData == null)
                    {
                        RRFrameData = frameTexture;
                    }
                    else
                    {
                        System.Buffer.BlockCopy(frameTexture, 0, RRFrameData, 0, RRFrameData.Length);
                    }

                    RRMediaTexture.LoadRawTextureData(RRFrameData);
                    RRMediaTexture.Apply();
                }


                // // JULIA: Get the Unity time
                // double unity_time = Time.timeAsDouble;
                // // float unity_time = Time.time;

                // uint unity_time_sec = (uint)unity_time;
                // uint unity_time_nano = (uint)((unity_time - (int)unity_time_sec) * 1e9);

                HeaderMsg header = new HeaderMsg(
                    0,
                    new TimeMsg(),
                    "DepthMap"
                );
                // get nanoseconds since last second
                header.stamp.nanosec = (uint) (ts % TimeSpan.TicksPerSecond) * 100;
                ulong seconds = (ulong) (ts / TimeSpan.TicksPerSecond);
                ulong secondsSince1970 = seconds - 62135596800 ; // 62135596800  is the number of seconds between 0001 and 1970
                header.stamp.sec = (uint) secondsSince1970;

                // // JULIA: I'm assuming ticks is hundreds of nanoseconds
                // long ticksSinceLinux = getTicksSince1970(ts);                
                // header.stamp.sec = (uint)(ticksSinceLinux/TimeSpan.TicksPerSecond); // Just the number of seconds
                // header.stamp.nanosec = (uint)( (ticksSinceLinux) - (header.stamp.sec*TimeSpan.TicksPerSecond) ) * 100; // Number of ns with the seconds subtracted

                // JULIA: add passing byte[] frameTexture to a ROS message
                ImageMsg imageMsg = new ImageMsg(
                    header,
                    480,
                    640,
                    "mono8",
                    1, // True
                    640, // row length in bytes is just the row length
                    frameTexture
                );

                ros.Publish(RRImageTopicName, imageMsg);
                timeElapsed = 0;
            }
        }

        // Update point cloud
        UpdatePointCloud(); // Updates the DepthPC

        // Print the rig SpatialLoationMatrix
        
#endif
    }


    // To visualize the point cloud:
    // - able to get a windows spatial locator world frame
    // - able to get a maplab "world" frame
    // - theoretically, the transform between these two should be static
    // - what is the transform between the windows spatial locator world frame and the unity world frame?
    //   - assuming they are at the same origin on app start up, then it's just a RH->LH and rotation?
#if ENABLE_WINMD_SUPPORT
    private void UpdatePointCloud()
    {
        // need to update how we get the timestamps if we use the depth point cloud
        if (enablePointCloud && renderPointCloud && pointCloudRendererGo != null)
        {
            if ((depthSensorMode == DepthSensorMode.LongThrow && !researchMode.LongThrowPointCloudUpdated()) ||
                (depthSensorMode == DepthSensorMode.ShortThrow && !researchMode.PointCloudUpdated())) return;

            float[] pointCloud = new float[] { };
            long ts;
            // JULIA: a little bit hacky, in order to have the ts variable be defined we need to have the longthrow
            // Otherwise the else statement will see that ts could be undefined
            // if (depthSensorMode == DepthSensorMode.LongThrow) pointCloud = researchMode.GetLongThrowPointCloudBuffer(out ts);
            // else if (depthSensorMode == DepthSensorMode.ShortThrow) pointCloud = researchMode.GetPointCloudBuffer();
            pointCloud = researchMode.GetLongThrowPointCloudBuffer(out ts);
            Debug.Log("Size of point cloud " + pointCloud.Length);

            if (pointCloud.Length > 0)
            {
                int pointCloudLength = pointCloud.Length / 3;
                Vector3[] pointCloudVector3 = new Vector3[pointCloudLength];
                for (int i = 0; i < pointCloudLength; i++)
                {
                    pointCloudVector3[i] = new Vector3(pointCloud[3 * i], pointCloud[3 * i + 1], pointCloud[3 * i + 2]);
                }
                text.text = "Point Cloud Length: " + pointCloudVector3.Length.ToString();
                // pointCloudRenderer.Render(pointCloudVector3, pointColor);

                // JULIA: Send the Vector3[] of point cloud to ROS
                // Convert to byte[]
                byte[] pointCloudBytes = new byte[pointCloud.Length * 4];
                Buffer.BlockCopy(pointCloud, 0, pointCloudBytes, 0, pointCloudBytes.Length);

                // // JULIA: Get the Unity time
                // double unity_time = Time.timeAsDouble;
                // // float unity_time = Time.time;

                // uint unity_time_sec = (uint)unity_time;
                // uint unity_time_nano = (uint)((unity_time - (int)unity_time_sec) * 1e9);

                // HeaderMsg header = new HeaderMsg(
                //     0,
                //     new TimeMsg(unity_time_sec, unity_time_nano),
                //     "DepthMap"
                // );

                // Getting the FileTime from 1601
                HeaderMsg header = new HeaderMsg(
                    0,
                    new TimeMsg(),
                    "DepthMap"
                );

                // get nanoseconds since last second
                header.stamp.nanosec = (uint) (ts % TimeSpan.TicksPerSecond) * 100;
                ulong seconds = (ulong) (ts / TimeSpan.TicksPerSecond);
                ulong secondsSince1970 = seconds -   62135596800 ; // 62135596800  is the number of seconds between 0001 and 1970
                // secondsSince1970 = secondsSince1970 - 1016948351; // magic number found by trial and error 62167132800+1016948351=63184081151
                header.stamp.sec = (uint) secondsSince1970;

                // // JULIA: I'm assuming ticks is hundreds of nanoseconds
                // long ticksSinceLinux = getTicksSince1970(ts);                
                // header.stamp.sec = (uint)(ticksSinceLinux/TimeSpan.TicksPerSecond); // Just the number of seconds
                // header.stamp.nanosec = (uint)( (ticksSinceLinux) - (header.stamp.sec*TimeSpan.TicksPerSecond) ) * 100; // Number of ns with the seconds subtracted


                PointFieldMsg[] pfMsg = new PointFieldMsg[3];
                pfMsg[0] = new PointFieldMsg(
                    "x",
                    0,
                    7,
                    1
                );
                pfMsg[1] = new PointFieldMsg(
                    "y",
                    4,
                    7,
                    1
                );
                pfMsg[2] = new PointFieldMsg(
                    "z",
                    8,
                    7,
                    1
                );
                PointCloud2Msg pc2Msg = new PointCloud2Msg(
                    header,
                    Convert.ToUInt32(1), // height
                    Convert.ToUInt32(pointCloudVector3.Length), // width
                    pfMsg,
                    true, // is_bigendian
                    Convert.ToUInt32(12), // point_step, length of a point in bytes
                    Convert.ToUInt32(4*pointCloudVector3.Length), // row_step, length of a row in bytes, I assume it's just the whole size
                    pointCloudBytes,
                    true // is_dense
                );

                ros.Publish(DepthTopicName, pc2Msg);
            }
        }
    }
#endif


    #region Button Event Functions
    public void TogglePreviewEvent()
    {
        startRealtimePreview = !startRealtimePreview;
    }

    bool renderPointCloud = true;
    public void TogglePointCloudEvent()
    {
        renderPointCloud = !renderPointCloud;
        if (renderPointCloud)
        {
            pointCloudRendererGo.SetActive(true);
        }
        else
        {
            pointCloudRendererGo.SetActive(false);
        }
    }

    public void StopSensorsEvent()
    {
#if ENABLE_WINMD_SUPPORT
        researchMode.StopAllSensorDevice();
#endif
        startRealtimePreview = false;
    }

    public void SaveAHATSensorDataEvent()
    {
#if ENABLE_WINMD_SUPPORT
        var depthMap = researchMode.GetDepthMapBuffer();
        var AbImage = researchMode.GetShortAbImageBuffer();
#if WINDOWS_UWP
        if (tcpClient != null)
        {
            tcpClient.SendUINT16Async(depthMap, AbImage);
        }
#endif
#endif
    }

    public void SaveSpatialImageEvent()
    {
#if ENABLE_WINMD_SUPPORT
#if WINDOWS_UWP
        long ts_ft_left, ts_ft_right;
        var LRFImage = researchMode.GetLRFCameraBuffer(out ts_ft_left, out ts_ft_right);
        Windows.Perception.PerceptionTimestamp ts_left = Windows.Perception.PerceptionTimestampHelper.FromHistoricalTargetTime(DateTime.FromFileTime(ts_ft_left));
        Windows.Perception.PerceptionTimestamp ts_right = Windows.Perception.PerceptionTimestampHelper.FromHistoricalTargetTime(DateTime.FromFileTime(ts_ft_right));

        long ts_unix_left = ts_left.TargetTime.ToUnixTimeMilliseconds();
        long ts_unix_right = ts_right.TargetTime.ToUnixTimeMilliseconds();
        long ts_unix_current = GetCurrentTimestampUnix();

        text.text = "Left: " + ts_unix_left.ToString() + "\n" +
            "Right: " + ts_unix_right.ToString() + "\n" +
            "Current: " + ts_unix_current.ToString();

        if (tcpClient != null)
        {
            tcpClient.SendSpatialImageAsync(LRFImage, ts_unix_left, ts_unix_right);
        }
#endif
#endif
    }

    #endregion
    private void OnApplicationFocus(bool focus)
    {
        if (!focus) StopSensorsEvent();
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
    private Windows.Perception.PerceptionTimestamp GetCurrentTimestamp()
    {
        // Get the current time, in order to create a PerceptionTimestamp. 
        Windows.Globalization.Calendar c = new Windows.Globalization.Calendar();
        return Windows.Perception.PerceptionTimestampHelper.FromHistoricalTargetTime(c.GetDateTime());
    }
#endif

    public void togglePublishingImages()
    {
        publishingImages = !publishingImages;
    }
}