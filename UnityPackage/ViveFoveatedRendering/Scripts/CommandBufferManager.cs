//========= Copyright 2019, HTC Corporation. All rights reserved. ===========

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HTC.UnityPlugin.FoveatedRendering
{
    public class CommandBufferManager
    {
        public delegate void CommandBufferEvent(CommandBuffer cmd);

        private class CommandBufferObject
        {
            string name;
            CameraEvent cameraEvent;
            CommandBuffer buffer;

            public CommandBufferObject(string bufName, CameraEvent camEvnt, params CommandBufferEvent[] commands)
            {
                name = bufName;
                cameraEvent = camEvnt;

                buffer = new CommandBuffer();
                buffer.name = name;
                
                foreach (var cmd in commands)
                {
                    cmd(buffer);
                }
            }

            public void Enable(Camera cam)
            {
                cam.AddCommandBuffer(cameraEvent, buffer);
            }

            public void Disable(Camera cam)
            {
                cam.RemoveCommandBuffer(cameraEvent, buffer);
            }
        }

        public void AppendCommands(string bufName, CameraEvent camEvnt, params CommandBufferEvent[] commands)
        {
            var buf = new CommandBufferObject(bufName, camEvnt, commands);
            commandBufList.Add(buf);
        }

        public void ClearCommands()
        {
            commandBufList.Clear();
        }

        public void EnableCommands(Camera cam)
        {
            foreach (var buf in commandBufList)
            {
                buf.Enable(cam);
            }
        }

        public void DisableCommands(Camera cam)
        {
            foreach (var buf in commandBufList)
            {
                buf.Disable(cam);
            }
        }

        private List<CommandBufferObject> commandBufList = new List<CommandBufferObject>();
    }
}
