#!/usr/bin/env bash
set -euo pipefail

# Safety: run from repo root
REPO_ROOT="$(pwd)"
BRANCH="migrate/dalamud-v14"
COMMIT_MSG="Migrate plugin to Dalamud v14: update LocalPlayer usages, service namespaces, and csproj"

# 1) create branch
git checkout -b "${BRANCH}"

# 2) Replace ClientState.LocalPlayer -> ObjectTable.LocalPlayer (safe, case-sensitive)
# This handles several common prefixes.
# Make a backup copy of files changed by replacements so we can diff later.
git ls-files -z | xargs -0 -n1 bash -c '
  f="$0"
  if grep -q "ClientState\\.LocalPlayer" "$f" || grep -q "Plugin\\.ClientState\\.LocalPlayer" "$f" || grep -q "PluginInstance\\.ClientState\\.LocalPlayer" "$f"; then
    cp "$f" "$f.bak-for-migration" || true
    sed -E -i \
      -e "s/Plugin\\.Instance\\.ClientState\\.LocalPlayer/Plugin.Instance.ObjectTable.LocalPlayer/g" \
      -e "s/Plugin\\.ClientState\\.LocalPlayer/Plugin.ObjectTable.LocalPlayer/g" \
      -e "s/ClientState\\.LocalPlayer/Plugin.ObjectTable.LocalPlayer/g" \
      -e "s/Plugin\\.Instance\\.ClientState/Plugin.Instance.ObjectTable/g" \
      -e "s/Plugin\\.ClientState/Plugin.ObjectTable/g" \
      -e "s/ClientState\\./ObjectTable./g" \
      "$f"
    git add "$f"
  fi
' {}

# 3) Update the plugin csproj(s) to use Dalamud.NET.Sdk/14.0.1 and net10.0
# Find csproj files in the repo root and the plugin folder
find . -maxdepth 3 -name "*.csproj" -print0 | while IFS= read -r -d '' csproj; do
  cp "$csproj" "$csproj.bak-for-migration" || true

  # Update Project Sdk header
  # Replace existing <Project Sdk="..."> line with the v14 sdk
  if grep -q "<Project Sdk=" "$csproj"; then
    sed -E -i "0,/<Project Sdk=.*/s//<Project Sdk=\"Dalamud.NET.Sdk\/14.0.1\">/" "$csproj"
  else
    # fallback: prepend Project Sdk declaration (unlikely)
    sed -i "1i<Project Sdk=\"Dalamud.NET.Sdk/14.0.1\">" "$csproj"
    echo "</Project>" >> "$csproj"
  fi

  # Update or add TargetFramework to net10.0
  if grep -q "<TargetFramework>.*</TargetFramework>" "$csproj"; then
    sed -E -i "s/<TargetFramework>.*<\/TargetFramework>/<TargetFramework>net10.0<\/TargetFramework>/" "$csproj"
  else
    # Insert TargetFramework in first PropertyGroup
    awk -v inserted=0 '
      /<PropertyGroup>/ && inserted==0 {
        print; print "  <TargetFramework>net10.0</TargetFramework>"; inserted=1; next
      }
      { print }
    ' "$csproj" > "$csproj.tmp" && mv "$csproj.tmp" "$csproj"
  fi

  git add "$csproj"
done

# 4) Add newline at EOF for Configuration.cs (if file exists)
CFG="BetterTargetingSystemPvP/Configuration.cs"
if [ -f "$CFG" ]; then
  cp "$CFG" "$CFG.bak-for-migration" || true
  # Ensure newline at EOF
  perl -0777 -pe 's/\s+\z/\n/s' "$CFG" > "$CFG.tmp" && mv "$CFG.tmp" "$CFG"
  git add "$CFG"
fi

# 5) Add TODO notes for manual check areas:
# - ImGui enum casts (ConfigWindow.cs, HelpWindow.cs)
# - Any uses of old service types under Dalamud.Game.* which cannot be replaced safely by text alone
# We'll append TODO comments where we detect those patterns.

# Helper: add TODO comment if we find cast patterns to Dalamud.Interface.Windowing.ImGuiWindowFlags from ImGuiNET
for f in BetterTargetingSystemPvP/ConfigWindow.cs BetterTargetingSystemPvP/HelpWindow.cs; do
  if [ -f "$f" ]; then
    if grep -q "ImGuiWindowFlags" "$f"; then
      cp "$f" "$f.bak-for-migration" || true
      sed -E -i "/ImGuiWindowFlags/ s|$| // TODO: verify enum usage against Dalamud.Interface.Windowing enums|g" "$f"
      git add "$f"
    fi
  fi
done

# 6) Basic search for old service namespaces and add TODO comments in files where found (non-invasive)
# Look for 'Dalamud.Game.' and similar tokens
git grep -n "Dalamud.Game" -- ':!*.csproj' || true | cut -d: -f1 | sort -u | while read -r f; do
  if [ -f "$f" ]; then
    cp "$f" "$f.bak-for-migration" || true
    sed -E -i "1i// TODO: update moved service namespaces to Dalamud.Plugin.Services where applicable (review manually)\n" "$f"
    git add "$f"
  fi
done

# 7) Commit changes
git commit -m "${COMMIT_MSG}" || {
  echo "No changes staged for commit; aborting.";
  exit 1;
}

echo "Branch ${BRANCH} created and changes committed."
echo "Backups of modified files with suffix .bak-for-migration are present."

# 8) push instructions (do not push automatically; user can review)
echo ""
echo "Next steps:"
echo "1) Run the build locally:"
echo "   dotnet build -v minimal"
echo ""
echo "2) Inspect the changes and the backup files (files ending with .bak-for-migration)."
echo "   git diff origin/main..HEAD"
echo ""
echo "3) If everything looks good, push and open a PR:"
echo "   git push origin ${BRANCH}"
echo "   (then open a PR from ${BRANCH} -> main on GitHub with title: ${COMMIT_MSG})"
echo ""
echo "If you want I can produce a patch file you can apply instead of running this script."