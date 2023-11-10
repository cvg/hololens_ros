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

        // string PCTopic = configReader.point_cloud_topic;
        string MeshTopic = configReader.mesh_topic;
        // Debug.Log("Setting up subscriber for pc " + PCTopic);
        Debug.Log("Setting up subscriber for mesh " + MeshTopic);

        // Add new topic for /PCtoVisualize
        // RosTopicState state = ros.GetOrCreateTopic(PCTopic, "sensor_msgs/PointCloud2", false);
        // vistab.OnNewTopicPublic(state);

        // Add new topic for /MeshToVisualize
        RosTopicState state_mesh = ros.GetOrCreateTopic(MeshTopic, "visualization_msgs/Marker", false);
        vistab.OnNewTopicPublic(state_mesh);

        // VisualizationTopicsTabEntry vis;
        // vis = vistab.getVisTab(PCTopic);

        VisualizationTopicsTabEntry vis_mesh;
        vis_mesh = vistab.getVisTab(MeshTopic);

        // if (vis == null)
        // {
            // Debug.LogError("VisualizationTopicsTabEntry not found for " + PCTopic);
            // yield break;
        // }

        if (vis_mesh == null)
        {
            Debug.LogError("VisualizationTopicsTabEntry not found for " + MeshTopic);
            yield break;
        }

        // Debug.Log(vis.GetVisualFactory().GetType());
        Debug.Log(vis_mesh.GetVisualFactory().GetType());

        // PointCloud2DefaultVisualizer visFactory = (PointCloud2DefaultVisualizer)(vis.GetVisualFactory());
        // ((PointCloud2DefaultVisualizer)visFactory).GetOrCreateVisual(PCTopic).SetDrawingEnabled(true);
        // Debug.Log("VisualizationTopicsTab connected to" + ros.RosIPAddress + " " + ros.RosPort);       

        MarkerDefaultVisualizer visFactory_mesh = (MarkerDefaultVisualizer)(vis_mesh.GetVisualFactory());
        ((MarkerDefaultVisualizer)visFactory_mesh).GetOrCreateVisual(MeshTopic).SetDrawingEnabled(true);
        Debug.Log("VisualizationTopicsTab connected to" + ros.RosIPAddress + " " + ros.RosPort);

        // JULIA: I think I need all the code above to initialize some stuff, but also would like to change the topic name
        GameObject markerVisualizationSuite = GameObject.Find("MarkerVisualizationSuite");
        MarkerDefaultVisualizer markerDefaultVisualizer = markerVisualizationSuite.GetComponent<MarkerDefaultVisualizer>();
        markerDefaultVisualizer.Topic = MeshTopic;
    }

    public void AddSubscriberToMesh() {
        string MeshTopic = configReader.mesh_topic;

        ros = ROSConnection.GetOrCreateInstance();
        VisualizationTopicsTab vistab = visTopicsTabGameObject.GetComponent<VisualizationTopicsTab>();

        // Add new topic for /MeshToVisualize
        RosTopicState state_mesh = ros.GetOrCreateTopic(MeshTopic, "visualization_msgs/Marker", false);
        vistab.OnNewTopicPublic(state_mesh);

        VisualizationTopicsTabEntry vis_mesh;
        vis_mesh = vistab.getVisTab(MeshTopic);

        if (vis_mesh == null)
        {
            Debug.LogError("VisualizationTopicsTabEntry not found for " + MeshTopic);
            return;
        }

        MarkerDefaultVisualizer visFactory_mesh = (MarkerDefaultVisualizer)(vis_mesh.GetVisualFactory());
        ((MarkerDefaultVisualizer)visFactory_mesh).GetOrCreateVisual(MeshTopic).SetDrawingEnabled(true);
        Debug.Log("VisualizationTopicsTab connected to" + ros.RosIPAddress + " " + ros.RosPort);

        // JULIA: I think I need all the code above to initialize some stuff, but also would like to change the topic name
        GameObject markerVisualizationSuite = GameObject.Find("MarkerVisualizationSuite");
        MarkerDefaultVisualizer markerDefaultVisualizer = markerVisualizationSuite.GetComponent<MarkerDefaultVisualizer>();
        markerDefaultVisualizer.Topic = MeshTopic;
    }
    
}
