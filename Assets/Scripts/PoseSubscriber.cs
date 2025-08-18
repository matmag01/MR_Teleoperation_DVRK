using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using RosMessageTypes.Geometry;
using UnityEngine;

public class PoseSubscriber : MonoBehaviour
{
    public string topicName = "/test_pose";

    void Start()
    {
        ROSConnection.GetOrCreateInstance().Subscribe<PoseMsg>(topicName, ReceivePose);
    }

    void ReceivePose(PoseMsg pose)
    {
        Vector3 position = pose.position.From<FLU>();
        Quaternion rotation = pose.orientation.From<FLU>();

        Debug.Log($"[ROS] Pos: {position}, Rot: {rotation}");
        transform.SetPositionAndRotation(position, rotation);
    }
}