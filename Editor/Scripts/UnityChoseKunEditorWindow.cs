﻿namespace Utj.UnityChoseKun
{
    using System.IO;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;
    using UnityEditor;
    using UnityEngine.Profiling;
#if UNITY_2018_1_OR_NEWER
    using UnityEngine.Networking.PlayerConnection;

#if UNITY_2020_1_OR_NEWER
    using ConnectionUtility = UnityEditor.Networking.PlayerConnection.PlayerConnectionGUIUtility;
    using ConnectionGUILayout = UnityEditor.Networking.PlayerConnection.PlayerConnectionGUILayout;
#else
    using ConnectionUtility = UnityEditor.Experimental.Networking.PlayerConnection.EditorGUIUtility;
    using ConnectionGUILayout = UnityEditor.Experimental.Networking.PlayerConnection.EditorGUILayout;
    using UnityEngine.Experimental.Networking.PlayerConnection;
#endif

    using UnityEditor.Networking.PlayerConnection;
    using System;
    using System.Text;
    using System.Reflection;
    using System.Runtime.InteropServices;
#endif

   

    /// <summary>
    /// UnityChoseKunのEditorWindow
    /// </summary>
    public class UnityChoseKunEditorWindow : EditorWindow
    {
        private static class Styles
        {                    
            public static readonly GUIContent TitleContent = new GUIContent("Player Inspector", (Texture2D)EditorGUIUtility.Load("d_UnityEditor.InspectorWindow"));
        }

        delegate void Task();
        delegate void OnMessageFunc(BinaryReader binaryReader);
        
                
        [SerializeField] int                toolbarIdx = 0;
        [SerializeField] ScreenView         m_screenView;
        [SerializeField] TimeView           m_timeView;
        [SerializeField] InspectorView      m_inspectorView;
        [SerializeField] ShadersView        m_shadersView;
        [SerializeField] TexturesView       m_texturesView;
        [SerializeField] ApplicationView    m_applicationView;
        [SerializeField] AndroidView        m_androidView;        
        [SerializeField] Vector2            scrollPos;
        [SerializeField] ObjectCounterView  m_objectCounterView;
        [SerializeField] QualitySettingsView m_qualitySettingsView;
        [SerializeField] OnDemandRenderingView m_onDemandRenderingView;

        IConnectionState                                    m_attachProfilerState;
        Dictionary<UnityChoseKun.MessageID, OnMessageFunc>  onMessageFuncDict;
        Dictionary<string, Action>                          onGUILayoutFuncDict;


        /// <summary>
        /// ScreenView
        /// </summary>
        ScreenView screenView
        {
            get {
                if(m_screenView == null ){
                    m_screenView = new ScreenView();
                }
                return m_screenView;
            }
            set {
                m_screenView = value;
            }
        }        
        

        /// <summary>
        /// TimeView
        /// </summary>
        TimeView timeView{
            get {
                if(m_timeView == null){
                    m_timeView = new TimeView();
                }
                return m_timeView;
            }
            set {
                m_timeView = value;
            }
        }        


        /// <summary>
        /// InspectorView
        /// </summary>
        InspectorView inspectorView {
            get {if(m_inspectorView == null){m_inspectorView = new InspectorView();}return m_inspectorView;}
        }
        

        /// <summary>
        /// ShadersView
        /// </summary>
        ShadersView shaderView {
            get{if(m_shadersView == null){m_shadersView = new ShadersView();}return m_shadersView;}
        }
        

        /// <summary>
        /// TexturesView
        /// </summary>
        TexturesView texturesView{
            get{if(m_texturesView == null){m_texturesView = new TexturesView();}return m_texturesView;}            
        }
        

        /// <summary>
        /// ApplicationView
        /// </summary>
        public ApplicationView applicationView
        {
            get {
                if (m_applicationView == null) {
                    m_applicationView = new ApplicationView();
                }
                return m_applicationView;
            }
        }

        /// <summary>
        /// AndroidView
        /// </summary>
        public AndroidView androidView
        {
            get { if (m_androidView == null) { m_androidView = new AndroidView(); } return m_androidView; }            
        }

        public ObjectCounterView objectCounterView
        {
            get
            {
                if(m_objectCounterView == null)
                {
                    m_objectCounterView = new ObjectCounterView();
                }
                return m_objectCounterView;
            }
        }
        
        public QualitySettingsView qualitySettingsView
        {
            get
            {
                if(m_qualitySettingsView == null)
                {
                    m_qualitySettingsView = new QualitySettingsView();
                }
                return m_qualitySettingsView;
            }
        }


        public OnDemandRenderingView onDemandRenderingView
        {
            get
            {
                if(m_onDemandRenderingView == null)
                {
                    m_onDemandRenderingView = new OnDemandRenderingView();
                }
                return m_onDemandRenderingView;
            }
        }


        [MenuItem("Window/UnityChoseKun/Player Inspector")]
        static void Inite()
        {            
            var window = (UnityChoseKunEditorWindow)EditorWindow.GetWindow(typeof(UnityChoseKunEditorWindow));            
            window.titleContent = Styles.TitleContent;
            window.wantsMouseMove = true;
            window.autoRepaintOnSceneChange = true;
            window.Show();
        }


        /// <summary>
        /// Platformを取得する
        /// </summary>
        /// <returns></returns>
        public RuntimePlatform GetRuntimePlatform()
        {
            if (applicationView.applicationKun != null)
            {
                return applicationView.applicationKun.platform;
            }
            return RuntimePlatform.WindowsEditor;
        }

        
        /// <summary>
        /// 描画処理
        /// </summary>
        private void OnGUI()
        {                        
            GUILayoutConnect();            
            EditorGUILayout.Space();
            
            //if (onGUILayoutFuncDict != null)
            {
                var texts = onGUILayoutFuncDict.Keys.ToArray();                
                toolbarIdx = GUILayout.Toolbar(
                    toolbarIdx,
                    texts,
                    EditorStyles.toolbarButton);
                EditorGUILayout.Space();

                var key = texts[toolbarIdx];
                
                Action onGUI;
                onGUILayoutFuncDict.TryGetValue(key,out onGUI);
                if (onGUI != null)
                {
                    scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
                    onGUI();
                    EditorGUILayout.EndScrollView();
                }
            }            
        }


        /// <summary>
        /// 接続先選択用GUI
        /// </summary>
        private void GUILayoutConnect()
        {
            EditorGUILayout.BeginHorizontal();

            var contents = new GUIContent("Connect To");
            var v2 = EditorStyles.label.CalcSize(contents);
            EditorGUILayout.LabelField(contents, GUILayout.Width(v2.x));
            if (m_attachProfilerState != null)
            {
#if UNITY_2020_1_OR_NEWER
                ConnectionGUILayout.ConnectionTargetSelectionDropdown(m_attachProfilerState, EditorStyles.toolbarDropDown);
#else
                ConnectionGUILayout.AttachToPlayerDropdown(m_attachProfilerState, EditorStyles.toolbarDropDown);
#endif
                switch (m_attachProfilerState.connectedToTarget)
                {
                    case ConnectionTarget.None:
                        //This case can never happen within the Editor, since the Editor will always fall back onto a connection to itself.
                        break;
                    case ConnectionTarget.Player:
                        Profiler.enabled = GUILayout.Toggle(Profiler.enabled, string.Format("Profile the attached Player ({0})", m_attachProfilerState.connectionName), EditorStyles.toolbarButton);
                        break;
                    case ConnectionTarget.Editor:
                        // The name of the Editor or the PlayMode Player would be "Editor" so adding the connectionName here would not add anything.
                        Profiler.enabled = GUILayout.Toggle(Profiler.enabled, "Profile the Player in the Editor", EditorStyles.toolbarButton);
                        break;
                    default:
                        break;
                }
            }


            EditorGUILayout.EndHorizontal();

            var playerCount = EditorConnection.instance.ConnectedPlayers.Count;
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(string.Format("{0} players connected.", playerCount));
            int i = 0;
            foreach (var p in EditorConnection.instance.ConnectedPlayers)
            {
                builder.AppendLine(string.Format("[{0}] - {1} {2}", i++, p.name, p.playerId));
            }
            EditorGUILayout.HelpBox(builder.ToString(), MessageType.Info);
        }        


        
        private void OnEnable()
        {            
#if UNITY_2020_1_OR_NEWER
            m_attachProfilerState = ConnectionUtility.GetConnectionState(this);
#else
            m_attachProfilerState = ConnectionUtility.GetAttachToPlayerState(this);
#endif
            UnityEditor.Networking.PlayerConnection.EditorConnection.instance.Initialize();
            UnityEditor.Networking.PlayerConnection.EditorConnection.instance.Register(UnityChoseKun.kMsgSendPlayerToEditor, OnMessageEvent);

            onGUILayoutFuncDict = new Dictionary<string, Action>()
            {
                {"Inspector",   inspectorView.OnGUI},
                {"Component",   objectCounterView.OnGUI },
                {"Texture",     texturesView.OnGUI},
                {"Shader",      shaderView.OnGUI},
                {"Screen",      screenView.OnGUI },
                {"Time",        timeView.OnGUI},
                {"Application", applicationView.OnGUI},
                {"Android",     androidView.OnGUI},
                {"Quality", qualitySettingsView.OnGUI },
                {"OnDemandRendering",onDemandRenderingView.OnGUI },

                // 機能をここに追加していく                                              
            };
                        
            onMessageFuncDict = new Dictionary<UnityChoseKun.MessageID, OnMessageFunc>()
            {
                {UnityChoseKun.MessageID.ScreenPull,        screenView.OnMessageEvent},
                {UnityChoseKun.MessageID.TimePull,          timeView.OnMessageEvent },
                {UnityChoseKun.MessageID.GameObjectPull,    inspectorView.OnMessageEvent},
                {UnityChoseKun.MessageID.ShaderPull,        shaderView.OnMessageEvent},
                {UnityChoseKun.MessageID.TexturePull,       texturesView.OnMessageEvent},
                {UnityChoseKun.MessageID.ApplicationPull,   applicationView.OnMessageEvent },
                {UnityChoseKun.MessageID.AndroidPull,       androidView.OnMessageEvent },
                {UnityChoseKun.MessageID.QualitySettingsPull,   qualitySettingsView.OnMessageEvent},
                {UnityChoseKun.MessageID.OnDemandRenderingPull,onDemandRenderingView.OnMessageEvent },
                // 機能をここに追加していく                                              
            };
            
        }

        private void OnDisable()
        {            
            if (onMessageFuncDict != null)
            {
                onMessageFuncDict.Clear();
                onMessageFuncDict = null;
            }
            
            if (onGUILayoutFuncDict != null)
            {
                onGUILayoutFuncDict.Clear();
                onGUILayoutFuncDict = null;
            }


            UnityEditor.Networking.PlayerConnection.EditorConnection.instance.Unregister(UnityChoseKun.kMsgSendPlayerToEditor, OnMessageEvent);
            UnityEditor.Networking.PlayerConnection.EditorConnection.instance.DisconnectAll();

            if (m_attachProfilerState != null)
            {
                m_attachProfilerState.Dispose();
                m_attachProfilerState = null;
            }
        }        

               
        /// <summary>
        /// メッセージの受信CB
        /// </summary>
        /// <param name="args">メッセージデータ</param>
        private void OnMessageEvent(UnityEngine.Networking.PlayerConnection.MessageEventArgs args)
        {
            MemoryStream memoryStream = new MemoryStream(args.data);
            BinaryReader binaryReader = new BinaryReader(memoryStream);

            var messageID = (UnityChoseKun.MessageID)binaryReader.ReadInt32();


            
            UnityChoseKun.Log("UnityChosekunEditorWindow.OnMessageEvent(playerID: " + args.playerId + ",message.id" + messageID + ")");            
            if (onMessageFuncDict != null && onMessageFuncDict.ContainsKey(messageID) == true)
            {
                var func = onMessageFuncDict[messageID];
                func(binaryReader);
            }

            binaryReader.Close();
            memoryStream.Close();
        }
    }
}

