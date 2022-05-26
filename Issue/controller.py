# utilities for Maya

import socket

# socket client controller
class MayaController:
    """
    This is controller to `remotely` control Maya to make character animations. 
    The default local host is *127.0.0.1*

    :param PORT: port to connect to the local socket server, defaults to 0
    :type PORT: int 

    :ivar client: socket client
    """
    def __init__(self, PORT = 12345):
        """Constructor method
        """
        # connect to Maya server
        HOST = '127.0.0.1'  # Symbolic name meaning the local host
        #PORT = PORT  # Arbitrary non-privileged port

        ADDR = (HOST, PORT)

        self.client = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self.client.connect(ADDR)

    def SendCommand(self, command: str):
        """Send a string command to the socket server (Maya side)

        :param command: a string command
        :type command: str
        """
        # client = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        # client.connect(ADDR)
        command = command.encode()  # the command from external editor to maya

        my_message = command
        self.client.send(my_message)
        data = self.client.recv(16384)  # receive the result info
        # client.close()
        # print(data)
        # ret = str(data.decode(encoding="ASCII"))
        ret = data.decode("utf-8")
        return ret

    def SendPythonCommand(self, command: str):
        """Send a string command to the socket server (Maya side)

        :param command: a string command
        :type command: str
        """
        # client = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        # client.connect(ADDR)
        command = 'python("{}")'.format(command)
        command = command.encode()  # the command from external editor to maya

        my_message = command
        self.client.send(my_message)
        data = self.client.recv(16384)  # receive the result info
        # client.close()
        # print(data)
        # ret = str(data.decode(encoding="ASCII"))
        ret = data.decode("utf-8")
        return ret

    def Close(self):
        """Close socket client
        """
        self.client.close()

    def __del__(self):
        self.Close()

    #--------------------------------SET-----------------------------------------
    def SetNewScene(self):
        """Set new Maya empty scene
        """
        send_message = "file -f -new;"
        recv_message = self.SendCommand(send_message)

    def SetCurrentTimeFrame(self, time_frame: int):
        """Set timeline in the Maya scene

        :param time_frame: key frame
        :type time_frame: int
        """
        send_message = "currentTime -edit" + " " + str(time_frame) + ";"
        recv_message = self.SendCommand(send_message)

    def SetObjectWorldTransform(self, object_name: str, location: list):
        """Set world absolute location for object with location [x, y, z]

        :param object_name: the name of the object in hierarchy view
        :type object_name: str
        :param location: location in list [x, y, z]
        :type location: list
        """
        send_message = "select -replace " + object_name + ";"
        send_message += "move -absolute " + str(location[0]) + " " + str(location[1]) + " " + str(location[2]) + ";"
        recv_message = self.SendCommand(send_message)

    def MoveObjectWorldRelative(self, object_name: str, location: list):
        """Move an object relatively with location [x, y, z]

        :param object_name: the name of the object in hierarchy view
        :type object_name: str
        :param location: location in list [x, y, z]
        :type location: list
        """
        send_message = "select -replace " + object_name + ";"
        send_message += "move -relative " + str(location[0]) + " " + str(location[1]) + " " + str(location[2]) + ";"
        recv_message = self.SendCommand(send_message)

    def SetObjectLocalTransform(self, object_name: str, location: list):
        """Set world absolute location for object with location [x,y] or [x, y, z]

        :param object_name: the name of the object in hierarchy view
        :type object_name: str
        :param location: location in list [x, y, z]
        :type location: list
        """
        send_message = "select -replace " + object_name + ";" + "move -relative "
        for value in location:
            send_message += str(value) + " "

        send_message += ";"
        recv_message = self.SendCommand(send_message)

    def SetObjectLocalRotation(self, object_name: str, rotation: list):
        """Set world absolute location for object with rotation [x,y] or [x, y, z] in degree

        :param object_name: the name of the object in hierarchy view
        :type object_name: str
        :param location: location in list [x, y, z]
        :type location: list
        """
        send_message = "select -replace " + object_name + ";" + "rotate -relative "
        for value in rotation:
            send_message += str(value) + "deg "

        send_message += ";"
        recv_message = self.SendCommand(send_message)

    def SetCurrentKeyFrameForAttribute(self, object_name: str, attr_name: str):
        """Set keyframe for object attribute

        :param object_name: name of the object in hierarchy view
        :type object_name: str
        :param attr_name: name of attribute
        :type attr_name: list
        """
        send_message = "select -r " + object_name + ";"
        send_message += "setKeyframe -at " + attr_name + ";"
        recv_message = self.SendCommand(send_message)

    def SetCurrentKeyFrameForPositionAndRotation(self, object_name: str):
        """Set keyframe for object position and rotation

        :param object_name: name of the object in hierarchy view
        :type object_name: str
        """
        send_message = "select -r " + object_name + ";"
        send_message += "setKeyframe -at translate;"
        send_message += "setKeyframe -at rotate;"
        recv_message = self.SendCommand(send_message)

    def SetCurrentKeyFrameForObjects(self, object_list):
        """Set keyframe for a list of objects

        :param object_list: a list of object names
        :type object_list: list
        """
        send_message = "setKeyframe {"
        for obj in object_list:
            send_message += "\"" + str(obj) + "\", "
        send_message = send_message[:-2] + "};"
        recv_message = self.SendCommand(send_message)

    def SetObjectAttribute(self, object_name: str, attr_name: str, value: float):
        """Set object attriture with value

        :param object_name: name of the object
        :type object_name: str
        :param attr_name: attribute of the object
        :type attr_name: str
        :param value: attribute value
        :type attr_name: float
        """

        send_message = "setAttr " + object_name + "." + attr_name + " " + str(value) + ";"
        recv_message = self.SendCommand(send_message)

    # def SetMultipleAttributes(self, attributes: dict):
    #     '''
    #     Set object attriture with value

    #     :param attributes: dictionary of facial attributes to be set
    #     :type attributes: dict
    #     '''
    #     for joint, attr_dict in attributes.items():
    #         for name, value in attr_dict.items():
    #             self.SetObjectAttribute(joint, name, value)

    def Undo(self):
        """Send undo command to socket server (Maya)
        """
        send_message = "undo;"
        rec_message = self.SendCommand(send_message)
        # print(rec_message)
        return rec_message

    def UndoToBeginning(self, max_step=200):
        '''
        Undo Maya file to beginning

        :param max_step: max undo steps, default 200
        :type max_step: int, optional
        '''
        for _ in range(max_step):
            rec_message = self.Undo()
            if "There are no more commands to undo." in rec_message:
                print("(UndoToBeginning)Undo steps:", _)
                return

    def ScreenShot(self, save_file: str, camera="persp", width=1024, height=1024):
        '''
        Take maya screen shot and save to picture

        :param save_file: save file name
        :type save_file: str
        :param camera: camera name in the Maya scene, default "persp"
        :type camera: str, optional
        :param width: screenshot width, default 1024
        :type width: int, optional
        :param height: screenshot height, default 1024
        :type height: int, optional
        
        '''
        send_message = "string $editor = `renderWindowEditor -q -editorName`;\n"
        #send_message += "string $myCamera = " + camera + ";\n"
        send_message += "string $myFilename =\"" + save_file + "\";\n"
        send_message += "render -x " + str(width) + " -y " + str(height) + " " + camera + ";\n"
        send_message += "renderWindowEditor -e -wi $myFilename $editor;"

        recv_message = self.SendCommand(send_message)
        print("(ScreenShot)", recv_message)

    #---------------------------GET-----------------------------------
    def GetAllObjects(self):
        '''
        Get all the objects from Maya scene
        
        :return recv_message: a list of object names
        :rtype: list
        '''
        send_message = "ls;"
        recv_message = self.SendCommand(send_message)
        return recv_message.rstrip('\x00').rstrip('\n').split('\t')

    def GetTimeSliderRange(self):
        '''
        Get the range of time slider
        
        :return: [min, max] representing the minimum and maximum values of the timeline
        :rtype: list
        '''
        send_message = "playbackOptions -q -minTime"
        recv_message_1 = self.SendCommand(send_message)
        recv_message_1 = recv_message_1.rstrip('\x00').rstrip('\n').split('\t')

        send_message = "playbackOptions -q -maxTime"
        recv_message_2 = self.SendCommand(send_message)
        recv_message_2 = recv_message_2.rstrip('\x00').rstrip('\n').split('\t')

        return [int(float(recv_message_1[0])), int(float(recv_message_2[0]))]

    def GetObjectWorldTransform(self, object_name: str):
        '''
        Get object world location

        :param object_name: name of the object
        :type object_name: str

        :return: cordindate [x,y,z] of the object
        :rtype: list
        '''
        send_message = "xform -q -t -ws " + object_name + ";"
        recv_message = self.SendCommand(send_message)
        recv_message = recv_message.rstrip('\x00').rstrip('\n').split('\t')
        return [np.around(float(_), decimals=2) for _ in recv_message]

    def GetObjectLocalRoation(self, object_name: str):
        '''
        Get object local rotation

        :param object_name: name of the object
        :type object_name: str

        :return: cordindate [x,y,z] of the object
        :rtype: list
        '''
        send_message = "xform -q -ro -os " + object_name + ";"
        recv_message = self.SendCommand(send_message)
        recv_message = recv_message.rstrip('\x00').rstrip('\n').split('\t')
        return [np.around(float(_), decimals=2) for _ in recv_message]

    def GetObjectAttribute(self, object_name: str, attr_name: str):
        '''
        Get attribute value of an object

        :param object_name: name of the object
        :type object_name: str

        :param attr_name: name of the attribute
        :type attr_name: str

        :return: attribute value
        :rtype: float
        '''
        send_message = "getAttr " + object_name + "." + attr_name + ";"
        recv_message = self.SendCommand(send_message)
        recv_message = recv_message.rstrip('\x00').rstrip('\n').split('\t')
        return [np.around(float(_), decimals=2) for _ in recv_message]

    def GetBodyInformation(self, joint_list: list):
        '''
        Get a list of information of the joint list

        :param joint_list: a list of joint names
        :type joint_list: list

        :return: a list with joint positions
        :rtype: list
        '''
        BODY_INFO_DIC = {}
        for joint_name in joint_list:
            info_dict = {}
            world_transform = self.GetObjectWorldTransform(joint_name)
            info_dict["world_transform"] = world_transform

            local_transform = self.GetObjectAttribute(joint_name, "translate")
            info_dict["local_transform"] = local_transform

            local_rotation = self.GetObjectAttribute(joint_name, "rotate")
            info_dict["local_rotation"] = local_rotation

            BODY_INFO_DIC[joint_name] = info_dict

        return BODY_INFO_DIC

    #--------------------------UTIL-------------------------------------
    # def GetAdvancedSkeletonJointNameFromIndex(self, joint_index: int):
    #     '''
        
    #     :param joint_index:
    #     :return: advanced skeleton joint name
    #     '''
    #     return G_Joint_Maya_Advanced_Skeleton[joint_index]

    # def GenerateSceneFromPose(self, loading_path: str, saving_path: str, if_save = False):
    #     '''
    #     Load a pose from loading_path into the scene and save it to saving_path
    #     '''
    #     with open(loading_path) as f:
    #         data = json.load(f)
    #     #print(json.dumps(data, indent = 4, sort_keys=True))
    #     for joints, attrs in data["objects"].items():
    #         for attr_name, value in attrs["attrs"].items():
    #             if isinstance(value["value"], float):
    #                 self.SetObjectAttribute(joints, attr_name, value["value"])     
    #     if if_save:
    #       self.saveFile(saving_path)
        
    # def SaveFile(self, saving_path: str):
    #     '''
    #     Save file to the specified directory
    #     '''
    #     def save(saving_path):
    #         send_message = "file -rename \"%s\"; file -save -type \"mayaBinary\"" % saving_path
    #         rec_message = self.SendCommand(send_message)
    #         return rec_message
        
    #     save_thread = threading.Thread(target=save, args=(saving_path,))
    #     save_thread.start()
    #     time.sleep(1)
    #     press("enter")
    #     time.sleep(1)
       
    # def GenerateSceneFromPoseWithIdentifiers(self, loading_path: str, identifiers: list):
    #     '''
    #     Load a pose from loading_path into the scene and save it to saving_path
    #     params: loading_path: json path
    #     identifiers list(str): joints names
    #     '''
    #     with open(loading_path) as f:
    #         data = json.load(f)
    #     # print(json.dumps(data, indent = 4, sort_keys=True))
    #     for joints, attrs in data["objects"].items():
    #         if joints in identifiers:
    #             for attr_name, value in attrs["attrs"].items():
    #                 if isinstance(value["value"], float):
    #                     self.SetObjectAttribute(joints, attr_name, value["value"])