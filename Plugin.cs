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

        void Awake()
        {
            _bubbleRect.x = PlayerPrefs.GetFloat("BubbleX", 20);
            _bubbleRect.y = PlayerPrefs.GetFloat("BubbleY", 300);
            _selectedMode = PlayerPrefs.GetInt("Mod_SpeedMode", 2);
            _level = PlayerPrefs.GetFloat("Mod_SpeedLevel", 1.0f);
        }

        void OnGUI()
        {
            if (!_showMenu) _bubbleRect = GUI.Window(99, _bubbleRect, DrawBubble, "");
            else _windowRect = GUI.Window(0, _windowRect, DrawMainWindow, "ENEMY SPEED CONTROL");
        }

        void DrawBubble(int windowID)
        {
            if (GUI.Button(new Rect(0, 0, _bubbleRect.width, _bubbleRect.height), "SPEED MOD")) _showMenu = true;
            GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        }

        void DrawMainWindow(int windowID)
        {
            GUILayout.BeginVertical();
            GUILayout.Label($"Multiplier: {(_selectedMode == 1 ? (1f/(float)Math.Floor(_level)).ToString("F2") : ((int)_level).ToString())}x");
            
            if (GUILayout.Toggle(_selectedMode == 0, " INCREASE")) _selectedMode = 0;
            if (GUILayout.Toggle(_selectedMode == 1, " DECREASE")) _selectedMode = 1;
            if (GUILayout.Toggle(_selectedMode == 2, " NORMAL")) _selectedMode = 2;

            _level = GUILayout.HorizontalSlider(_level, 1f, 5f);

            if (GUILayout.Button("SAVE & CLOSE", GUILayout.Height(60)))
            {
                SaveSettings();
                _showMenu = false;
            }
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        void Update()
        {
            float floorLevel = (float)Math.Floor(_level);
            _currentSpeedMult = _selectedMode == 0 ? floorLevel : (_selectedMode == 1 ? 1.0f / floorLevel : 1.0f);

            ApplyDeepHooks();
        }

        private void ApplyDeepHooks()
        {
            // Find all potential actors (Enemies=11, NPCs=12, Projectiles=17)
            GameObject[] allObjects = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            
            foreach (GameObject obj in allObjects)
            {
                if (obj == null) continue;
                int layer = obj.layer;

                if (layer == 11 || layer == 12 || layer == 17)
                {
                    // 1. Standard Animators
                    var anim = obj.GetComponent<Animator>();
                    if (anim != null) anim.speed = _currentSpeedMult;

                    // 2. Spine / Skeleton Animations (Common for NPC abilities)
                    // We use SendMessage to avoid needing the Spine DLL as a reference
                    obj.SendMessage("set_timeScale", _currentSpeedMult, SendMessageOptions.DontRequireReceiver);

                    // 3. PlayMaker FSMs (Common for Boss/NPC logic)
                    obj.SendMessage("SetFsmSpeed", _currentSpeedMult, SendMessageOptions.DontRequireReceiver);
                }
                else if (layer == 9) // Protect Hornet
                {
                    var anim = obj.GetComponent<Animator>();
                    if (anim != null) anim.speed = 1.0f;
                    obj.SendMessage("set_timeScale", 1.0f, SendMessageOptions.DontRequireReceiver);
                }
            }
        }

        void SaveSettings()
        {
            PlayerPrefs.SetInt("Mod_SpeedMode", _selectedMode);
            PlayerPrefs.SetFloat("Mod_SpeedLevel", _level);
            PlayerPrefs.SetFloat("BubbleX", _bubbleRect.x);
            PlayerPrefs.SetFloat("BubbleY", _bubbleRect.y);
            PlayerPrefs.Save();
        }
    }
}
