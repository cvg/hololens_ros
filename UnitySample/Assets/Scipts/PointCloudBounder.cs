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
    GameObject beagle; // For visualizing beagle location
    GameObject beagleShadow; // For controlling beagle
    GameObject poodle; // For visualizing poodle location
    GameObject poodleShadow; // For controlling poodle

    int x_frames_passed = 0;

    bool controllerMode = false;

    string overlay_alignment_tr;
    string robot_odom_topic_beagle;
    string robot_odom_topic_poodle;
    string goal_pose_topic_beagle;
    string goal_pose_topic_poodle;

    bool use_multi_floor;

    float shift_dog = -0.3f;
    bool first_odom_beagle = true;
    bool first_odom_poodle = true;

    float latest_odom_height_beagle;
    float latest_odom_height_poodle;

    public ConfigReader configReader;
    ROSConnection ros;

    // TODO:
    // - Generalize to N agents
    // - Fix height offset
    // - Change the odometry shadow to an arrow in controller mode

    // Start is called before the first frame update
    IEnumerator Start()
    {
        // Subscribe to a PoseStamped topic
        yield return new WaitUntil(() => configReader.FinishedReader);   

        // Set a robot odom topic variable
        robot_odom_topic_beagle = configReader.robot_odom_topic_beagle;
        robot_odom_topic_poodle = configReader.robot_odom_topic_poodle;

        // Set goal pose topic variable
        goal_pose_topic_beagle = configReader.goal_pose_topic_beagle;
        goal_pose_topic_poodle = configReader.goal_pose_topic_poodle;

        // Set Overlay Alignment Transform publishing topic
        overlay_alignment_tr = configReader.overlay_alignment_tr;

        // Flag for using multi-floor, the goal pose sent for ControllerMode will have free height
        use_multi_floor = configReader.use_multi_floor;

        // Register publisher for /desired_pose
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<PoseStampedMsg>(goal_pose_topic_beagle);
        ros.RegisterPublisher<PoseStampedMsg>(goal_pose_topic_poodle);

        // Register publisher for /overlay_alignment_tr
        ros.RegisterPublisher<TransformMsg>(overlay_alignment_tr);

        Drawing3dManagerGO = GameObject.Find("Drawing3dManager");
        meshRenderer = Drawing3dManagerGO.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = (Material)Resources.Load("Default Point", typeof(Material));
        meshRenderer.sharedMaterial.shader = Shader.Find("Point Cloud/singleDisk");
        // meshRenderer.sharedMaterial.shader = Shader.Find("Point Cloud/Point");

        // Get the beagle in the scene
        beagle = GameObject.Find("Beagle");
        poodle = GameObject.Find("Poodle");
        beagleShadow = GameObject.Find("BeagleShadow");
        poodleShadow = GameObject.Find("PoodleShadow");

        beagle.transform.parent = Drawing3dManagerGO.transform;
        poodle.transform.parent = Drawing3dManagerGO.transform;
        beagleShadow.transform.parent = Drawing3dManagerGO.transform;
        poodleShadow.transform.parent = Drawing3dManagerGO.transform;

        beagle.SetActive(false);
        poodle.SetActive(false);
        beagleShadow.SetActive(false);
        poodleShadow.SetActive(false);

        // Check if subscribed to anymal/odom, otherwise subscribe to anymal/odom
        if (!ros.HasSubscriber(robot_odom_topic_beagle))
        {
            ros.Subscribe<OdometryMsg>(robot_odom_topic_beagle, OdometryCallbackBeagle);
        }

        if (!ros.HasSubscriber(robot_odom_topic_poodle))
        {
            ros.Subscribe<OdometryMsg>(robot_odom_topic_poodle, OdometryCallbackPoodle);
        }

        // Logic to start in controller mode
        yield return new WaitUntil(() => GameObject.Find("PointCloud") != null);
        ControllerMode();
    }

    // bool waitForPointCloud()
    // {
    //     return true;
    // }

    void OdometryCallbackBeagle(OdometryMsg msg) {
        if (first_odom_beagle) { // TODO: Check the first_odom_beagle for other things
            beagle.SetActive(true);
            first_odom_beagle = false;
        }

        // Extract xyz pose information from msg
        Vector3 position = new Vector3((float)msg.pose.pose.position.x, (float)msg.pose.pose.position.y, (float)msg.pose.pose.position.z);
        // Extract quaternion pose information from msg
        Quaternion rotation = new Quaternion((float)msg.pose.pose.orientation.x, (float)msg.pose.pose.orientation.y, (float)msg.pose.pose.orientation.z, (float)msg.pose.pose.orientation.w);

        // Convert position from ROS to Unity
        position = position.Ros2Unity();
        // Convert rotation from ROS to Unity
        rotation = rotation.Ros2Unity();

        // latest_odom_height_beagle = position[1];

        // Shift the position down, scaled to local scale, because the origin of dog is at feet
        position = position + new Vector3(0.0f, shift_dog, 0.0f);

        latest_odom_height_beagle = position[1];
        
        // Set local transform of beagle
        beagle.transform.localPosition = position;
        beagle.transform.localRotation = rotation;
    }

    void OdometryCallbackPoodle(OdometryMsg msg) {
        if (first_odom_poodle) {
            poodle.SetActive(true);
            first_odom_poodle = false;
        }

        // Extract xyz pose information from msg
        Vector3 position = new Vector3((float)msg.pose.pose.position.x, (float)msg.pose.pose.position.y, (float)msg.pose.pose.position.z);
        // Extract quaternion pose information from msg
        Quaternion rotation = new Quaternion((float)msg.pose.pose.orientation.x, (float)msg.pose.pose.orientation.y, (float)msg.pose.pose.orientation.z, (float)msg.pose.pose.orientation.w);

        // Convert position from ROS to Unity
        position = position.Ros2Unity();
        // Convert rotation from ROS to Unity
        rotation = rotation.Ros2Unity();

        // latest_odom_height_poodle = position[1];

        // Shift the position down, scaled to local scale, usually because the user starts the app with the beagle in the air
        position = position + new Vector3(0.0f, shift_dog, 0.0f);

        latest_odom_height_poodle = position[1];
        
        // Set local transform of beagle
        poodle.transform.localPosition = position;
        poodle.transform.localRotation = rotation;
    }

    public void ControllerMode()
    {
        controllerMode = true;

        DestroyDrawing3dManagerComponents();

        // Set the position of the shadows to be the last known position of the dogs
        // The actual yellow beagle and poodle will always track the odom messages
        beagleShadow.transform.localPosition = beagle.transform.localPosition;
        beagleShadow.transform.localRotation = beagle.transform.localRotation;
        poodleShadow.transform.localPosition = poodle.transform.localPosition;
        poodleShadow.transform.localRotation = poodle.transform.localRotation;

        // Set the shadows to be active only if odometry messages have been heard
        if (!first_odom_beagle) { // Odometry message came in already
            beagleShadow.SetActive(true);
        }
        if (!first_odom_poodle) {
            poodleShadow.SetActive(true); // Odometry message came in already
        }

        // Make Drawing3dManagerGO smaller
        Drawing3dManagerGO.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f); // Don't need to set the position or rotation

        meshRenderer = Drawing3dManagerGO.GetComponent<MeshRenderer>();
        meshRenderer.sharedMaterial.SetFloat("_PointSize", 0.05f * Drawing3dManagerGO.transform.localScale[0]);

        // Add manipulation components to beagleShadow, near interaction grabbable
        ObjectManipulator beagleObjManipulator = beagleShadow.AddComponent<ObjectManipulator>();
        beagleShadow.AddComponent<NearInteractionGrabbable>();
        
        // Add a listener to the beagleObjManipulator, where if the beagleShadow is moved, the height is set to 0
        beagleObjManipulator.OnManipulationEnded.AddListener((eventData) => {
            if (!use_multi_floor) {
                beagleShadow.transform.localPosition = new Vector3(beagleShadow.transform.localPosition.x, latest_odom_height_beagle, beagleShadow.transform.localPosition.z);
            } else {
                // Do nothing to localPosition
            }
            // set beagleShadow rotation as euler angles, but only around y-axis
            beagleShadow.transform.localRotation = Quaternion.Euler(0.0f, beagleShadow.transform.localRotation.eulerAngles[1], 0.0f);

            Vector3 position = beagleShadow.transform.localPosition;
            position = position - new Vector3(0.0f, shift_dog, 0.0f); // Shift the position back up before publishing
            Quaternion rotation = beagleShadow.transform.localRotation;

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
            ros.Send(goal_pose_topic_beagle, msg);
        });

        // Add manipulation components to poodleShadow, near interaction grabbable
        ObjectManipulator poodleObjManipulator = poodleShadow.AddComponent<ObjectManipulator>();
        poodleShadow.AddComponent<NearInteractionGrabbable>();
        
        // Add a listener to the poodleObjManipulator, where if the poodleShadow is moved, the height is set to 0
        poodleObjManipulator.OnManipulationEnded.AddListener((eventData) => {
            if (!use_multi_floor) {
                poodleShadow.transform.localPosition = new Vector3(poodleShadow.transform.localPosition.x, latest_odom_height_poodle, poodleShadow.transform.localPosition.z);
            } else {
                // Do nothing to the localPosition
            }
            // set poodleShadow rotation as euler angles, but only around y-axis
            poodleShadow.transform.localRotation = Quaternion.Euler(0.0f, poodleShadow.transform.localRotation.eulerAngles[1], 0.0f);

            Vector3 position = poodleShadow.transform.localPosition;
            position = position - new Vector3(0.0f, shift_dog, 0.0f); // Shift the position back up before publishing
            Quaternion rotation = poodleShadow.transform.localRotation;

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
            ros.Send(goal_pose_topic_poodle, msg);
        });
    }

    public void BoundPointCloud()
    {
        // Set shadows to inactive
        beagleShadow.SetActive(false);
        poodleShadow.SetActive(false);

        // Check if subscribed to anymal/odom, otherwise subscribe to anymal/odom
        if (!ros.HasSubscriber(robot_odom_topic_beagle))
        {
            ros.Subscribe<OdometryMsg>(robot_odom_topic_beagle, OdometryCallbackBeagle);
        }

        if (!ros.HasSubscriber(robot_odom_topic_poodle))
        {
            ros.Subscribe<OdometryMsg>(robot_odom_topic_poodle, OdometryCallbackPoodle);
        }

        DestroyBeagleShadowComponents();
        DestroyPoodleShadowComponents();

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
        beagleShadow.SetActive(false);
        poodleShadow.SetActive(false);

        // Check if subscribed to anymal/odom, otherwise subscribe to anymal/odom
        if (!ros.HasSubscriber(robot_odom_topic_beagle))
        {
            ros.Subscribe<OdometryMsg>(robot_odom_topic_beagle, OdometryCallbackBeagle);
        }

        if (!ros.HasSubscriber(robot_odom_topic_poodle))
        {
            ros.Subscribe<OdometryMsg>(robot_odom_topic_poodle, OdometryCallbackPoodle);
        }

        controllerMode = false;
        // Remove all the new components added from bounds
        DestroyDrawing3dManagerComponents();
        DestroyBeagleShadowComponents();
        DestroyPoodleShadowComponents();

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

        // Interact with the overlay and publish the transform of Unity frame
        // Remove all the new components added from bounds, no shadow dog controller
        DestroyDrawing3dManagerComponents();
        DestroyBeagleShadowComponents();
        DestroyPoodleShadowComponents();

        // Set shadows to inactive
        beagleShadow.SetActive(false); // TODO: double check ghost behavior
        poodleShadow.SetActive(false);

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

    void DestroyBeagleShadowComponents()
    {
        // Remove all the new components added from manipulator
        Destroy(beagleShadow.GetComponent<ObjectManipulator>());
        Destroy(beagleShadow.GetComponent<NearInteractionGrabbable>());
        Destroy(beagleShadow.GetComponent<ConstraintManager>());
    }

    void DestroyPoodleShadowComponents()
    {
        // Remove all the new components added from manipulator
        Destroy(poodleShadow.GetComponent<ObjectManipulator>());
        Destroy(poodleShadow.GetComponent<NearInteractionGrabbable>());
        Destroy(poodleShadow.GetComponent<ConstraintManager>());
    }

    public void RescalePointSize()
    {
        meshRenderer.sharedMaterial.shader = Shader.Find("Point Cloud/singleDisk");
        meshRenderer.sharedMaterial.SetFloat("_PointSize", 0.05f * Drawing3dManagerGO.transform.localScale[0]);
    }
}
