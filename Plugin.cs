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
        private Rect _windowRect = new Rect(100, 100, 500, 600);
        
        private int _selectedMode = 2; 
        private float _level = 1.0f;
        private float _currentSpeed = 1.0f;
        private float _pulseTimer = 0f;

        void Awake()
        {
            _bubbleRect.x = PlayerPrefs.GetFloat("Mod_BubbleX", 50);
            _bubbleRect.y = PlayerPrefs.GetFloat("Mod_BubbleY", 300);
            _selectedMode = PlayerPrefs.GetInt("Mod_SpeedMode", 2);
            _level = PlayerPrefs.GetFloat("Mod_SpeedLevel", 1.0f);
        }

        void OnGUI()
        {
            // Fixes touch/UI scaling for mobile
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(Screen.width / 1920f, Screen.height / 1080f, 1));

            if (!_showMenu)
                _bubbleRect = GUI.Window(99, _bubbleRect, DrawBubble, "DRAG AREA");
            else
                _windowRect = GUI.Window(0, _windowRect, DrawMainWindow, "NPC MASTER CONTROL");
        }

        void DrawBubble(int windowID)
        {
            if (GUI.Button(new Rect(10, 35, 180, 55), "OPEN MENU")) _showMenu = true;
            GUI.DragWindow(new Rect(0, 0, 200, 30));
        }

        void DrawMainWindow(int windowID)
        {
            GUILayout.BeginVertical();
            float val = Mathf.Floor(_level);
            _currentSpeed = (_selectedMode == 0) ? val : (_selectedMode == 1 ? 1f / val : 1f);
            
            GUILayout.Label($"<size=30>NPC Power: {_currentSpeed:F2}x</size>");

            if (GUILayout.Toggle(_selectedMode == 0, " SUPER FAST")) _selectedMode = 0;
            if (GUILayout.Toggle(_selectedMode == 1, " SUPER SLOW")) _selectedMode = 1;
            if (GUILayout.Toggle(_selectedMode == 2, " NORMAL")) _selectedMode = 2;

            _level = GUILayout.HorizontalSlider(_level, 1f, 10f);
            
            GUILayout.Space(40);
            if (GUILayout.Button("FORCE UPDATE ALL NPCs", GUILayout.Height(80))) { ForcePulse(); }
            
            if (GUILayout.Button("SAVE & CLOSE", GUILayout.Height(80))) { SaveSettings(); _showMenu = false; }
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        void Update()
        {
            // Safety: Keep player timescale at 1.0
            Time.timeScale = 1.0f;

            // Slow Pulse: Update everything on the NPC layer every 2 seconds to keep FPS high
            _pulseTimer += Time.deltaTime;
            if (_pulseTimer >= 2.0f)
            {
                ForcePulse();
                _pulseTimer = 0;
            }
        }

        private void ForcePulse()
        {
            // We search for everything, but filter by LAYER to save performance
            GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
            foreach (GameObject obj in allObjects)
            {
                // Layer 12 is the standard NPC layer. Layer 11 is Enemies.
                if (obj.layer == 12 || obj.layer == 11)
                {
                    // 1. Force the Animator (Unity standard)
                    var anim = obj.GetComponentInChildren<Animator>(true);
                    if (anim != null) anim.speed = _currentSpeed;

                    // 2. Force PlayMaker FSMs (The 'Brain' of Sprintmaster)
                    obj.SendMessage("SetFsmSpeed", _currentSpeed, SendMessageOptions.DontRequireReceiver);

                    // 3. Force Spine Skeleton (The visual skeleton)
                    obj.SendMessage("set_timeScale", _currentSpeed, SendMessageOptions.DontRequireReceiver);
                    
                    // 4. Force Custom Speed Variables
                    obj.SendMessage("set_speed", _currentSpeed, SendMessageOptions.DontRequireReceiver);
                }
            }
        }

        void SaveSettings()
        {
            PlayerPrefs.SetFloat("Mod_BubbleX", _bubbleRect.x);
            PlayerPrefs.SetFloat("Mod_BubbleY", _bubbleRect.y);
            PlayerPrefs.SetInt("Mod_SpeedMode", _selectedMode);
            PlayerPrefs.SetFloat("Mod_SpeedLevel", _level);
            PlayerPrefs.Save();
        }
    }
}
