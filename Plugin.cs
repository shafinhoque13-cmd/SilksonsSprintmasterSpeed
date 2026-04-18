using BepInEx;
using UnityEngine;
using System;

namespace WorldMod.Speed
{
    [BepInPlugin("com.game.worldspeed", "World Speed Controller", "1.0.0")]
    public class WorldSpeedPlugin : BaseUnityPlugin
    {
        private bool _showMenu = false;
        private Rect _bubbleRect = new Rect(20, 300, 160, 70); 
        private Rect _windowRect = new Rect(100, 100, 450, 400);
        
        private int _selectedMode = 2; 
        private float _level = 1.0f;
        private float _currentSpeedMult = 1.0f;
        private float _scanTimer = 0f;

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
            else _windowRect = GUI.Window(0, _windowRect, DrawMainWindow, "TARGET: SPRINTMASTER");
        }

        void DrawBubble(int windowID) {
            if (GUI.Button(new Rect(5, 5, 150, 60), "SPEED MENU")) _showMenu = true;
            GUI.DragWindow();
        }

        void DrawMainWindow(int windowID) {
            GUILayout.BeginVertical();
            float displayVal = (_selectedMode == 1) ? (1.0f / Mathf.Floor(_level)) : Mathf.Floor(_level);
            GUILayout.Label($"Sprintmaster Speed: {displayVal:F2}x");

            if (GUILayout.Toggle(_selectedMode == 0, " FAST")) _selectedMode = 0;
            if (GUILayout.Toggle(_selectedMode == 1, " SLOW")) _selectedMode = 1;
            if (GUILayout.Toggle(_selectedMode == 2, " NORMAL")) _selectedMode = 2;

            _level = GUILayout.HorizontalSlider(_level, 1f, 5f);
            
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("SAVE & CLOSE", GUILayout.Height(70))) { SaveSettings(); _showMenu = false; }
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        void Update()
        {
            float floorLevel = Mathf.Floor(_level);
            _currentSpeedMult = _selectedMode == 0 ? floorLevel : (_selectedMode == 1 ? 1.0f / floorLevel : 1.0f);

            // Scan every 2 seconds - very light on FPS
            _scanTimer += Time.deltaTime;
            if (_scanTimer >= 2.0f) {
                TargetSprintmaster();
                _scanTimer = 0;
            }
        }

        private void TargetSprintmaster()
        {
            // Lock Global Time to 1.0 to ensure Hornet stays normal
            Time.timeScale = 1.0f;

            // Find only objects with the exact name "Sprintmaster"
            GameObject npc = GameObject.Find("Sprintmaster");
            
            // If the main object isn't found, try finding by partial name (clones/variants)
            if (npc == null)
            {
                GameObject[] all = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                foreach(var o in all) { if(o.name.Contains("Sprintmaster")) { npc = o; break; } }
            }

            if (npc != null)
            {
                // 1. Force the Animator speed
                var anim = npc.GetComponentInChildren<Animator>();
                if (anim != null) anim.speed = _currentSpeedMult;

                // 2. Force the internal logic/FSM speed
                npc.SendMessage("set_speed", _currentSpeedMult, SendMessageOptions.DontRequireReceiver);
                npc.SendMessage("set_timeScale", _currentSpeedMult, SendMessageOptions.DontRequireReceiver);
                npc.SendMessage("SetFsmSpeed", _currentSpeedMult, SendMessageOptions.DontRequireReceiver);
            }
        }

        void SaveSettings() {
            PlayerPrefs.SetInt("Mod_SpeedMode", _selectedMode);
            PlayerPrefs.SetFloat("Mod_SpeedLevel", _level);
            PlayerPrefs.SetFloat("Mod_BubbleX", _bubbleRect.x);
            PlayerPrefs.SetFloat("Mod_BubbleY", _bubbleRect.y);
            PlayerPrefs.Save();
        }
    }
}
