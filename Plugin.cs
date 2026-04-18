using BepInEx;
using UnityEngine;
using System;

namespace WorldMod.Speed
{
    [BepInPlugin("com.game.worldspeed", "World Speed Controller", "1.0.0")]
    public class WorldSpeedPlugin : BaseUnityPlugin
    {
        private bool _showMenu = false;
        
        // Rects for the UI
        private Rect _bubbleRect = new Rect(20, 300, 120, 60); 
        private Rect _windowRect = new Rect(100, 100, 400, 500);
        
        private int _selectedMode = 2; // 0=Inc, 1=Dec, 2=Norm
        private float _level = 1.0f;
        private float _currentSpeedMult = 1.0f;

        void Awake()
        {
            // Load saved settings
            _bubbleRect.x = PlayerPrefs.GetFloat("BubbleX", 20);
            _bubbleRect.y = PlayerPrefs.GetFloat("BubbleY", 300);
            _selectedMode = PlayerPrefs.GetInt("Mod_SpeedMode", 2);
            _level = PlayerPrefs.GetFloat("Mod_SpeedLevel", 1.0f);
            
            Logger.LogInfo("Speed Mod Initialized");
        }

        void OnGUI()
        {
            if (!_showMenu)
            {
                // Use a unique Window ID (e.g., 99) for the bubble to prevent conflicts
                _bubbleRect = GUI.Window(99, _bubbleRect, DrawBubble, "");
            }
            else
            {
                _windowRect = GUI.Window(0, _windowRect, DrawMainWindow, "ENEMY SPEED CONTROL");
            }
        }

        void DrawBubble(int windowID)
        {
            // The button fills the window
            if (GUI.Button(new Rect(0, 0, _bubbleRect.width, _bubbleRect.height), "SPEED MOD"))
            {
                _showMenu = true;
            }
            // Dragging enabled for the whole bubble area
            GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        }

        void DrawMainWindow(int windowID)
        {
            GUILayout.BeginVertical();
            GUILayout.Space(10);

            // MODE 0: INCREASE (1x, 2x, 3x, 4x, 5x)
            if (GUILayout.Toggle(_selectedMode == 0, " [MODE] INCREASE SPEED")) _selectedMode = 0;
            if (_selectedMode == 0)
            {
                GUILayout.Label($"Speed Multiplier: {(int)_level}x");
                _level = GUILayout.HorizontalSlider(_level, 1f, 5f);
            }

            GUILayout.Space(15);

            // MODE 1: DECREASE (1/1, 1/2, 1/3, 1/4, 1/5)
            if (GUILayout.Toggle(_selectedMode == 1, " [MODE] DECREASE SPEED")) _selectedMode = 1;
            if (_selectedMode == 1)
            {
                // Calculation shown to user: 1.0 / Level
                float displaySpeed = 1.0f / (float)Math.Floor(_level);
                GUILayout.Label($"Slowness Level: {(int)_level} (Speed: {displaySpeed:F2}x)");
                _level = GUILayout.HorizontalSlider(_level, 1f, 5f);
            }

            GUILayout.Space(15);

            if (GUILayout.Toggle(_selectedMode == 2, " [MODE] NORMAL")) _selectedMode = 2;

            GUILayout.FlexibleSpace();

            GUI.color = Color.green;
            if (GUILayout.Button("SAVE & CLOSE", GUILayout.Height(60)))
            {
                SaveSettings();
                _showMenu = false;
            }
            GUI.color = Color.white;

            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        void Update()
        {
            // Core Speed Logic
            float floorLevel = (float)Math.Floor(_level);
            
            if (_selectedMode == 0) 
                _currentSpeedMult = floorLevel; // 1 to 5
            else if (_selectedMode == 1) 
                _currentSpeedMult = 1.0f / floorLevel; // 1.0, 0.5, 0.33, 0.25, 0.20
            else 
                _currentSpeedMult = 1.0f;

            ApplyTargetedSpeed();
        }

        private void ApplyTargetedSpeed()
        {
            Animator[] anims = UnityEngine.Object.FindObjectsByType<Animator>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var anim in anims)
            {
                if (anim == null) continue;
                int layer = anim.gameObject.layer;

                // Targeted Layers: 11 (Enemy), 12 (NPC), 17 (Enemy Projectiles)
                if (layer == 11 || layer == 12 || layer == 17)
                {
                    anim.speed = _currentSpeedMult;
                }
                else if (layer == 9) // Force Player to stay 1.0
                {
                    anim.speed = 1.0f;
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
