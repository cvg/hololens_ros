using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Runtime.InteropServices;

using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;
using RosMessageTypes.Sensor;
using RosMessageTypes.BuiltinInterfaces;
using RosMessageTypes.Std;
using RosMessageTypes.Nav;

#if ENABLE_WINMD_SUPPORT
using HL2UnityPlugin;


#endif

public class OdomPublisher : MonoBehaviour
{
#if ENABLE_WINMD_SUPPORT
    HL2ResearchMode researchMode;

    // For the Spatial Locator World
    Windows.Perception.Spatial.SpatialCoordinateSystem unityWorldOrigin;

    Windows.Perception.PerceptionTimestamp previousTimestamp;
#endif
    public Transform Device;
    public Transform World;

    public string FrameId = "Unity";

    private OdometryMsg message;

    private double previousRealTime;
    private Vector3 previousPosition = Vector3.zero;
    private Quaternion previousRotation = Quaternion.identity;

    ROSConnection ros;
    string OdomTopicName = "Odometry";

    private static Timer timer = new Timer();

    public ConfigReader configReader;

    private void Awake()
    {
#if ENABLE_WINMD_SUPPORT
#if UNITY_2020_1_OR_NEWER
    // IntPtr WorldOriginPtr = UnityEngine.XR.WindowsMR.WindowsMREnvironment.OriginSpatialCoordinateSystem;
    // unityWorldOrigin = Marshal.GetObjectForIUnknown(WorldOriginPtr) as Windows.Perception.Spatial.SpatialCoordinateSystem;
    unityWorldOrigin = Windows.Perception.Spatial.SpatialLocator.GetDefault().CreateStationaryFrameOfReferenceAtCurrentLocation().CoordinateSystem;
#endif
#endif
    }

    IEnumerator Start()
    {
        yield return new WaitUntil(() => configReader.FinishedReader);    
        // yield return new WaitForSeconds(3f); // Wait 5 seconds to establish all the ROS connections

        InitializeMessage();

        if (configReader.FinishedReader == true)
        {
            // Set up ROSConnection
            ros = ROSConnection.GetOrCreateInstance();
            ros.RegisterPublisher<OdometryMsg>(OdomTopicName);
            Debug.Log("Registered Odom Publisher");
        }


#if ENABLE_WINMD_SUPPORT
        previousTimestamp = GetCurrentTimestamp();
#endif

    }

    void FixedUpdate()
    {
        // UpdateMessage();
        UpdateMessageUsingWindowsSpatialLocator();
    }

    private void InitializeMessage()
    {
        message = new OdometryMsg
        {
            header = new HeaderMsg
            {
                seq = 0,
                frame_id = FrameId,
                stamp = timer.Now()
            },
            child_frame_id = "Device",
            pose = new PoseWithCovarianceMsg
            {
                pose = new PoseMsg
                {
                    position = new PointMsg(),
                    orientation = new QuaternionMsg()
                },
                covariance = new double[36]
            },
            twist = new TwistWithCovarianceMsg
            {
                twist = new TwistMsg
                {
                    linear = new Vector3Msg(),
                    angular = new Vector3Msg()
                },
                covariance = new double[36]
            }
        };
    }

    private void UpdateMessage()
    {
        // Add check to see if odometry never changed (aka lose tracking)

        // Update header
        message.header.seq++;
        // timer.Now(message.header.stamp);
        // // Get the midpoint time
        float deltaTime = (float)(Time.realtimeSinceStartup - previousRealTime);
        double midTime = previousRealTime + ((Time.realtimeSinceStartup - previousRealTime) / 2.0);
        message.header.stamp.sec = (uint)midTime;
        message.header.stamp.nanosec = (uint)((midTime - (int)message.header.stamp.sec) * 1e9);
        
        // Adding the Pose
        Debug.Log("Adding the pose to message");
        var relativePosition = World.InverseTransformPoint(Device.position);
        Quaternion relativeRotation = Quaternion.Inverse(World.rotation) * Device.rotation;
        Debug.Log("relativePosition: " + relativePosition);
        Debug.Log("relativeRotation: " + relativeRotation);
        Debug.Log("Device.rotation: " + Device.rotation);

        // Turning the Pose to ROS messages
        GetGeometryPoint(relativePosition.Unity2Ros(), message.pose.pose.position);
        GetGeometryQuaternion(relativeRotation.Unity2Ros(), message.pose.pose.orientation);

        // Adding the Twist
        Debug.Log("Adding twist to message");
        Vector3 linearVelocity = (relativePosition - previousPosition)/deltaTime;
        linearVelocity = relativeRotation * linearVelocity;

        Vector3 angularVelocity = (relativeRotation.eulerAngles - previousRotation.eulerAngles)/deltaTime;

        message.twist.twist.linear = GetGeometryVector3(linearVelocity.Unity2Ros());
        message.twist.twist.angular = GetGeometryVector3(angularVelocity.Unity2Ros());

        previousRealTime = Time.realtimeSinceStartup;
        previousPosition = relativePosition;
        previousRotation = relativeRotation;

        // Publish Odometry Message
        Debug.Log("Publishing OdometryMsg");
        ros.Publish(OdomTopicName, message);
        Debug.Log("Published OdometryMsg");
    }

    private void UpdateMessageUsingWindowsSpatialLocator()
    {
#if WINDOWS_UWP
        // Update header
        message.header.seq++;
        float deltaTime = (float)(Time.realtimeSinceStartup - previousRealTime);
        double midTime = previousRealTime + ((Time.realtimeSinceStartup - previousRealTime) / 2.0);
        message.header.stamp.sec = (uint)midTime;
        message.header.stamp.nanosec = (uint)((midTime - (int)message.header.stamp.sec) * 1e9);

        // Adding the Pose
        Debug.Log("Adding the pose to message using spatial locator");
        // World Spatial Locator is a global variable, access using unityWorldOrigin

        // Getting the Device Spatial Locator
        var perceptionTimestamp = GetCurrentTimestamp();
        var DeviceSpatialLocation = Windows.Perception.Spatial.SpatialLocator.GetDefault().TryLocateAtTimestamp(perceptionTimestamp, unityWorldOrigin); // unityWorldOrigin is a SpatialCoordinateSystem
        var DeviceStationaryCoordSys = Windows.Perception.Spatial.SpatialLocator.GetDefault().CreateStationaryFrameOfReferenceAtCurrentLocation().CoordinateSystem;
        var PreviousInCurrent = Windows.Perception.Spatial.SpatialLocator.GetDefault().TryLocateAtTimestamp(previousTimestamp, DeviceStationaryCoordSys);

        Vector3 devicePosition = new Vector3(DeviceSpatialLocation.Position.X, DeviceSpatialLocation.Position.Y, DeviceSpatialLocation.Position.Z);
        Quaternion deviceRotation = new Quaternion
        {
            x = DeviceSpatialLocation.Orientation.X,
            y = DeviceSpatialLocation.Orientation.Y,
            z = DeviceSpatialLocation.Orientation.Z,
            w = DeviceSpatialLocation.Orientation.W
        };

        // Adding the Pose
        Debug.Log("Adding the pose to message using Windows");
        GetGeometryPoint(devicePosition, message.pose.pose.position);
        GetGeometryQuaternion(deviceRotation, message.pose.pose.orientation);
        Debug.Log("devicePosition POS: " + devicePosition);
        Debug.Log("deviceRotation ORI: " + deviceRotation);

        // Adding the Twist
        Debug.Log("Adding twist to message");
        Vector3 linearVelocity = (devicePosition - previousPosition)/deltaTime;
        linearVelocity = Quaternion.Inverse(deviceRotation) * linearVelocity;
        Vector3 angularVelocity = (deviceRotation.eulerAngles - previousRotation.eulerAngles)/deltaTime;

        linearVelocity.z = -linearVelocity.z; // Undo the flip in z for linear velocity
        message.twist.twist.linear = GetGeometryVector3(linearVelocity);
        angularVelocity = DegToRad(angularVelocity);
        message.twist.twist.angular = GetGeometryVector3(angularVelocity); // JULIA ?????????????????????????????

////////////////////////////////////////////////////
        // // Print PreviousInCurrent
        // var PreviousInCurrentPos = PreviousInCurrent.Position;
        // var PreviousInCurrentOri = PreviousInCurrent.Orientation;
        // Debug.Log("PreviousInCurrent POS: " + PreviousInCurrentPos);
        // Debug.Log("PreviousInCurrent ORI: " + PreviousInCurrentOri);

        // Vector3 prevPos = new Vector3(PreviousInCurrent.Position.X, PreviousInCurrent.Position.Y, PreviousInCurrent.Position.Z);
        // Quaternion prevRot = new Quaternion
        // {
        //     x = PreviousInCurrent.Orientation.X,
        //     y = PreviousInCurrent.Orientation.Y,
        //     z = PreviousInCurrent.Orientation.Z,
        //     w = PreviousInCurrent.Orientation.W
        // };

        // // Adding the Twist from PREV
        // Debug.Log("Adding twist to message");
        // Vector3 linearVelocity = (-prevPos)/deltaTime;
        // Vector3 angularVelocity = (-prevRot.eulerAngles)/deltaTime;

        // message.twist.twist.linear = GetGeometryVector3(linearVelocity);
        // message.twist.twist.angular = GetGeometryVector3(angularVelocity);
/////////////////////////////////////////////////////


        previousRealTime = Time.realtimeSinceStartup;
        previousPosition = devicePosition;
        previousRotation = deviceRotation;

        // Publish Odometry Message
        Debug.Log("Publishing OdometryMsg");
        ros.Publish(OdomTopicName, message);
        Debug.Log("Published OdometryMsg");
#endif
    }

#if WINDOWS_UWP
    private Windows.Perception.PerceptionTimestamp GetCurrentTimestamp()
    {
        // Get the current time, in order to create a PerceptionTimestamp. 
        Windows.Globalization.Calendar c = new Windows.Globalization.Calendar();
        return Windows.Perception.PerceptionTimestampHelper.FromHistoricalTargetTime(c.GetDateTime());
    }

#endif

    // Also clips everything to below +/-9 radians
    private static Vector3 DegToRad(Vector3 vec)
    {
        vec.x = ClipRadians(vec.x * Mathf.PI / 180.0);
        vec.y = ClipRadians(vec.y * Mathf.PI / 180.0);
        vec.z = ClipRadians(vec.z * Mathf.PI / 180.0);
        return vec; // Not sure if this is in place so returning it anyways
    }

    private static float ClipRadians(double r)
    {
        if (r < -9)
        {
            r = -9.0;
        } else if(r > 9)
        {
            r = 9;
        }
        return (float)r;
    }

    private static Vector3Msg GetGeometryVector3(Vector3 vector3)
    {
        Vector3Msg geometryVector3 = new Vector3Msg();
        geometryVector3.x = vector3.x;
        geometryVector3.y = vector3.y;
        geometryVector3.z = -vector3.z;
        return geometryVector3;
    }

    private static void GetGeometryPoint(Vector3 position, PointMsg geometryPoint)
    {
        geometryPoint.x = position.x;
        geometryPoint.y = position.y;
        geometryPoint.z = position.z;
    }

    private static void GetGeometryQuaternion(Quaternion quaternion, QuaternionMsg geometryQuaternion)
    {
        geometryQuaternion.x = quaternion.x;
        geometryQuaternion.y = quaternion.y;
        geometryQuaternion.z = quaternion.z;
        geometryQuaternion.w = quaternion.w;
    }
}