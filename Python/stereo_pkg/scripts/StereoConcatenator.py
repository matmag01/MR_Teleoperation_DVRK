#!/usr/bin/env python
import rospy
import numpy as np
import cv2
from sensor_msgs.msg import Image, CompressedImage
from cv_bridge import CvBridge
import message_filters

class StereoConcatenator:
    def __init__(self):
        rospy.init_node('stereo_concatenator_sync', anonymous=True)
        self.bridge = CvBridge()

        # Publisher
        self.pub = rospy.Publisher('/concatenated_image/compressed', CompressedImage, queue_size=1)

        # Subscribers
        left_sub = message_filters.Subscriber('/dvrk_cam/left/image_raw', Image)
        right_sub = message_filters.Subscriber('/dvrk_cam/right/image_raw', Image)
        ts = message_filters.ApproximateTimeSynchronizer([left_sub, right_sub], queue_size=10, slop=0.05)
        ts.registerCallback(self.callback)

        
        # Parameters
        self.contrast = rospy.get_param('~contrast', 1.2)
        self.brightness = rospy.get_param('~brightness', 0)
        self.quality = 78

        rospy.loginfo("StereoConcatenator sync node started with contrast %.2f and brightness %.2f .", self.contrast, self.brightness)
        rospy.spin()


    def callback(self, left_msg, right_msg):
        try:
            left_header = left_msg.header.stamp
            left_cv = self.bridge.imgmsg_to_cv2(left_msg, 'bgr8')
            right_cv = self.bridge.imgmsg_to_cv2(right_msg, 'bgr8')
        except Exception as e:
            rospy.logerr("CvBridge error: %s", e)
            return

        try:
            concat = np.concatenate((left_cv, right_cv), axis=1)

            # JPEG
            encode_param = [int(cv2.IMWRITE_JPEG_QUALITY), self.quality]
            result, encimg = cv2.imencode('.jpg', concat, encode_param)

            msg_out = CompressedImage()
            msg_out.header.stamp = left_header
            msg_out.format = "jpeg"
            msg_out.data = encimg.tobytes()
            self.pub.publish(msg_out)
        except Exception as e:
            rospy.logerr("Concatenation or compression error: %s", e)


if __name__ == '__main__':
    try:
        StereoConcatenator()
    except rospy.ROSInterruptException:
        pass

