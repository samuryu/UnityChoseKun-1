﻿using System.IO;
using UnityEngine;


namespace Utj.UnityChoseKun
{
    /// <summary>
    /// HierarchyPlayerで使用するMessageデータ
    /// </summary>
    [System.Serializable]
    public class HierarchyMessage : ISerializerKun
    {
        public enum MessageID
        {
            Duplicate,
            Delete,
            CreateEmpty,
            CreatePrimitive,
            CreateClass,
        }

        [SerializeField] public MessageID messageID;
        [SerializeField] public int baseID;
        [SerializeField] public PrimitiveType primitiveType;        
        [SerializeField] public string systemType;

        public System.Type type;


        /// <summary>
        /// Serialize
        /// </summary>
        /// <param name="binaryWriter">BinaryWriter</param>
        public virtual void Serialize(BinaryWriter binaryWriter)
        {
            binaryWriter.Write((int)messageID);
            binaryWriter.Write(baseID);
            binaryWriter.Write((int)primitiveType);
            if (type != null)
            {
                systemType = type.ToString();
            } 
            else
            {
                systemType = "";
            }
            binaryWriter.Write(systemType);
            //SerializerKun.Serialize(binaryWriter, systemType);
        }


        /// <summary>
        /// Deserialize
        /// </summary>
        /// <param name="binaryReader">BinaryReader</param>
        public virtual void Deserialize(BinaryReader binaryReader)
        {
            messageID = (MessageID)binaryReader.ReadInt32();
            baseID = binaryReader.ReadInt32();
            primitiveType = (PrimitiveType)binaryReader.ReadInt32();
            
            //systemType = SerializerKun.DesirializeString(binaryReader);
            systemType = binaryReader.ReadString();

            if (!string.IsNullOrEmpty(systemType))
            {
                type = System.Type.GetType(systemType);
            } 
            
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            var other = obj as HierarchyMessage;
            if(other == null)
            {
                return false;
            }
            if (!messageID.Equals(other.messageID))
            {
                return false;
            }
            if (!baseID.Equals(other.baseID))
            {
                return false;
            }
            if (!primitiveType.Equals(other.primitiveType))
            {
                return false;
            }

            if (!type.Equals(other.type))
            {
                return false;
            }
            return true;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }


    /// <summary>
    /// HierarchyPlayer Class
    /// </summary>
    public class HierarchyPlayer : BasePlayer
    {
        public void OnMessageEventPush(BinaryReader binaryReader)
        {
            HierarchyMessage message = new HierarchyMessage();
            message.Deserialize(binaryReader);
            
            switch (message.messageID)
            {
                case HierarchyMessage.MessageID.Duplicate:
                    {
                        var go = FindGameObjectInScene(message.baseID);
                        if(go != null)
                        {
                            var clone = GameObject.Instantiate(go);
                            clone.transform.parent = go.transform.parent;
                            clone.transform.localPosition = go.transform.localPosition;
                            clone.transform.localRotation = go.transform.localRotation;
                            clone.transform.localScale = go.transform.localScale;
                        }
                    }
                    break;

                case HierarchyMessage.MessageID.Delete:
                    {
                        var go = FindGameObjectInScene(message.baseID);
                        if (go != null)
                        {
                            GameObject.DestroyImmediate(go);
                        }
                    }
                    break;

                case HierarchyMessage.MessageID.CreateEmpty:
                    {
                        var parent = FindGameObjectInScene(message.baseID);
                        var go = new GameObject();
                        if (go != null)
                        {
                            if (parent != null)
                            {
                                go.transform.parent = parent.transform;
                            }
                            else
                            {
                                go.transform.parent = null;
                            }

                            go.transform.localPosition = Vector3.zero;
                            go.transform.localRotation = Quaternion.identity;
                            go.transform.localScale = Vector3.one;
                        }
                    }
                    break;

                case HierarchyMessage.MessageID.CreateClass:
                    {                        
                        var parent = FindGameObjectInScene(message.baseID);
                        var go = new GameObject(message.type.Name,message.type);
                        if (go != null)
                        {
                            if (parent != null)
                            {
                                go.transform.parent = parent.transform;
                            }
                            else
                            {
                                go.transform.parent = null;
                            }

                            go.transform.localPosition = Vector3.zero;
                            go.transform.localRotation = Quaternion.identity;
                            go.transform.localScale = Vector3.one;
                        }
                    }            
                    break;

                case HierarchyMessage.MessageID.CreatePrimitive:
                    {
                        var parent = FindGameObjectInScene(message.baseID);
                        var go = GameObject.CreatePrimitive(message.primitiveType);
                        if (go != null)
                        {
                            if (parent != null)
                            {
                                go.transform.parent = parent.transform;
                            }
                            else
                            {
                                go.transform.parent = null;
                            }

                            go.transform.localPosition = Vector3.zero;
                            go.transform.localRotation = Quaternion.identity;
                            go.transform.localScale = Vector3.one;
                        }
                    }
                    break;
            }

            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var sceneKun = new SceneKun(scene);
            UnityChoseKunPlayer.SendMessage<SceneKun>(UnityChoseKun.MessageID.GameObjectPull, sceneKun);
        }


        


        /// <summary>
        /// instanceIDをキーにしてScene内のGameObjectを検索する
        /// </summary>
        /// <param name="instanceID">instanceID</param>
        /// <returns>instanceIDが一致するGameObject</returns>
        public static GameObject FindGameObjectInScene(int instanceID)
        {
            foreach (var obj in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                var go = FindGameObjectInChildren(obj, instanceID);
                if(go != null)
                {
                    return go;
                }
            }
            return null;
        }


        /// <summary>
        /// instanceIDをキーにしてGameObjectを検索する
        /// </summary>
        /// <param name="gameObject">検索の起点となるGameObject</param>
        /// <param name="instanceID">検索するinstanceID</param>
        /// <returns>instanceIDと一致するGameObject</returns>
        public static GameObject FindGameObjectInChildren(GameObject gameObject, int instanceID)
        {
            if (gameObject == null)
            {
                return null;
            }
            else if (gameObject.GetInstanceID() == instanceID)
            {
                return gameObject;
            }
            for (var i = 0; i < gameObject.transform.childCount; i++)
            {
                var result = FindGameObjectInChildren(gameObject.transform.GetChild(i).gameObject, instanceID);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }
    }
}