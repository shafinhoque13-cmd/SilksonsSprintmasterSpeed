using BepInEx;
using UnityEngine;
using System;

namespace WorldMod.Speed
{
    [BepInPlugin("com.game.worldspeed", "World Speed Controller", "1.0.0")]
    public class WorldSpeedPlugin : BaseUnityPlugin
    {
        // Settings states
        private int _selectedSection = 2; // 0=Increase, 1=Decrease, 2=Normal
        private float _level = 1.0f;
        
        // This is the function the Android Port's "Mod List" looks for
        void OnGUI()
        {
            // This 'GUILayout' container will appear inside the Mod List when you tap the mod
            GUILayout.BeginVertical("box");
            GUILayout.Label("--- SPEED SETTINGS ---");

            // Option 1: Increase
            if (GUILayout.Toggle(_selectedSection == 0, "Increase Speed Mode")) _selectedSection = 0;
            if (_selectedSection == 0)
            {
                GUILayout.Label($"Level: {(int)_level}");
                _level = GUILayout.HorizontalSlider(_level, 1f, 5f);
            }

            GUILayout.Space(10);

            // Option 2: Decrease
            if (GUILayout.Toggle(_selectedSection == 1, "Decrease Speed Mode")) _selectedSection = 1;
            if (_selectedSection == 1)
            {
                GUILayout.Label($"Level: {(int)_level}");
                _level = GUILayout.HorizontalSlider(_level, 1f, 5f);
            }

            GUILayout.Space(10);

            // Option 3: Normal
            if (GUILayout.Toggle(_selectedSection == 2, "Normal Speed (Reset)")) _selectedSection = 2;

            GUILayout.EndVertical();
        }

        void Update()
        {
            float targetSpeed = 1.0f;

            if (_selectedSection == 0) // Increase: Level 1=1.8x, Level 5=5.0x
                targetSpeed = 1.0f + ((float)Math.Floor(_level) * 0.8f);
            else if (_selectedSection == 1) // Decrease: Level 1=0.85x, Level 5=0.25x
                targetSpeed = 1.0f - ((float)Math.Floor(_level) * 0.15f);
            else // Normal
                targetSpeed = 1.0f;

            ApplySpeed(targetSpeed);
        }

        private void ApplySpeed(float speed)
        {
            Animator[] allAnimators = UnityEngine.Object.FindObjectsByType<Animator>(FindObjectsSortMode.None);
            foreach (Animator anim in allAnimators)
            {
                if (anim == null) continue;
                // Layer 9 is the Player. We keep Hornet at 1.0 speed.
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
