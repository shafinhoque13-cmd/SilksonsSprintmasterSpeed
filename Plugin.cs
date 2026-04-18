using BepInEx;
using UnityEngine;
using System;

namespace WorldMod.Speed
{
    [BepInPlugin("com.game.worldspeed", "World Speed Controller", "1.0.0")]
    public class WorldSpeedPlugin : BaseUnityPlugin
    {
        private bool _showMenu = false;
        private Rect _bubbleRect = new Rect(20, 300, 120, 60); 
        private Rect _windowRect = new Rect(100, 100, 400, 500);
        
        private int _selectedMode = 2; 
        private float _level = 1.0f;
        private float _currentSpeedMult = 1.0f;
        private float _timer = 0f;

        void Awake()
        {
            _bubbleRect.x = PlayerPrefs.GetFloat("Mod_BubbleX", 20);
            _bubbleRect.y = PlayerPrefs.GetFloat("Mod_BubbleY", 300);
            _selectedMode = PlayerPrefs.GetInt("Mod_SpeedMode", 2);
            _level = PlayerPrefs.GetFloat("Mod_SpeedLevel", 1.0f);
        }

        void OnGUI()
        {
            if (!_showMenu) _bubbleRect = GUI.Window(99, _bubbleRect, DrawBubble, "");
            else _windowRect = GUI.Window(0, _windowRect, DrawMainWindow, "NPC SPEED ONLY");
        }

        void DrawBubble(int windowID)
        {
            if (GUI.Button(new Rect(0, 0, _bubbleRect.width, _bubbleRect.height), "SPEED MOD")) _showMenu = true;
            GUI.DragWindow();
        }

        void DrawMainWindow(int windowID)
        {
            GUILayout.BeginVertical();
            float displayVal = (_selectedMode == 1) ? (1.0f / Mathf.Floor(_level)) : Mathf.Floor(_level);
            GUILayout.Label($"Target Speed: {displayVal:F2}x");

            if (GUILayout.Toggle(_selectedMode == 0, " [MODE] INCREASE ENEMIES")) _selectedMode = 0;
            if (GUILayout.Toggle(_selectedMode == 1, " [MODE] DECREASE ENEMIES")) _selectedMode = 1;
            if (GUILayout.Toggle(_selectedMode == 2, " [MODE] NORMAL")) _selectedMode = 2;

            _level = GUILayout.HorizontalSlider(_level, 1f, 5f);

            GUILayout.Space(20);
            if (GUILayout.Button("FORCE UPDATE AREA", GUILayout.Height(50))) { ManualUpdate(); }

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("SAVE & CLOSE", GUILayout.Height(60))) { SaveSettings(); _showMenu = false; }
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        void Update()
        {
            float floorLevel = Mathf.Floor(_level);
            _currentSpeedMult = _selectedMode == 0 ? floorLevel : (_selectedMode == 1 ? 1.0f / floorLevel : 1.0f);

            // Optimization: Update NPC speeds every 1 second
            _timer += Time.deltaTime;
            if (_timer >= 1.0f)
            {
                ManualUpdate();
                _timer = 0;
            }
        }

        private void ManualUpdate()
        {
            // We NO LONGER use Time.timeScale. We keep it at 1.0 to protect Hornet.
            Time.timeScale = 1.0f;

            GameObject[] allObjects = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (GameObject obj in allObjects)
            {
                if (obj == null) continue;

                // Targeted Layers: 11 (Enemy), 12 (NPC), 17 (Projectiles)
                // WE IGNORE LAYER 9 (Player) COMPLETELY
                if (obj.layer == 11 || obj.layer == 12 || obj.layer == 17)
                {
                    // Update Animator
                    var anim = obj.GetComponent<Animator>();
                    if (anim != null) anim.speed = _currentSpeedMult;

                    // Update Physics Velocity (For projectiles/movement)
                    var rb = obj.GetComponent<Rigidbody2D>();
                    if (rb != null)
                    {
                        // We use SendMessage to avoid direct script conflicts
                        obj.SendMessage("set_speed", _currentSpeedMult, SendMessageOptions.DontRequireReceiver);
                    }

                    // Update Spine/FSM logic
                    obj.SendMessage("set_timeScale", _currentSpeedMult, SendMessageOptions.DontRequireReceiver);
                    obj.SendMessage("SetFsmSpeed", _currentSpeedMult, SendMessageOptions.DontRequireReceiver);
                }
            }
        }

        void SaveSettings()
        {
            PlayerPrefs.SetInt("Mod_SpeedMode", _selectedMode);
            PlayerPrefs.SetFloat("Mod_SpeedLevel", _level);
            PlayerPrefs.SetFloat("Mod_BubbleX", _bubbleRect.x);
            PlayerPrefs.SetFloat("Mod_BubbleY", _bubbleRect.y);
            PlayerPrefs.Save();
            ManualUpdate();
        }
    }
}
