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
            // Mobile UI Scaling
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(Screen.width / 1920f, Screen.height / 1080f, 1));

            if (!_showMenu)
                _bubbleRect = GUI.Window(99, _bubbleRect, DrawBubble, "DRAG BAR");
            else
                _windowRect = GUI.Window(0, _windowRect, DrawMainWindow, "QUEST NPC CONTROL");
        }

        void DrawBubble(int windowID)
        {
            if (GUI.Button(new Rect(10, 35, 180, 55), "OPEN MOD")) _showMenu = true;
            GUI.DragWindow(new Rect(0, 0, 200, 30)); // Drag handle at the top
        }

        void DrawMainWindow(int windowID)
        {
            GUILayout.BeginVertical();
            float val = Mathf.Floor(_level);
            _currentSpeed = (_selectedMode == 0) ? val : (_selectedMode == 1 ? 1f / val : 1f);
            
            GUILayout.Label($"<size=30>NPC Multiplier: {_currentSpeed:F2}x</size>");

            if (GUILayout.Toggle(_selectedMode == 0, " FAST SPEED")) _selectedMode = 0;
            if (GUILayout.Toggle(_selectedMode == 1, " SLOW MOTION")) _selectedMode = 1;
            if (GUILayout.Toggle(_selectedMode == 2, " NORMAL (1x)")) _selectedMode = 2;

            _level = GUILayout.HorizontalSlider(_level, 1f, 10f);
            
            GUILayout.Space(30);
            if (GUILayout.Button("FORCE UPDATE NPC", GUILayout.Height(80))) { ApplyToQuestNpc(); }
            
            if (GUILayout.Button("CLOSE & SAVE", GUILayout.Height(70))) { 
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
            // Update every 2 seconds to make sure he doesn't reset after a dialogue
            _pulseTimer += Time.deltaTime;
            if (_pulseTimer >= 2.0f)
            {
                ApplyToQuestNpc();
                _pulseTimer = 0;
            }
        }

        private void ApplyToQuestNpc()
        {
            // Fastest way to find objects in Unity 2022+ (Fixes your warnings)
            GameObject[] all = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            
            foreach (var obj in all)
            {
                if (obj == null) continue;
                string name = obj.name.ToUpper();

                // Target both Sprintmaster and Speedmaster specifically
                if (name.Contains("SPRINTMASTER") || name.Contains("SPEEDMASTER"))
                {
                    // 1. Force the Visuals (Animators/Spine)
                    obj.SendMessage("set_timeScale", _currentSpeed, SendMessageOptions.DontRequireReceiver);
                    var anim = obj.GetComponentInChildren<Animator>(true);
                    if (anim != null) anim.speed = _currentSpeed;

                    // 2. Force the Brain (FSM logic for Quests)
                    obj.SendMessage("SetFsmSpeed", _currentSpeed, SendMessageOptions.DontRequireReceiver);
                    obj.SendMessage("SetFsmTimeScale", _currentSpeed, SendMessageOptions.DontRequireReceiver);

                    // 3. Force the Physics (Walking/Running)
                    obj.SendMessage("set_speed", _currentSpeed, SendMessageOptions.DontRequireReceiver);
                    obj.SendMessage("SetSpeed", _currentSpeed, SendMessageOptions.DontRequireReceiver);
                }
            }
        }
    }
}
