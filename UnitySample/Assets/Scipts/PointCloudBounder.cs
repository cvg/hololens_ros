using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.UI.BoundsControl;
using Microsoft.MixedReality.Toolkit.Utilities;
using Pcx;

using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;
using RosMessageTypes.Sensor;
using RosMessageTypes.BuiltinInterfaces;
using RosMessageTypes.Std;
using RosMessageTypes.Nav;

using Unity.Robotics.Visualizations;

public class PointCloudBounder : MonoBehaviour
{

    GameObject Drawing3dManagerGO;
    ObjectManipulator objManipulator;
    MeshRenderer meshRenderer;
    BoundsControl boundsControl;
    GameObject beagle;
    GameObject beagleShadow;
    Dictionary<string, GameObject> robots = new Dictionary<string, GameObject>();        // For visualizing robots
    Dictionary<string, GameObject> robot_shadows = new Dictionary<string, GameObject>(); // For controlling robots

    int x_frames_passed = 0;

    bool controllerMode = false;

    string overlay_alignment_tr;
    string[] robot_odom_topics;
    string[] goal_pose_topics;
    int num_robots;

    bool use_multi_floor;

    float shift_dog = -0.3f;
    // Dictonary that maps robot_odom_topic to boolean
    Dictionary<string, bool> first_odom = new Dictionary<string, bool>();
    Dictionary<string, float> latest_odom_height = new Dictionary<string, float>();

    public ConfigReader configReader;
    ROSConnection ros;

    // Start is called before the first frame update
    IEnumerator Start()
    {
        // Subscribe to a PoseStamped topic
        yield return new WaitUntil(() => configReader.FinishedReader);   

        // Set topics
        robot_odom_topics = configReader.robot_odom_topics; // Using the robot_odom topic name as the key for all the information currently
        goal_pose_topics = configReader.goal_pose_topics;
        num_robots = robot_odom_topics.Length; // Should be same as goal_pose_topics length

        // Set Overlay Alignment Transform publishing topic
        overlay_alignment_tr = configReader.overlay_alignment_tr;

        // Flag for using multi-floor, the goal pose sent for ControllerMode will have free height
        use_multi_floor = configReader.use_multi_floor;

        // Register publisher for /desired_pose
        ros = ROSConnection.GetOrCreateInstance();
        for (int i = 0; i < num_robots; i++) {
            ros.RegisterPublisher<PoseStampedMsg>(goal_pose_topics[i]);

            // Also set dictionary first_odom and height
            first_odom.Add(robot_odom_topics[i], true);
            latest_odom_height.Add(robot_odom_topics[i], 0.0f);
        }

        // Register publisher for /overlay_alignment_tr
        ros.RegisterPublisher<TransformMsg>(overlay_alignment_tr);

        beagle = GameObject.Find("Beagle");
        beagleShadow = GameObject.Find("BeagleShadow");

        Drawing3dManagerGO = GameObject.Find("Drawing3dManager");
        meshRenderer = Drawing3dManagerGO.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = (Material)Resources.Load("Default Point", typeof(Material));
        meshRenderer.sharedMaterial.shader = Shader.Find("Point Cloud/singleDisk");
        // meshRenderer.sharedMaterial.shader = Shader.Find("Point Cloud/Point");

        // Initialize robot GameObjects
        for (int i = 0; i < num_robots; i++) {
            // Clone the beagle GameObject
            GameObject robot = GameObject.Instantiate(beagle);
            robot.name = robot_odom_topics[i]; // Temporary for now, will be changed to robot name
            robot.transform.parent = Drawing3dManagerGO.transform;

            GameObject robotShadow = GameObject.Instantiate(beagleShadow);
            robotShadow.name = "shadow_" + robot_odom_topics[i]; // Temporary
            robotShadow.transform.parent = Drawing3dManagerGO.transform;

            robot.SetActive(false);
            robotShadow.SetActive(false);

            // Add to dictionaries
            robots[robot_odom_topics[i]] = robot;
            robot_shadows[robot_odom_topics[i]] = robotShadow;
        }

        // Dummy GameObjects used to initialize the other robots
        beagle.SetActive(false);
        beagleShadow.SetActive(false);

        // Check if subscribed to anymal/odom, otherwise subscribe to anymal/odom
        for (int i = 0; i < num_robots; i++) {
            if (!ros.HasSubscriber(robot_odom_topics[i]))
            {
                System.Action<OdometryMsg> wrappedCallback = (msg) => OdometryCallback(msg, robot_odom_topics[i]);
                ros.Subscribe<OdometryMsg>(robot_odom_topics[i], wrappedCallback);
            }
        }

        // Logic to start in controller mode
        yield return new WaitUntil(() => GameObject.Find("PointCloud") != null);
        ControllerMode();
    }

    void OdometryCallback(OdometryMsg msg, string robot_odom_topic) {
        GameObject currentRobot = robots[robot_odom_topic];

        if (first_odom[robot_odom_topic]) {
            currentRobot.SetActive(true);
            first_odom[robot_odom_topic] = false;
        }

        // Extract xyz pose information from msg
        Vector3 position = new Vector3((float)msg.pose.pose.position.x, (float)msg.pose.pose.position.y, (float)msg.pose.pose.position.z);
        // Extract quaternion pose information from msg
        Quaternion rotation = new Quaternion((float)msg.pose.pose.orientation.x, (float)msg.pose.pose.orientation.y, (float)msg.pose.pose.orientation.z, (float)msg.pose.pose.orientation.w);

        // Convert position from ROS to Unity
        position = position.Ros2Unity();
        // Convert rotation from ROS to Unity
        rotation = rotation.Ros2Unity();

        latest_odom_height[robot_odom_topic] = position[1];

        // Shift the position down, scaled to local scale, usually because the user starts the app with the robot in the air
        position = position + new Vector3(0.0f, shift_dog, 0.0f);
        
        // Set local transform of robot
        currentRobot.transform.localPosition = position;
        currentRobot.transform.localRotation = rotation;
    }

    public void ControllerMode()
    {
        controllerMode = true;

        DestroyDrawing3dManagerComponents();

        // Go through all the robots
        for (int i = 0; i < num_robots; i++) {
            GameObject robot = robots[robot_odom_topics[i]];
            GameObject robotShadow = robot_shadows[robot_odom_topics[i]];

            // Set the position of the shadows to be the last known position of the robots
            // The actual robots will always track the positions
            robotShadow.transform.localPosition = robot.transform.localPosition;
            robotShadow.transform.localRotation = robot.transform.localRotation;

            if (!first_odom[robot_odom_topics[i]]) { // Odometry message came in already
                robotShadow.SetActive(true);
            }

            // Add manipulation components to shadows, near interaction grabbable
            ObjectManipulator objManipulator = robotShadow.AddComponent<ObjectManipulator>();
            robotShadow.AddComponent<NearInteractionGrabbable>();

            // Add a listener to the beagleObjManipulator, where if the robotShadow is moved, the height is set to 0
            objManipulator.OnManipulationEnded.AddListener((eventData) => {
                if (!use_multi_floor) {
                    robotShadow.transform.localPosition = new Vector3(robotShadow.transform.localPosition.x, latest_odom_height[robot_odom_topics[i]], robotShadow.transform.localPosition.z);
                } else {
                    // Do nothing to localPosition
                }
                // set robotShadow rotation as euler angles, but only around y-axis
                robotShadow.transform.localRotation = Quaternion.Euler(0.0f, robotShadow.transform.localRotation.eulerAngles[1], 0.0f);

                Vector3 position = robotShadow.transform.localPosition;
                Quaternion rotation = robotShadow.transform.localRotation;

                if (!use_multi_floor) {
                    robotShadow.transform.localPosition = robotShadow.transform.localPosition + new Vector3(0.0f, shift_dog, 0.0f);
                } else {
                    // Do nothing, the offset is already "included in user manipulation"
                }

                position = position.Unity2Ros();
                rotation = rotation.Unity2Ros();

                // Create a PoseStampedMsg
                PoseStampedMsg msg = new PoseStampedMsg();
                // Set the header of the msg
                msg.header.frame_id = "map";
                // Set the pose of the msg
                msg.pose.position.x = position[0];
                msg.pose.position.y = position[1];
                msg.pose.position.z = position[2];
                msg.pose.orientation.x = rotation[0];
                msg.pose.orientation.y = rotation[1];
                msg.pose.orientation.z = rotation[2];
                msg.pose.orientation.w = rotation[3];

                // Publish the msg
                ros.Send(goal_pose_topics[i], msg);
            });
        }

        // Make Drawing3dManagerGO smaller
        Drawing3dManagerGO.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f); // Don't need to set the position or rotation

        meshRenderer = Drawing3dManagerGO.GetComponent<MeshRenderer>();
        meshRenderer.sharedMaterial.SetFloat("_PointSize", 0.05f * Drawing3dManagerGO.transform.localScale[0]);
    }

    public void BoundPointCloud()
    {
        // Set shadows to inactive
        for (int i = 0; i < num_robots; i++) {
            robot_shadows[robot_odom_topics[i]].SetActive(false);

            // Check if subscribed to anymal/odom, otherwise subscribe to anymal/odom
            if (!ros.HasSubscriber(robot_odom_topics[i]))
            {
                System.Action<OdometryMsg> wrappedCallback = (msg) => OdometryCallback(msg, robot_odom_topics[i]);
                ros.Subscribe<OdometryMsg>(robot_odom_topics[i], wrappedCallback);
            }

            DestroyShadowComponents(i);
        }

        controllerMode = false;
        Drawing3dManagerGO.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
        Drawing3dManagerGO.AddComponent<BoxCollider>();
        if (boundsControl == null) {
            boundsControl = Drawing3dManagerGO.AddComponent<BoundsControl>();
        } else {
            Destroy(boundsControl);
            boundsControl = Drawing3dManagerGO.AddComponent<BoundsControl>();
        }
        UnityEvent rescale = new UnityEvent();
        rescale.AddListener(RescalePointSize);
        boundsControl.ScaleStopped = rescale;

        meshRenderer = Drawing3dManagerGO.GetComponent<MeshRenderer>();
        meshRenderer.sharedMaterial.SetFloat("_PointSize", 0.05f * Drawing3dManagerGO.transform.localScale[0]);

        objManipulator = Drawing3dManagerGO.AddComponent<ObjectManipulator>();
        objManipulator.AllowFarManipulation = true;
        Drawing3dManagerGO.AddComponent<NearInteractionGrabbable>();
        Debug.Log("Added bounds");
    }

    public void OverlayPointCloud()
    {
        // Set shadows to inactive
        for (int i = 0; i < num_robots; i++) {
            robot_shadows[robot_odom_topics[i]].SetActive(false);

            // Check if subscribed to anymal/odom, otherwise subscribe to anymal/odom
            if (!ros.HasSubscriber(robot_odom_topics[i]))
            {
                System.Action<OdometryMsg> wrappedCallback = (msg) => OdometryCallback(msg, robot_odom_topics[i]);
                ros.Subscribe<OdometryMsg>(robot_odom_topics[i], wrappedCallback);
            }

            DestroyShadowComponents(i);
        }

        controllerMode = false;
        // Remove all the new components added from bounds
        DestroyDrawing3dManagerComponents();

        // Reset scale of Drawing3dManagerGO
        Drawing3dManagerGO.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
        // Reset position of Drawing3dManagerGO
        Drawing3dManagerGO.transform.position = new Vector3(0.0f, 0.0f, 0.0f);
        Drawing3dManagerGO.transform.rotation = Quaternion.identity;

        meshRenderer = Drawing3dManagerGO.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.sharedMaterial.SetFloat("_PointSize", 0.02f * Drawing3dManagerGO.transform.localScale[0]);
            Debug.Log("Added overlay");
        }
    }

    public void InteractiveOverlayPointCloud()
    {
        // TODO: Check everything still works with other functionalities
        controllerMode = false;

        for (int i = 0; i < num_robots; i++) {
            robot_shadows[robot_odom_topics[i]].SetActive(false);

            DestroyShadowComponents(i);
        }

        // Interact with the overlay and publish the transform of Unity frame
        // Remove all the new components added from bounds, no shadow dog controller
        DestroyDrawing3dManagerComponents();

        // Reset scale of Drawing3dManagerGO
        Drawing3dManagerGO.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
        // Reset position of Drawing3dManagerGO
        Drawing3dManagerGO.transform.position = new Vector3(0.0f, 0.0f, 0.0f);
        Drawing3dManagerGO.transform.rotation = Quaternion.identity;

        meshRenderer = Drawing3dManagerGO.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.sharedMaterial.SetFloat("_PointSize", 0.02f * Drawing3dManagerGO.transform.localScale[0]);
            Debug.Log("Added overlay");
        }

        Drawing3dManagerGO.AddComponent<BoxCollider>();
        // TODO: try without bounds control!
        if (boundsControl == null) {
            boundsControl = Drawing3dManagerGO.AddComponent<BoundsControl>();
        } else {
            Destroy(boundsControl);
            boundsControl = Drawing3dManagerGO.AddComponent<BoundsControl>();
        }

        if (objManipulator == null) {
            objManipulator = Drawing3dManagerGO.AddComponent<ObjectManipulator>();
        } else {
            Destroy(objManipulator);
            objManipulator = Drawing3dManagerGO.AddComponent<ObjectManipulator>();
        }
        // objManipulator.AllowFarManipulation = true; // TODO: TRY?
        // Set Manipulation type to OneHanded // TODO: Test if this actually works!!
        objManipulator.ManipulationType = ManipulationHandFlags.OneHanded;
        Drawing3dManagerGO.AddComponent<NearInteractionGrabbable>();

        // Turn on smoothing
        objManipulator.SmoothingNear = true;
        objManipulator.SmoothingFar = true;
        objManipulator.MoveLerpTime = 0.1f; // TODO: This value kind of works! But need to tune it.
        objManipulator.RotateLerpTime = 0.1f;

        // Once finished moving the objManipulator, publish the transform of Drawing3dManagerGO
        objManipulator.OnManipulationEnded.AddListener((eventData) => {
            // Get the position and rotation of Drawing3dManagerGO
            Vector3 position = Drawing3dManagerGO.transform.position; // We want transform relative to the whole Unity world frame
            Quaternion rotation = Drawing3dManagerGO.transform.rotation;

            position = position.Unity2Ros();
            rotation = rotation.Unity2Ros();

            // Create a TransformMsg
            TransformMsg msg = new TransformMsg();
            // Set the translation and rotation of he msg
            msg.translation.x = position[0];
            msg.translation.y = position[1];
            msg.translation.z = position[2];
            msg.rotation.x = rotation[0];
            msg.rotation.y = rotation[1];
            msg.rotation.z = rotation[2];
            msg.rotation.w = rotation[3];

            // Publish the msg
            ros.Send(overlay_alignment_tr, msg);
        });        
    }

    void DestroyDrawing3dManagerComponents() 
    {
        // Remove all the new components added from manipulator
        Destroy(Drawing3dManagerGO.GetComponent<ObjectManipulator>());
        Destroy(Drawing3dManagerGO.GetComponent<NearInteractionGrabbable>());
        Destroy(Drawing3dManagerGO.GetComponent<BoxCollider>());
        Destroy(Drawing3dManagerGO.GetComponent<BoundsControl>());
        Destroy(Drawing3dManagerGO.GetComponent<ConstraintManager>());
    }

    void DestroyShadowComponents(int i)
    {
        // Remove all the new components added from manipulator
        Destroy(robot_shadows[robot_odom_topics[i]].GetComponent<ObjectManipulator>());
        Destroy(robot_shadows[robot_odom_topics[i]].GetComponent<NearInteractionGrabbable>());
        Destroy(robot_shadows[robot_odom_topics[i]].GetComponent<ConstraintManager>());
    }

    public void RescalePointSize()
    {
        meshRenderer.sharedMaterial.shader = Shader.Find("Point Cloud/singleDisk");
        meshRenderer.sharedMaterial.SetFloat("_PointSize", 0.05f * Drawing3dManagerGO.transform.localScale[0]);
    }
}
