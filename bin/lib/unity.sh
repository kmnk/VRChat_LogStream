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

# run_unity <ログのパス> <Unity の引数…>。このリポジトリのプロジェクトで動かし、Unity の終了コードを返す
run_unity() {
  run_unity_at "$root" "$@"
}

# run_unity_at <プロジェクトのパス> <ログのパス> <Unity の引数…>。指定したプロジェクトで動かし、Unity の終了コードを返す
# Unity Hub のライセンスのクライアントが既定の窓口（/tmp/LicenseClient-<ユーザー名>.sock）で待っていないときは、
# UNITY_LICENSING_IPC に Hub の窓口の名前（/tmp/Unity-LicenseClient-<乱数>.sock の Unity- と .sock を除いたもの）を渡す
run_unity_at() {
  local project="$1" log="$2"
  shift 2
  local unity
  unity="$(unity_path)"
  if [ ! -x "$unity" ]; then
    echo "Unity が見つからない: ${unity}。Unity Hub で $(sed -n 's/^m_EditorVersion: //p' "$root/ProjectSettings/ProjectVersion.txt") を入れるか、UNITY_EDITOR で場所を渡す" >&2
    return 1
  fi
  if [ -e "$project/Temp/UnityLockfile" ] && lsof "$project/Temp/UnityLockfile" >/dev/null 2>&1; then
    echo "このプロジェクト（${project}）を Unity の Editor が開いている。Editor を閉じてから動かす" >&2
    return 1
  fi
  local licensing=()
  if [ -n "${UNITY_LICENSING_IPC:-}" ]; then
    licensing=(-licensingIpc "$UNITY_LICENSING_IPC")
  fi
  mkdir -p "$(dirname "$log")"
  local code=0
  "$unity" -batchmode -nographics -projectPath "$project" -buildTarget Win64 "${licensing[@]+"${licensing[@]}"}" -logFile "$log" "$@" || code=$?
  return $code
}

# compile_errors <ログのパス>。C# と UdonSharp のエラーの行を出す
compile_errors() {
  # UdonSharp のエラーは「[<color=…>UdonSharp</color>] <ファイル>(<行>,<列>): <文面>」の形で出る
  grep -E 'error CS[0-9]+|UdonSharp(</color>)?\] .*\.cs\([0-9]+,[0-9]+\)|Scripts have compiler errors' "$1" \
    | sed -E 's/<\/?color[^>]*>//g' | sort -u || true
}

# unity_license_hint <ログのパス>。ライセンスで止まったときに、UNITY_LICENSING_IPC に渡す候補を出す
unity_license_hint() {
  grep -q 'No valid Unity Editor license found' "$1" 2>/dev/null || return 0
  echo "Unity がライセンスを取れずに止まった。Unity Hub の窓口を UNITY_LICENSING_IPC で渡して動かし直す:" >&2
  local s
  for s in /tmp/Unity-LicenseClient-*.sock; do
    [ -e "$s" ] || continue
    case "$s" in *-notifications.sock) continue ;; esac
    s="${s#/tmp/Unity-}"
    echo "  UNITY_LICENSING_IPC=${s%.sock} <同じコマンド>" >&2
  done
}
