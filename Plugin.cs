using BepInEx;
using UnityEngine;
using System;

namespace WorldMod.Speed
{
    [BepInPlugin("com.game.worldspeed", "World Speed Controller", "1.0.0")]
    public class WorldSpeedPlugin : BaseUnityPlugin
    {
        // 0 = Increase, 1 = Decrease, 2 = Normal
        private int _selectedMode = 2; 
        private float _level = 1.0f;
        private float _currentActiveSpeed = 1.0f;

        void Awake()
        {
            Logger.LogInfo("World Speed Controller: Integrated Menu Loaded");
        }

        // This is what draws the options inside the Mod List menu
        void OnGUI()
        {
            GUILayout.BeginVertical("box");
            
            GUILayout.Label("--- SPEED SETTINGS ---", GUI.skin.label);

            // MODE 1: INCREASE
            bool isIncrease = GUILayout.Toggle(_selectedMode == 0, "Increase Speed Mode");
            if (isIncrease) _selectedMode = 0;
            if (_selectedMode == 0)
            {
                GUILayout.Label($"Speed Level: {(int)_level} (Fast)");
                _level = GUILayout.HorizontalSlider(_level, 1f, 5f);
            }

            GUILayout.Space(10);

            // MODE 2: DECREASE
            bool isDecrease = GUILayout.Toggle(_selectedMode == 1, "Decrease Speed Mode");
            if (isDecrease) _selectedMode = 1;
            if (_selectedMode == 1)
            {
                GUILayout.Label($"Speed Level: {(int)_level} (Slow-Mo)");
                _level = GUILayout.HorizontalSlider(_level, 1f, 5f);
            }

            GUILayout.Space(10);

            // MODE 3: NORMAL
            bool isNormal = GUILayout.Toggle(_selectedMode == 2, "Normal Speed (Reset)");
            if (isNormal) _selectedMode = 2;

            GUILayout.EndVertical();
        }

        void Update()
        {
            float targetSpeed = 1.0f;

            if (_selectedMode == 0) // Increase Logic
                targetSpeed = 1.0f + ((float)Math.Floor(_level) * 0.8f); 
            else if (_selectedMode == 1) // Decrease Logic
                targetSpeed = 1.0f - ((float)Math.Floor(_level) * 0.15f);
            else // Normal
                targetSpeed = 1.0f;

            // Only update if speed actually changed to save performance
            if (Math.Abs(targetSpeed - _currentActiveSpeed) > 0.01f)
            {
                _currentActiveSpeed = targetSpeed;
                ApplySpeedChange(targetSpeed);
            }
        }

        private void ApplySpeedChange(float speed)
        {
            Animator[] allAnimators = UnityEngine.Object.FindObjectsByType<Animator>(FindObjectsSortMode.None);
            foreach (Animator anim in allAnimators)
            {
                if (anim == null) continue;

                // Layer 9 check ensures Hornet (The Player) is NEVER affected
                if (anim.gameObject.layer == 9)
                {
                    anim.speed = 1.0f;
                    continue;
                }

                anim.speed = speed;
            }
        }
    }
}
