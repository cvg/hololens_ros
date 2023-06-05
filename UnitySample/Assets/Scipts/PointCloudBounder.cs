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
    GameObject poodle;

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

        beagle.transform.parent = Drawing3dManagerGO.transform;
        poodle.transform.parent = Drawing3dManagerGO.transform;

        beagle.SetActive(false);
        poodle.SetActive(false);

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

        // Unsubscribe from anymal/odom
        if (ros.HasSubscriber(robot_odom_topic_beagle))
        {
            ros.Unsubscribe(robot_odom_topic_beagle);
        }
        if (ros.HasSubscriber(robot_odom_topic_poodle))
        {
            ros.Unsubscribe(robot_odom_topic_poodle);
        }

        // Make Drawing3dManagerGO smaller
        Drawing3dManagerGO.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
        // Reset position of Drawing3dManagerGO
        // Drawing3dManagerGO.transform.position = new Vector3(0.0f, 0.0f, 0.0f);
        // Drawing3dManagerGO.transform.rotation = Quaternion.identity;

        meshRenderer = Drawing3dManagerGO.GetComponent<MeshRenderer>();
        meshRenderer.sharedMaterial.SetFloat("_PointSize", 0.05f * Drawing3dManagerGO.transform.localScale[0]);

        // Add manipulation components to beagle, near interaction grabbable
        objManipulator = beagle.AddComponent<ObjectManipulator>();
        beagle.AddComponent<NearInteractionGrabbable>();
        
        // Add a listener to the objManipulator, where if the beagle is moved, the height is set to 0
        objManipulator.OnManipulationEnded.AddListener((eventData) => {
            beagle.transform.localPosition = new Vector3(beagle.transform.localPosition.x, latest_odom_height_beagle, beagle.transform.localPosition.z);
            // set beagle rotation as euler angles, but only around y-axis
            // beagle.transform.rotation = Quaternion.Euler(0.0f, beagle.transform.rotation.eulerAngles[1], 0.0f);
            beagle.transform.localRotation = Quaternion.Euler(0.0f, beagle.transform.localRotation.eulerAngles[1], 0.0f);

            Vector3 position = beagle.transform.localPosition;
            Quaternion rotation = beagle.transform.localRotation;

            beagle.transform.localPosition = beagle.transform.localPosition + new Vector3(0.0f, shift_dog, 0.0f);

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

        // Add manipulation components to poodle, near interaction grabbable
        objManipulator = poodle.AddComponent<ObjectManipulator>();
        poodle.AddComponent<NearInteractionGrabbable>();
        
        // Add a listener to the objManipulator, where if the poodle is moved, the height is set to 0
        objManipulator.OnManipulationEnded.AddListener((eventData) => {
            poodle.transform.localPosition = new Vector3(poodle.transform.localPosition.x, latest_odom_height_poodle, poodle.transform.localPosition.z);
            // set poodle rotation as euler angles, but only around y-axis
            // poodle.transform.rotation = Quaternion.Euler(0.0f, poodle.transform.rotation.eulerAngles[1], 0.0f);
            poodle.transform.localRotation = Quaternion.Euler(0.0f, poodle.transform.localRotation.eulerAngles[1], 0.0f);

            Vector3 position = poodle.transform.localPosition;
            Quaternion rotation = poodle.transform.localRotation;

            poodle.transform.localPosition = poodle.transform.localPosition + new Vector3(0.0f, shift_dog, 0.0f);

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
        // Check if subscribed to anymal/odom, otherwise subscribe to anymal/odom
        if (!ros.HasSubscriber(robot_odom_topic_beagle))
        {
            ros.Subscribe<OdometryMsg>(robot_odom_topic_beagle, OdometryCallbackBeagle);
        }

        if (!ros.HasSubscriber(robot_odom_topic_poodle))
        {
            ros.Subscribe<OdometryMsg>(robot_odom_topic_poodle, OdometryCallbackPoodle);
        }

        // TODO: set things up for poodle
        DestroyBeagleComponents();
        DestroyPoodleComponents();

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
        DestroyBeagleComponents();
        DestroyPoodleComponents();

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

    void DestroyBeagleComponents()
    {
        // Remove all the new components added from manipulator
        Destroy(beagle.GetComponent<ObjectManipulator>());
        Destroy(beagle.GetComponent<NearInteractionGrabbable>());
        Destroy(beagle.GetComponent<ConstraintManager>());
    }

    void DestroyPoodleComponents()
    {
        // Remove all the new components added from manipulator
        Destroy(poodle.GetComponent<ObjectManipulator>());
        Destroy(poodle.GetComponent<NearInteractionGrabbable>());
        Destroy(poodle.GetComponent<ConstraintManager>());
    }

    public void RescalePointSize()
    {
        meshRenderer.sharedMaterial.shader = Shader.Find("Point Cloud/singleDisk");
        meshRenderer.sharedMaterial.SetFloat("_PointSize", 0.05f * Drawing3dManagerGO.transform.localScale[0]);
    }
}
