#!/bin/zsh
set -eu
project_dir="${0:A:h}"
unity_bin="${UNITY_EDITOR_PATH:-}"
if [[ -z "$unity_bin" && -f "$project_dir/.unity-editor-path" ]]; then
  IFS= read -r unity_bin < "$project_dir/.unity-editor-path"
fi
if [[ -z "$unity_bin" ]]; then
  for candidate in \
    /Applications/Unity/Hub/Editor/2022.3.22f1/Unity.app/Contents/MacOS/Unity \
    /Applications/Unity/Unity.app/Contents/MacOS/Unity; do
    if [[ -x "$candidate" ]]; then unity_bin="$candidate"; break; fi
  done
fi
if [[ -z "$unity_bin" || ! -x "$unity_bin" ]]; then
  print 'Set UNITY_EDITOR_PATH or .unity-editor-path to the Unity 2022.3.22f1 executable.'
  exit 1
fi
mkdir -p "$project_dir/Logs"
"$unity_bin" -batchmode -quit -projectPath "$project_dir" \
  -executeMethod DesktopFish.Editor.FishRebuiltModel.Build \
  -logFile "$project_dir/Logs/build.log"
if [[ ! -d "$project_dir/Builds/RebuiltBigFish.app" ]]; then
  print "Build did not produce an app. See $project_dir/Logs/build.log"
  exit 1
fi
print "Built: $project_dir/Builds/RebuiltBigFish.app"
