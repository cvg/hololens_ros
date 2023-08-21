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
    public string[] robot_odom_topics = {"/anymal/odom1", "/anymal/odom2"};     // List of odom topics to subscribe to
    public string point_cloud_topic = "/transformed_point_cloud_topic";
    public string[] goal_pose_topics = {"/unity_move_base_simple/goal1", "/unity_move_base_simple/goal2"};    // List of goal pose topics to publish to

    public string overlay_alignment_tr = "/overlay_alignment_tr";
    public bool use_multi_floor = false;

    private ROSConnection ros;

    void Start()
    {
        LoadFile();
    }

    async void LoadFile()
    {
#if ENABLE_WINMD_SUPPORT
        Debug.Log("Getting the file");
        StorageFile file = await StorageFile.GetFileFromPathAsync("U:\\Users\\chenj\\Pictures\\ipsecond.txt"); // Need to make this path not dependent on username
        Debug.Log("Getting the Random Access Stream");
        IRandomAccessStream stream = await file.OpenAsync(0);
        string fileContent = await FileIO.ReadTextAsync(file);
        ConfigFile json  = JsonConvert.DeserializeObject<ConfigFile>(fileContent);
        ip = json.ip;
        port = json.port;
        robot_odom_topics = json.robot_odom_topics;
        point_cloud_topic = json.point_cloud_topic;
        goal_pose_topics = json.goal_pose_topics;
        overlay_alignment_tr = json.overlay_alignment_tr;
        use_multi_floor = json.use_multi_floor;

        Debug.Log(fileContent);
#endif

        ros = ROSConnection.GetOrCreateInstance();
        ros.Connect(ip, port); // Specify the unique ip and port once, all other GetOrCreateInstance() should point here

        Debug.Log("ip: " + ip);
        Debug.Log("port: " + port);

        FinishedReader = true;
    }
}

public class ConfigFile
{
    public string ip { get; set; }
    public int port { get; set; }

    // List of odom topics to subscribe to
    public string[] robot_odom_topics { get; set; }
    // public string robot_odom_topic_beagle { get; set; }
    // public string robot_odom_topic_poodle { get; set; }

    public string point_cloud_topic { get; set; }

    // List of goal pose topics to publish to
    public string[] goal_pose_topics { get; set; }
    // public string goal_pose_topic_beagle { get; set; }
    // public string goal_pose_topic_poodle { get; set; }
    
    public string overlay_alignment_tr { get; set; }
    public bool use_multi_floor { get; set; }
}
