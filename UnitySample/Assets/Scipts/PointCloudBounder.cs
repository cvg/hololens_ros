using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.UI.BoundsControl;
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
    GameObject poodle;
    GameObject poodleShadow;

    int x_frames_passed = 0;

    bool controllerMode = false;
    string robot_odom_topic_beagle;
    string robot_odom_topic_poodle;
    string goal_pose_topic_beagle;
    string goal_pose_topic_poodle;

    float shift_dog = -0.3f;
    bool first_odom_beagle = true;
    bool first_odom_poodle = true;

    float latest_odom_height_beagle;
    float latest_odom_height_poodle;

    public ConfigReader configReader;
    ROSConnection ros;

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

        // Register publisher for /desired_pose
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<PoseStampedMsg>(goal_pose_topic_beagle);
        ros.RegisterPublisher<PoseStampedMsg>(goal_pose_topic_poodle);

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

    bool waitForPointCloud()
    {

        return true;
    }

    // Update is called once per frame
    void Update() 
    {
    }

    void OdometryCallbackBeagle(OdometryMsg msg) {
        if (first_odom_beagle) {
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

        latest_odom_height_beagle = position[1];

        // Shift the position down, scaled to local scale, usually because the user starts the app with the beagle in the air
        position = position + new Vector3(0.0f, shift_dog, 0.0f);
        
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

        latest_odom_height_poodle = position[1];

        // Shift the position down, scaled to local scale, usually because the user starts the app with the beagle in the air
        position = position + new Vector3(0.0f, shift_dog, 0.0f);
        
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

        // Set the shadows to be active
        beagleShadow.SetActive(true);
        poodleShadow.SetActive(true);

        // Make Drawing3dManagerGO smaller
        Drawing3dManagerGO.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
        // Reset position of Drawing3dManagerGO
        // Drawing3dManagerGO.transform.position = new Vector3(0.0f, 0.0f, 0.0f);
        // Drawing3dManagerGO.transform.rotation = Quaternion.identity;

        meshRenderer = Drawing3dManagerGO.GetComponent<MeshRenderer>();
        meshRenderer.sharedMaterial.SetFloat("_PointSize", 0.05f * Drawing3dManagerGO.transform.localScale[0]);

        // Add manipulation components to beagleShadow, near interaction grabbable
        objManipulator = beagleShadow.AddComponent<ObjectManipulator>();
        beagleShadow.AddComponent<NearInteractionGrabbable>();
        
        // Add a listener to the objManipulator, where if the beagleShadow is moved, the height is set to 0
        objManipulator.OnManipulationEnded.AddListener((eventData) => {
            beagleShadow.transform.localPosition = new Vector3(beagleShadow.transform.localPosition.x, latest_odom_height_beagle, beagleShadow.transform.localPosition.z);
            // set beagleShadow rotation as euler angles, but only around y-axis
            // beagleShadow.transform.rotation = Quaternion.Euler(0.0f, beagleShadow.transform.rotation.eulerAngles[1], 0.0f);
            beagleShadow.transform.localRotation = Quaternion.Euler(0.0f, beagleShadow.transform.localRotation.eulerAngles[1], 0.0f);

            Vector3 position = beagleShadow.transform.localPosition;
            Quaternion rotation = beagleShadow.transform.localRotation;

            beagleShadow.transform.localPosition = beagleShadow.transform.localPosition + new Vector3(0.0f, shift_dog, 0.0f);

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
        objManipulator = poodleShadow.AddComponent<ObjectManipulator>();
        poodleShadow.AddComponent<NearInteractionGrabbable>();
        
        // Add a listener to the objManipulator, where if the poodleShadow is moved, the height is set to 0
        objManipulator.OnManipulationEnded.AddListener((eventData) => {
            poodleShadow.transform.localPosition = new Vector3(poodleShadow.transform.localPosition.x, latest_odom_height_poodle, poodleShadow.transform.localPosition.z);
            // set poodleShadow rotation as euler angles, but only around y-axis
            // poodleShadow.transform.rotation = Quaternion.Euler(0.0f, poodleShadow.transform.rotation.eulerAngles[1], 0.0f);
            poodleShadow.transform.localRotation = Quaternion.Euler(0.0f, poodleShadow.transform.localRotation.eulerAngles[1], 0.0f);

            Vector3 position = poodleShadow.transform.localPosition;
            Quaternion rotation = poodleShadow.transform.localRotation;

            poodleShadow.transform.localPosition = poodleShadow.transform.localPosition + new Vector3(0.0f, shift_dog, 0.0f);

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
