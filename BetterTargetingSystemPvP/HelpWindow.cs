using Dalamud.Interface.Windowing;
using ImGuiNET;
using System.Numerics;

namespace BetterTargetingSystemPvP
{
    public class HelpWindow : Window, System.IDisposable
    {
        public HelpWindow(Plugin plugin) : base(
            "Better Targeting System - Help",
            ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
        {
            this.Size = new Vector2(768, 512);
            this.SizeCondition = ImGuiCond.Appearing;
        }

        public override void Draw()
        {
            if (!IsOpen) return;

            ImGui.TextUnformatted("Better Targeting System - Help");
            ImGui.Separator();
            ImGui.TextWrapped("This window contains help and usage information for the plugin.");
        }

        public void Dispose()
        {
            // Cleanup if needed
        }
    }
}
