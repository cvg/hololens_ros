using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Runtime.InteropServices;

// JULIA: imports for geometry_msgs
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;
using RosMessageTypes.Sensor;
using RosMessageTypes.BuiltinInterfaces;
using RosMessageTypes.Std;


#if ENABLE_WINMD_SUPPORT
using HL2UnityPlugin;
#endif

public class ResearchModeImuStream : MonoBehaviour
{
    public Transform PublishedTransform;

#if ENABLE_WINMD_SUPPORT
    HL2ResearchMode researchMode;
#endif
    private float[] accelSampleData = null;
    private Vector3 accelVector;

    private float[] gyroSampleData = null;
    private Vector3 gyroEulerAngle;

    private float[] magSampleData = null;

    public Text AccelText = null;
    public Text GyroText = null;
    public Text MagText = null;

    public ImuVisualize RefImuVisualize = null;

    public ConfigReader configReader;

    ROSConnection ros;
    string ImuTopicName = "Imu";

    double[] covariance = new double[9]{0, 0, 0, 0, 0, 0, 0, 0, 0};
    double[] orientation_covariance = new double[9]{-1, 0, 0, 0, 0, 0, 0, 0, 0};
    QuaternionMsg orientation = new QuaternionMsg{
        x = 0,
        y = 0,
        z = 0,
        w = 0
    }; // Getting the orientation from Main Camera Transform in Unity

    IEnumerator Start()
    {
        yield return new WaitUntil(() => configReader.FinishedReader);
        // yield return new WaitForSeconds(3f); // Wait 5 seconds to establish all the ROS connections

#if ENABLE_WINMD_SUPPORT
        researchMode = new HL2ResearchMode();
        researchMode.InitializeAccelSensor();
        researchMode.InitializeGyroSensor();
        researchMode.InitializeMagSensor();

        researchMode.StartAccelSensorLoop();
        researchMode.StartGyroSensorLoop();
        researchMode.StartMagSensorLoop();

        researchMode.PrintAccelExtrinsics();
        researchMode.PrintGyroExtrinsics();
#endif

        if (configReader.FinishedReader == true)
        {
            ros = ROSConnection.GetOrCreateInstance();
            ros.RegisterPublisher<ImuMsg>(ImuTopicName);

            Debug.Log("Registered Imu Publisher");
        }

    }


    void FixedUpdate()
    {
#if ENABLE_WINMD_SUPPORT
        if (researchMode == null) {
            Debug.Log("researchMode was null");
            return;
        }

        // update Gyro Sample
        if (researchMode.GyroSampleUpdated())
        {
            Debug.Log("Getting Gyro Sample");
            gyroSampleData = researchMode.GetGyroSample();
            Debug.Log("Got Gyro Sample " + gyroSampleData);
            if (gyroSampleData.Length == 3)
            {
                // GyroText.text = $"Gyro  : {gyroSampleData[0]:F3}, {gyroSampleData[1]:F3}, {gyroSampleData[2]:F3}";
            }
        }

        // update Mag Sample
        if (researchMode.MagSampleUpdated())
        {
            Debug.Log("Getting Mag Sample");
            magSampleData = researchMode.GetMagSample();
            Debug.Log("Got Mag Sample " + magSampleData);
            if (magSampleData.Length == 3)
            {
                // MagText.text = $"Mag   : {magSampleData[0]:F3}, {magSampleData[1]:F3}, {magSampleData[2]:F3}";
            }
        }

        // update Accel Sample
        if (researchMode.AccelSampleUpdated())
        {
            long ts;
            Debug.Log("Getting Accel Sample");
            accelSampleData = researchMode.GetAccelSample(out ts);
            Debug.Log("Got Accel Sample " + accelSampleData);
            if (accelSampleData.Length == 3)
            {
                // AccelText.text = $"Accel : {accelSampleData[0]:F3}, {accelSampleData[1]:F3}, {accelSampleData[2]:F3}";
            }
        }

        // JULIA: note! Dec 13, only publish IMU if the accelerometer is updated because that's where we get the timestamp
        // Hopefully this doesn't decrease the publishing rate too much, all other sensors should also be updated anyways
        Debug.Log("Setting up ImuMsg to publish");
        Debug.Log("Getting orientation");
        // ORIENTATION
        // QuaternionMsg orientation = this.ConvertToQuaternion(magSampleData); // Gets the orientation from magnetometer
        // GetGeometryQuaternion(PublishedTransform.rotation.Unity2Ros(), orientation);

        Debug.Log("Getting gyro");
        // ANGULAR VELOCITY
        Vector3Msg angular_v = new Vector3Msg(-gyroSampleData[0], -gyroSampleData[1], gyroSampleData[2]); // should be rad
        
        // LINEAR ACCELERATION
        Debug.Log("Getting accel");
        Vector3Msg linear_a = new Vector3Msg(accelSampleData[0], accelSampleData[1], accelSampleData[2]); // should be in m/s^2

        Debug.Log("Getting time as double");
        // // JULIA: Get the Unity time
        // double unity_time = Time.timeAsDouble;
        // // float unity_time = Time.time;

        // uint unity_time_sec = (uint)unity_time;
        // uint unity_time_nano = (uint)((unity_time - (int)unity_time_sec) * 1e9);

        // Old method of using the perception timestamp within the method
        HeaderMsg header = new HeaderMsg(
            0,
            new TimeMsg(),
            "DepthMap"
        );
        var perceptionTimestamp = GetCurrentTimestamp();
        var systemDTOffset = perceptionTimestamp.TargetTime;
        var ts_old = systemDTOffset.ToFileTime();
        // // header.stamp.sec = (uint)(systemDTOffset.Ticks/TimeSpan.TicksPerSecond); // Just the number of seconds
        // header.stamp.sec = (uint)(systemDTOffset.Ticks); // Just the number of hundreds of nanoseconds
        // header.stamp.nanosec = (uint)( (systemDTOffset.Ticks) - (header.stamp.sec*TimeSpan.TicksPerSecond) ) * 100; // Number of ns with the seconds subtracted
        header.stamp.sec = (uint)(ts_old/TimeSpan.TicksPerSecond); // Just the number of seconds
        // header.stamp.sec = (uint)(ts_old); // Just the number of hundredsofnanoseconds
        header.stamp.nanosec = (uint)( (ts_old) - (header.stamp.sec*TimeSpan.TicksPerSecond) ) * 100; // Number of ns with the seconds subtracted

        // // Extracting the Timestamp directly from the Accelerator sensor
        // HeaderMsg header = new HeaderMsg(
        //     0,
        //     new TimeMsg(),
        //     "DepthMap"
        // );
        // header.stamp.sec = (uint)(ts/TimeSpan.TicksPerSecond); // Just the number of seconds
        // header.stamp.nanosec = (uint)( (ts) - (header.stamp.sec*TimeSpan.TicksPerSecond) ) * 100; // Number of ns with the seconds subtracted

        Debug.Log("New Orientation " + orientation.x + orientation.y + orientation.z + orientation.w);
        ImuMsg imuMsg = new ImuMsg(
            header,
            orientation, // Quaternion orientation
            orientation_covariance, // float64[9] orientation_covariance
            angular_v, // Vector3 angular_velocity
            covariance, // float64[9] angular_velocity_covariance
            linear_a, // Vector3 linear_acceleration
            covariance // float64[9] linear_acceleration_covariance
        );

        Debug.Log("Publishing ImuMsg");
        ros.Publish(ImuTopicName, imuMsg);
        Debug.Log("Published ImuMsg");
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

    private Vector3 CreateAccelVector(float[] accelSample)
    {
        Vector3 vector = Vector3.zero;
        if ((accelSample?.Length ?? 0) == 3)
        {
            // Positive directions
            //  accelSample[0] : Down direction
            //  accelSample[1] : Back direction
            //  accelSample[2] : Right direction
            // vector = new Vector3(
            //     accelSample[2],
            //     -1.0f * accelSample[0],
            //     -1.0f * accelSample[1]
            //     );
            vector = new Vector3(
                accelSample[0],
                accelSample[1],
                accelSample[2]
                );
        }
        return vector;
    }

    private Vector3 CreateGyroEulerAngle(float[] gyroSample)
    {
        Vector3 vector = Vector3.zero;
        if ((gyroSample?.Length ?? 0) == 3)
        {
            // Axis of rotation
            //  gyroSample[0] : Unity Y axis(Plus)
            //  gyroSample[1] : Unity Z axis(Plus)
            //  gyroSample[2] : Unity X axis(Plus)

            vector = new Vector3(
                gyroSample[0], // 2
                gyroSample[1], // 0
                gyroSample[2]  // 1
                );
        }
        return vector;
    }

//     private QuaternionMsg ConvertToQuaternion(float[] data)
//     {
// #if ENABLE_WINMD_SUPPORT

//         if ((data?.Length ?? 0) == 3)
//         {
//             // Axis of rotation
//             //  data[0] : Unity Y axis(Plus) pitch
//             //  data[1] : Unity Z axis(Plus) yaw
//             //  data[2] : Unity X axis(Plus) roll
            
//             float roll = data[0]; // was not consistent with the other vector ordering
//             float pitch = data[1];
//             float yaw = data[2];

//             float cr = Mathf.Cos(roll * 0.5f);
//             float sr = Mathf.Sin(roll * 0.5f);
//             float cp = Mathf.Cos(pitch * 0.5f);
//             float sp = Mathf.Sin(pitch * 0.5f);
//             float cy = Mathf.Cos(yaw * 0.5f);
//             float sy = Mathf.Sin(yaw * 0.5f);

//             QuaternionMsg q = new QuaternionMsg(
//                 sr * cp * cy - cr * sp * sy,
//                 cr * sp * cy + sr * cp * sy,
//                 cr * cp * sy - sr * sp * cy,
//                 cr * cp * cy + sr * sp * sy
//             );
//             return q;
//         }
// #endif

//         return new QuaternionMsg();
//     }

    private static void GetGeometryQuaternion(Quaternion quaternion, QuaternionMsg geometryQuaternion)
    {
        geometryQuaternion.x = quaternion.x;
        geometryQuaternion.y = quaternion.y;
        geometryQuaternion.z = quaternion.z;
        geometryQuaternion.w = quaternion.w;
    }

    public void StopSensorsEvent()
    {
#if ENABLE_WINMD_SUPPORT
        researchMode.StopAllSensorDevice();
#endif
    }

    private void OnApplicationFocus(bool focus)
    {
        if (!focus) StopSensorsEvent();
    }
}