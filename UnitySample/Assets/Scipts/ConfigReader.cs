#if ENABLE_WINMD_SUPPORT
using Windows.Storage;
using Windows.Storage.Streams;
#endif

using System;
using UnityEngine;
using Newtonsoft.Json;

// JULIA: Set up imports for ROS
using Unity.Robotics.ROSTCPConnector;

public class ConfigReader : MonoBehaviour
{
    public bool FinishedReader = false;

    public string ip = "192.168.1.114";
    public int port = 9090;
    public string robot_odom_topic = "/anymal/odom";
    public string point_cloud_topic = "/transformed_point_cloud_topic";
    public string goal_pose_topic = "/unity_move_base_simple/goal";

    private ROSConnection ros;

    void Start()
    {
        LoadFile();
    }

    async void LoadFile()
    {
#if ENABLE_WINMD_SUPPORT
        Debug.Log("Getting the file");
        StorageFile file = await StorageFile.GetFileFromPathAsync("U:\\Users\\chenj\\Pictures\\ip.txt"); // Need to make this path not dependent on username
        Debug.Log("Getting the Random Access Stream");
        IRandomAccessStream stream = await file.OpenAsync(0);
        string fileContent = await FileIO.ReadTextAsync(file);
        ConfigFile json  = JsonConvert.DeserializeObject<ConfigFile>(fileContent);
        ip = json.ip;
        port = json.port;
        robot_odom_topic = json.robot_odom_topic;
        point_cloud_topic = json.point_cloud_topic;
        goal_pose_topic = json.goal_pose_topic;
        Debug.Log(fileContent);
#endif

        ros = ROSConnection.GetOrCreateInstance();
        ros.Connect(ip, port); // Specify the unique ip and port once, all other GetOrCreateInstance() should point here

        FinishedReader = true;
    }
}

public class ConfigFile
{
    public string ip { get; set; }
    public int port { get; set; }
    public string robot_odom_topic { get; set; }
    public string point_cloud_topic { get; set; }
    public string goal_pose_topic { get; set; }
}
