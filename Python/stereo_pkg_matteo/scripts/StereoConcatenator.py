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
        # self.pub_square_left = rospy.Publisher('/left/img_square', Image, queue_size=1)
        # self.pub_square_right = rospy.Publisher('/right/img_square', Image, queue_size=1)
        # self.pub_square = rospy.Publisher('/concatenated_image/square_compressed', CompressedImage, queue_size=1)

        # Subscribers
        left_sub = message_filters.Subscriber('/dvrk_cam/left/image_raw', Image)
        right_sub = message_filters.Subscriber('/dvrk_cam/right/image_raw', Image)
        ts = message_filters.ApproximateTimeSynchronizer([left_sub, right_sub], queue_size=10, slop=0.05)
        ts.registerCallback(self.callback)
        # Overlay user study
        # overlay_path = rospy.get_param('~overlay_img', "/home/mmagnan4/Desktop/userstudy")
        # self.overlay_static = cv2.imread(overlay_path, cv2.IMREAD_UNCHANGED)
        # if self.overlay_static is None:
        #     rospy.logwarn("No img found")

        
        # Parameters
        self.contrast = rospy.get_param('~contrast', 1.2)
        self.brightness = rospy.get_param('~brightness', 0)
        # self.size_factor = rospy.get_param('~size_factor', 0.15) # Square dimension in %
        # self.color_str = rospy.get_param('~square_color', "0,255,0") # Square color (default: green)
        # self.square_color = tuple(map(int, self.color_str.split(',')))
        self.quality = 78

        rospy.loginfo("StereoConcatenator sync node started with contrast %.2f and brightness %.2f .", self.contrast, self.brightness)
        rospy.spin()

    # def add_square(self, image):
    #     # Draw square:
    #     img_copy = image.copy()
    #     h, w = img_copy.shape[:2]
    #     side = int(min(w, h)*self.size_factor)

    #     x1 = int(w/2 - side/2)
    #     y1 = int(h/2 - side/2)
    #     x2 = x1 + side
    #     y2 = y1 + side

    #     cv2.rectangle(img_copy, (x1, y1), (x2, y2), self.square_color, -1)
    #     return img_copy

    
    # def apply_overlay(self, frame, overlay):
    #     if overlay is None:
    #         return frame

    #     # Change dimension to fit
    #     overlay_resized = cv2.resize(overlay, (frame.shape[1], frame.shape[0]))

    #     # Channel
    #     b, g, r, a = cv2.split(overlay_resized)
    #     alpha = a.astype(float) / 255.0
    #     alpha_inv = 1.0 - alpha

    #     # Output
    #     for c, overlay_channel in enumerate([b, g, r]):
    #         frame[:, :, c] = (alpha * overlay_channel + alpha_inv * frame[:, :, c]).astype(np.uint8)
    #     return frame

    def callback(self, left_msg, right_msg):
        try:
            left_header = left_msg.header.stamp
            left_cv = self.bridge.imgmsg_to_cv2(left_msg, 'bgr8')
            right_cv = self.bridge.imgmsg_to_cv2(right_msg, 'bgr8')
        except Exception as e:
            rospy.logerr("CvBridge error: %s", e)
            return

        # try:
        #     concat = np.concatenate((left_cv, right_cv), axis=1)
        #     #concat = cv2.convertScaleAbs(concat, alpha = self.contrast, beta = self.brightness)
        #     msg_out = CompressedImage()
        #     msg_out.header.stamp = rospy.Time.now()
        #     msg_out.format = "jpeg"
        #     msg_out.data = np.array(cv2.imencode('.jpg', concat)[1]).tobytes()
        #     self.pub.publish(msg_out)
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

        # # Square
        # try: 
        #     left_sq = self.add_square(left_cv)
        #     right_sq = self.add_square(right_cv)
            
        #     # Apply overlay
        #     left_sq = self.apply_overlay(left_sq, self.overlay_static)
        #     right_sq = self.apply_overlay(right_sq, self.overlay_static)
        #     self.pub_square_left.publish(self.bridge.cv2_to_imgmsg(left_sq, "bgr8"))
        #     self.pub_square_right.publish(self.bridge.cv2_to_imgmsg(right_sq, "bgr8"))
        #     concat_sq = np.concatenate((left_sq, right_sq), axis=1)
        #     concat_sq = cv2.convertScaleAbs(concat_sq, alpha = self.contrast, beta = self.brightness)
        #     msg_out_sq = CompressedImage()
        #     msg_out_sq.header.stamp = rospy.Time.now()
        #     msg_out_sq.format = "jpeg"
        #     msg_out_sq.data = np.array(cv2.imencode('.jpg', concat_sq)[1]).tobytes()
        #     self.pub_square.publish(msg_out_sq)
        # except Exception as e:
        #     rospy.logerr("Concatenation or compression error in img with square: %s", e)


if __name__ == '__main__':
    try:
        StereoConcatenator()
    except rospy.ROSInterruptException:
        pass

