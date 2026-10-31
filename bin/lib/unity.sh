# bin/ の Unity を使うコマンドが読み込む。Unity の場所を決め、batchmode で動かす
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"

unity_path() {
  if [ -n "${UNITY_EDITOR:-}" ]; then
    echo "$UNITY_EDITOR"
    return
  fi
  local version
  version="$(sed -n 's/^m_EditorVersion: //p' "$root/ProjectSettings/ProjectVersion.txt")"
  case "$(uname -s)" in
    Darwin) echo "/Applications/Unity/Hub/Editor/$version/Unity.app/Contents/MacOS/Unity" ;;
    MINGW*|MSYS*|CYGWIN*) echo "C:/Program Files/Unity/Hub/Editor/$version/Editor/Unity.exe" ;;
    *) echo "$HOME/Unity/Hub/Editor/$version/Editor/Unity" ;;
  esac
}

# run_unity <ログのパス> <Unity の引数…>。Unity の終了コードを返す
run_unity() {
  local log="$1"
  shift
  local unity
  unity="$(unity_path)"
  if [ ! -x "$unity" ]; then
    echo "Unity が見つからない: ${unity}。Unity Hub で $(sed -n 's/^m_EditorVersion: //p' "$root/ProjectSettings/ProjectVersion.txt") を入れるか、UNITY_EDITOR で場所を渡す" >&2
    return 1
  fi
  if [ -e "$root/Temp/UnityLockfile" ] && lsof "$root/Temp/UnityLockfile" >/dev/null 2>&1; then
    echo "このプロジェクトを Unity の Editor が開いている。Editor を閉じてから動かす" >&2
    return 1
  fi
  mkdir -p "$(dirname "$log")"
  local code=0
  "$unity" -batchmode -nographics -projectPath "$root" -buildTarget Win64 -logFile "$log" "$@" || code=$?
  return $code
}

# compile_errors <ログのパス>。C# と UdonSharp のエラーの行を出す
compile_errors() {
  # UdonSharp のエラーは「[<color=…>UdonSharp</color>] <ファイル>(<行>,<列>): <文面>」の形で出る
  grep -E 'error CS[0-9]+|UdonSharp(</color>)?\] .*\.cs\([0-9]+,[0-9]+\)|Scripts have compiler errors' "$1" \
    | sed -E 's/<\/?color[^>]*>//g' | sort -u || true
}
