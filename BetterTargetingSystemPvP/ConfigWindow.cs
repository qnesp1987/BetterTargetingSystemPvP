using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using System;
using System.Numerics;
using BetterTargetingSystem.Keybinds;
using BetterTargetingSystem;

namespace BetterTargetingSystem.Windows
{
    public class ConfigWindow : Window, IDisposable
    {
        private readonly Plugin Plugin;
        private readonly Configuration Configuration;

        public ConfigWindow(Plugin plugin) : base(
            "Better Targeting System",
            ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
        {
            this.Size = new Vector2(185, 270);
            this.SizeCondition = ImGuiCond.Appearing;

            this.Plugin = plugin;
            this.Configuration = plugin.Configuration;
        }

        public override void Draw()
        {
            if (!IsOpen) return;

            ImGui.TextUnformatted("Better Targeting System - Configuration");
            ImGui.Separator();

            // Sample controls - keep minimal to avoid introducing new dependencies
            ImGui.Text("Settings go here.");
        }

        public void Dispose()
        {
            // Cleanup if needed
        }
    }
}


