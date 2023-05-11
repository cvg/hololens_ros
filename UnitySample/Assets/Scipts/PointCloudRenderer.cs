using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;
using RosMessageTypes.Sensor;
using RosMessageTypes.BuiltinInterfaces;
using RosMessageTypes.Std;
using RosMessageTypes.Nav;

#if ENABLE_WINMD_SUPPORT
using HL2UnityPlugin;
#endif

using Unity.Robotics.Visualizations;

// Script that renders the point cloud using VisualizationTopicsTabEntry
public class PointCloudRenderer : MonoBehaviour
{
    ROSConnection ros;
    public GameObject visTopicsTabGameObject;

    public ConfigReader configReader;

    IEnumerator Start()
    {
        yield return new WaitUntil(() => configReader.FinishedReader);   
        
        ros = ROSConnection.GetOrCreateInstance();
        VisualizationTopicsTab vistab = visTopicsTabGameObject.GetComponent<VisualizationTopicsTab>();

        string PCTopic = configReader.point_cloud_topic;
        Debug.Log("Setting up subscriber for pc " + PCTopic);

        // Add new topic for /PCtoVisualize
        RosTopicState state = ros.GetOrCreateTopic(PCTopic, "sensor_msgs/PointCloud2", false);
        vistab.OnNewTopicPublic(state);

        VisualizationTopicsTabEntry vis;
        vis = vistab.getVisTab(PCTopic);

        if (vis == null)
        {
            Debug.LogError("VisualizationTopicsTabEntry not found for " + PCTopic);
            yield break;
        }

        Debug.Log(vis.GetVisualFactory().GetType());

        PointCloud2DefaultVisualizer visFactory = (PointCloud2DefaultVisualizer)(vis.GetVisualFactory());
        ((PointCloud2DefaultVisualizer)visFactory).GetOrCreateVisual(PCTopic).SetDrawingEnabled(true);
        Debug.Log("VisualizationTopicsTab connected to" + ros.RosIPAddress + " " + ros.RosPort);       
    }
}
