﻿namespace Utj.UnityChoseKun
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;

    
    [System.Serializable]
    public class GameObjectKun {
        [SerializeField] bool m_activeSelf;
        public bool activeSelf{
            get{return m_activeSelf;}
            set{m_activeSelf = value;}
        }
        [SerializeField] bool m_isStatic;
        public bool isStatic {
            get {return m_isStatic;}
            set {m_isStatic = value;}
        }

        [SerializeField] int m_layer;
        public int layer{
            get {return m_layer;}
            set {m_layer = value;}
        }
        [SerializeField] string m_tag;
        public string tag{
            get {return m_tag;}
            set {m_tag = value;}
        }
        [SerializeField] int m_instanceID;
        public int instanceID{
            get {return m_instanceID;}
            private set {m_instanceID = value;}
        }
        [SerializeField] string m_name;
        public string name {
            get {return m_name;}
            set {m_name = value;}
        }
        
        [SerializeField] string m_transformJson;
        public string transformJson{
            get{return m_transformJson;}
            set{m_transformJson = value;}
        }
        [SerializeField] ComponentKun.ComponentKunType [] m_componentKunTypes;
        public ComponentKun.ComponentKunType [] componentKunTypes{
            get{return m_componentKunTypes;}
            set{m_componentKunTypes = value;}
        }
        [SerializeField] string[] m_componentDataJsons;
        public string[] componentDataJsons {
            get{return m_componentDataJsons;}
            set{m_componentDataJsons = value;}
        }

        private TransformKun m_transformKun;
        public TransformKun transformKun {
            get {
                if(m_transformKun == null){
                    m_transformKun = JsonUtility.FromJson<TransformKun>(transformJson);
                }
                return m_transformKun;
            }
        }
        


        public GameObjectKun():this(null){}
        public GameObjectKun(GameObject go){
            if(go == null){
                return;
            }
            activeSelf = go.activeSelf;
            isStatic = go.isStatic;
            layer = go.layer;
            tag = go.tag;
            instanceID = go.GetInstanceID();
            name = go.name;                                
            transformJson = JsonUtility.ToJson(new TransformKun(go.transform));                
            var typeList = new List<BehaviourKun.ComponentKunType>();
            var jsonList = new List<string>();
            
            var components = go.GetComponents(typeof(Component));
            foreach(var component in components){
                var componentKunType = ComponentKun.GetComponentKunType(component);
                Debug.Log("ComponentKunType" + componentKunType);
                if(componentKunType == ComponentKun.ComponentKunType.Transform){
                    continue;
                }
                var systemType = ComponentKun.typeConverterTbls[(int)componentKunType,1];   
                var componentKun = System.Activator.CreateInstance(systemType,new object[]{component});
                var json = JsonUtility.ToJson(componentKun);
                typeList.Add(componentKunType);
                jsonList.Add(json);                                
            }
            componentKunTypes = typeList.ToArray();
            componentDataJsons = jsonList.ToArray();            
        }


        public void StoreGameObject(GameObject gameObject)
        {
            Debug.Log("GameObjectKun::StoreGameObject");

            gameObject.SetActive(activeSelf);
            gameObject.isStatic = isStatic;
            gameObject.layer = layer;
            gameObject.tag = tag;
            gameObject.name = name;

            var transFormKun = JsonUtility.FromJson<TransformKun>(transformJson);
            if(transFormKun != null){
                transFormKun.WriteBack(gameObject.transform);
            }

            for(var i = 0; i < componentKunTypes.Length; i++){                        
                var componentKunType = componentKunTypes[i];
                var systemType = ComponentKun.GetComponentSystemType(componentKunType);
                var component = gameObject.GetComponent(systemType);
                if(component == null){
                    Debug.LogWarning("component == null");
                    continue;
                }
                var componentKun = JsonUtility.FromJson(componentDataJsons[i],
                                                        ComponentKun.GetComponetKunSyetemType(componentKunType)) as ComponentKun;
                componentKun.WriteBack(component);
            }                        
        }


        
    }


}