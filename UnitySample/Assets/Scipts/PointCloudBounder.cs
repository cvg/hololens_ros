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

    int x_frames_passed = 0;

    bool controllerMode = false;
    string robot_odom_topic;
    string goal_pose_topic;

    float shift_dog = 0f;

    public ConfigReader configReader;
    ROSConnection ros;

    // Start is called before the first frame update
    IEnumerator Start()
    {
        // Subscribe to a PoseStamped topic
        yield return new WaitUntil(() => configReader.FinishedReader);   
        // Get the beagle in the scene
        beagle = GameObject.Find("Beagle");

        // Set a robot odom topic variable
        robot_odom_topic = configReader.robot_odom_topic;

        // Set goal pose topic variable
        goal_pose_topic = "/unity_goal_pose";

        // Register publisher for /desired_pose
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<PoseStampedMsg>(goal_pose_topic);

        Drawing3dManagerGO = GameObject.Find("Drawing3dManager");
        meshRenderer = Drawing3dManagerGO.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = (Material)Resources.Load("Default Point", typeof(Material));
        meshRenderer.sharedMaterial.shader = Shader.Find("Point Cloud/singleDisk");
        // meshRenderer.sharedMaterial.shader = Shader.Find("Point Cloud/Point");

        beagle.transform.parent = Drawing3dManagerGO.transform;

        // Check if subscribed to anymal/odom, otherwise subscribe to anymal/odom
        if (!ros.HasSubscriber(robot_odom_topic))
        {
            ros.Subscribe<OdometryMsg>(robot_odom_topic, OdometryCallback);
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
        // On the actual HoloLens it would take too much processing power to update the desired_pose every frame
        x_frames_passed += 1;
        // If in controllerMode and beagle is moved, update the desired_pose
        if (controllerMode && objManipulator != null && beagle.transform.hasChanged && x_frames_passed > 60)
        {
            // Get the position of the beagle
            Vector3 position = beagle.transform.localPosition;
            // Get the rotation of the beagle
            Quaternion rotation = beagle.transform.localRotation;

            // Add back the height difference we subtract earlier
            Vector3 pos_to_publish = position + new Vector3(0.0f, shift_dog, 0.0f) * Drawing3dManagerGO.transform.localScale[0];

            // Convert position from Unity to ROS
            pos_to_publish = pos_to_publish.Unity2Ros();
            // Convert rotation from Unity to ROS
            rotation = rotation.Unity2Ros();

            // Create a PoseStampedMsg
            PoseStampedMsg msg = new PoseStampedMsg();
            // Set the header of the msg
            msg.header.frame_id = "map";
            // Set the pose of the msg
            msg.pose.position.x = pos_to_publish[0];
            msg.pose.position.y = pos_to_publish[1];
            msg.pose.position.z = pos_to_publish[2];
            msg.pose.orientation.x = rotation[0];
            msg.pose.orientation.y = rotation[1];
            msg.pose.orientation.z = rotation[2];
            msg.pose.orientation.w = rotation[3];

            // Publish the msg
            ros.Send(goal_pose_topic, msg);

            beagle.transform.hasChanged = false;
            x_frames_passed = 0;
        }
    }

    void OdometryCallback(OdometryMsg msg) {
        // Extract xyz pose information from msg
        Vector3 position = new Vector3((float)msg.pose.pose.position.x, (float)msg.pose.pose.position.y, (float)msg.pose.pose.position.z);
        // Extract quaternion pose information from msg
        Quaternion rotation = new Quaternion((float)msg.pose.pose.orientation.x, (float)msg.pose.pose.orientation.y, (float)msg.pose.pose.orientation.z, (float)msg.pose.pose.orientation.w);

        // Convert position from ROS to Unity
        position = position.Ros2Unity();
        // Convert rotation from ROS to Unity
        rotation = rotation.Ros2Unity();

        // Shift the position down, scaled to local scale, usually because the user starts the app with the beagle in the air
        position = position - new Vector3(0.0f, shift_dog, 0.0f) * Drawing3dManagerGO.transform.localScale[0];
        

        // Set local transform of beagle
        beagle.transform.localPosition = position;
        beagle.transform.localRotation = rotation;

        // Debug.Log("Received PoseStampedMsg from ROS topic /anymal/odom");
        // // Extract xyz pose information from msg
        // Vector3 position = new Vector3((float)msg.pose.position.x, (float)msg.pose.position.y, (float)msg.pose.position.z);
        // // Extract quaternion pose information from msg
        // Quaternion rotation = new Quaternion((float)msg.pose.orientation.x, (float)msg.pose.orientation.y, (float)msg.pose.orientation.z, (float)msg.pose.orientation.w);

        // // Set local transform of beagle
        // beagle.transform.localPosition = position;
        // beagle.transform.localRotation = rotation;
    }

    public void ControllerMode()
    {
        controllerMode = true;

        DestroyDrawing3dManagerComponents();

        // Unsubscribe from anymal/odom
        if (ros.HasSubscriber(robot_odom_topic))
        {
            ros.Unsubscribe(robot_odom_topic);
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
            // beagle.transform.position = new Vector3(beagle.transform.position.x, -shift_dog * Drawing3dManagerGO.transform.localScale[0], beagle.transform.position.z);
            beagle.transform.localPosition = new Vector3(beagle.transform.localPosition.x, -shift_dog * Drawing3dManagerGO.transform.localScale[0], beagle.transform.localPosition.z);
            // set beagle rotation as euler angles, but only around y-axis
            // beagle.transform.rotation = Quaternion.Euler(0.0f, beagle.transform.rotation.eulerAngles[1], 0.0f);
            beagle.transform.localRotation = Quaternion.Euler(0.0f, beagle.transform.localRotation.eulerAngles[1], 0.0f);
        });
    }

    public void BoundPointCloud()
    {
        // Check if subscribed to anymal/odom, otherwise subscribe to anymal/odom
        if (!ros.HasSubscriber(robot_odom_topic))
        {
            ros.Subscribe<OdometryMsg>(robot_odom_topic, OdometryCallback);
        }

        DestroyBeagleComponents();

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
        if (!ros.HasSubscriber(robot_odom_topic))
        {
            ros.Subscribe<OdometryMsg>(robot_odom_topic, OdometryCallback);
        }

        controllerMode = false;
        // Remove all the new components added from bounds
        DestroyDrawing3dManagerComponents();
        DestroyBeagleComponents();

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

    public void RescalePointSize()
    {
        meshRenderer.sharedMaterial.shader = Shader.Find("Point Cloud/singleDisk");
        meshRenderer.sharedMaterial.SetFloat("_PointSize", 0.05f * Drawing3dManagerGO.transform.localScale[0]);
    }
}
