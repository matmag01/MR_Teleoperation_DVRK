#!/usr/bin/env python
import rospy
import numpy as np
import cv2
from sensor_msgs.msg import Image, CompressedImage
from cv_bridge import CvBridge
import message_filters

class StereoConcatenatorUserStudy:
    def __init__(self):
        rospy.init_node('stereo_concatenator_sync', anonymous=True)
        self.bridge = CvBridge()

        # Publisher
        self.pub = rospy.Publisher('/concatenated_image/compressed', CompressedImage, queue_size=1)
        self.pub_square_left = rospy.Publisher('/left/img_square', Image, queue_size=1)
        self.pub_square_right = rospy.Publisher('/right/img_square', Image, queue_size=1)

        # Subscribers
        left_sub = message_filters.Subscriber('/dvrk_cam/left/image_raw', Image)
        right_sub = message_filters.Subscriber('/dvrk_cam/right/image_raw', Image)
        ts = message_filters.ApproximateTimeSynchronizer([left_sub, right_sub], queue_size=10, slop=0.05)
        ts.registerCallback(self.callback)

        # Overlay user study
        overlay_path = rospy.get_param('~overlay_img', "/home/mmagnan4/Desktop/userstudy")
        self.overlay_static = cv2.imread(overlay_path, cv2.IMREAD_UNCHANGED)
        if self.overlay_static is None:
            rospy.logwarn("No img found")
            return
        
        # Parameters
        self.contrast = rospy.get_param('~contrast', 1.2)
        self.brightness = rospy.get_param('~brightness', 0)
        self.size_factor = rospy.get_param('~size_factor', 0.15) # Square dimension in %
        self.color_str = rospy.get_param('~square_color', "0,255,0") # Square color (default: green)
        self.square_color = tuple(map(int, self.color_str.split(',')))
        self.quality = 78 #JPEG img quality

        rospy.loginfo("StereoConcatenator sync node started with contrast %.2f and brightness %.2f . Square dimension %.2f and color %s", self.contrast, self.brightness, self.size_factor, str(self.square_color))
        rospy.spin()

    def add_square(self, image):
        # Draw square:
        img_copy = image.copy()
        h, w = img_copy.shape[:2]
        side = int(min(w, h)*self.size_factor)

        x1 = int(w/2 - side/2)
        y1 = int(h/2 - side/2)
        x2 = x1 + side
        y2 = y1 + side

        cv2.rectangle(img_copy, (x1, y1), (x2, y2), self.square_color, -1)
        return img_copy

    
    def apply_overlay_fixed(self, frame, overlay):
        
        if overlay is None:
            return frame

        output = frame.copy()
        h, w = output.shape[:2]
        oh, ow = overlay.shape[:2]

        # change dimension
        if ow > w or oh > h:
            scale = min(w / float(ow), h / float(oh))
            overlay_resized = cv2.resize(overlay, (int(ow * scale), int(oh * scale)))
        else:
            overlay_resized = overlay

        oh, ow = overlay_resized.shape[:2]
        x = (w - ow) // 2
        y = (h - oh) // 2

        # Alpha blending
        if overlay_resized.shape[2] == 4:  # BGRA
            alpha = overlay_resized[:, :, 3] / 255.0
            for c in range(3):
                output[y:y+oh, x:x+ow, c] = (
                    alpha * overlay_resized[:, :, c] +
                    (1 - alpha) * output[y:y+oh, x:x+ow, c]
                ).astype(np.uint8)
        else:
            output[y:y+oh, x:x+ow] = overlay_resized

        return output


    def callback(self, left_msg, right_msg):
        try:
            left_cv = self.bridge.imgmsg_to_cv2(left_msg, 'bgr8')
            right_cv = self.bridge.imgmsg_to_cv2(right_msg, 'bgr8')
        except Exception as e:
            rospy.logerr("CvBridge error: %s", e)
            return

        # Square
        try: 
            left_sq = self.add_square(left_cv)
            right_sq = self.add_square(right_cv)
            cv2.imshow("Left with square", left_sq)
            cv2.imshow("Right with square", right_sq)
            cv2.waitKey(1)
            # Publish images
            self.pub_square_left.publish(self.bridge.cv2_to_imgmsg(left_sq, "bgr8"))
            self.pub_square_right.publish(self.bridge.cv2_to_imgmsg(right_sq, "bgr8"))

            # Concatenated
            concat_sq = np.concatenate((left_sq, right_sq), axis=1)
            #concat_sq = cv2.convertScaleAbs(concat_sq, alpha=self.contrast, beta=self.brightness)

            # JPEG img quality
            encode_param = [int(cv2.IMWRITE_JPEG_QUALITY), self.quality]
            result, encimg = cv2.imencode('.jpg', concat_sq, encode_param)
            # Overlay
            concat_sq_overlay = self.apply_overlay_fixed(concat_sq, self.overlay_static)

            msg_out_sq = CompressedImage()
            msg_out_sq.header.stamp = rospy.Time.now()
            msg_out_sq.format = "jpeg"
            msg_out_sq.data =encimg.tobytes()
            self.pub.publish(msg_out_sq)

        except Exception as e:
            rospy.logerr("Errore concatenazione/compressione img con overlay: %s", e)
            return



if __name__ == '__main__':
    try:
        StereoConcatenatorUserStudy()
    except rospy.ROSInterruptException:
        pass

