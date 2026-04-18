using BepInEx;
using UnityEngine;
using System;

namespace WorldMod.Speed
{
    [BepInPlugin("com.game.worldspeed", "World Speed Controller", "1.0.0")]
    public class WorldSpeedPlugin : BaseUnityPlugin
    {
        private bool _showMenu = false;
        private Rect _bubbleRect = new Rect(50, 300, 200, 100); 
        private Rect _windowRect = new Rect(100, 100, 500, 550);
        
        private int _selectedMode = 2; 
        private float _level = 1.0f;
        private float _currentSpeed = 1.0f;

        // Optimization: Store the NPC so we can force it every frame
        private GameObject _npcRef;
        private float _scanTimer = 0f;

        void Awake()
        {
            _bubbleRect.x = PlayerPrefs.GetFloat("Mod_BubbleX", 50);
            _bubbleRect.y = PlayerPrefs.GetFloat("Mod_BubbleY", 300);
            _selectedMode = PlayerPrefs.GetInt("Mod_SpeedMode", 2);
            _level = PlayerPrefs.GetFloat("Mod_SpeedLevel", 1.0f);
        }

        void OnGUI()
        {
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(Screen.width / 1920f, Screen.height / 1080f, 1));

            if (!_showMenu)
                _bubbleRect = GUI.Window(99, _bubbleRect, DrawBubble, "DRAG AREA");
            else
                _windowRect = GUI.Window(0, _windowRect, DrawMainWindow, "NPC OVERDRIVE");
        }

        void DrawBubble(int windowID)
        {
            if (GUI.Button(new Rect(10, 35, 180, 55), "MENU")) _showMenu = true;
            GUI.DragWindow(new Rect(0, 0, 200, 30));
        }

        void DrawMainWindow(int windowID)
        {
            GUILayout.BeginVertical();
            float val = Mathf.Floor(_level);
            _currentSpeed = (_selectedMode == 0) ? val : (_selectedMode == 1 ? 1f / val : 1f);
            
            GUILayout.Label($"<size=30>NPC Target Speed: {_currentSpeed:F2}x</size>");

            if (GUILayout.Toggle(_selectedMode == 0, " FAST")) _selectedMode = 0;
            if (GUILayout.Toggle(_selectedMode == 1, " SLOW")) _selectedMode = 1;
            if (GUILayout.Toggle(_selectedMode == 2, " NORMAL")) _selectedMode = 2;

            _level = GUILayout.HorizontalSlider(_level, 1f, 10f);
            
            GUILayout.Space(30);
            if (GUILayout.Button("RE-SCAN FOR NPC", GUILayout.Height(80))) { ScanForNpc(); }
            
            if (GUILayout.Button("CLOSE", GUILayout.Height(60))) { 
                PlayerPrefs.SetFloat("Mod_BubbleX", _bubbleRect.x);
                PlayerPrefs.SetFloat("Mod_BubbleY", _bubbleRect.y);
                PlayerPrefs.Save();
                _showMenu = false; 
            }
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        void Update()
        {
            // 1. Scan for the NPC once every few seconds if not found
            _scanTimer += Time.deltaTime;
            if (_scanTimer >= 3f)
            {
                if (_npcRef == null) ScanForNpc();
                _scanTimer = 0;
            }

            // 2. FORCE the speed every single frame
            if (_npcRef != null && _selectedMode != 2)
            {
                // Force Spine animation
                _npcRef.SendMessage("set_timeScale", _currentSpeed, SendMessageOptions.DontRequireReceiver);
                
                // Force PlayMaker Logic
                _npcRef.SendMessage("SetFsmSpeed", _currentSpeed, SendMessageOptions.DontRequireReceiver);
                _npcRef.SendMessage("SetFsmTimeScale", _currentSpeed, SendMessageOptions.DontRequireReceiver);

                // Force Animator
                var anim = _npcRef.GetComponentInChildren<Animator>();
                if (anim != null) anim.speed = _currentSpeed;
            }
        }

        private void ScanForNpc()
        {
            GameObject[] all = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var obj in all)
            {
                if (obj == null) continue;
                // Target by name or the NPC layer identified in your screenshot/data
                if (obj.name.Contains("Sprintmaster") || obj.layer == 12)
                {
                    _npcRef = obj;
                    return;
                }
            }
        }
    }
}
