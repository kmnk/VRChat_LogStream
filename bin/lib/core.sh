# bin/check-core と bin/sync-core が読み込む。Core の git を決める
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"

# CORE_REPO があればそれを使い、無ければ Build/VRChat_Core に clone する（Unity は終わるときに Temp/ を消すので Build/ に置く）。Core の作業ツリーには触れない
core_repo() {
  if [ -n "${CORE_REPO:-}" ]; then
    echo "$CORE_REPO"
    return
  fi
  local dir="$root/Build/VRChat_Core"
  if [ ! -d "$dir/.git" ] && [ ! -f "$dir/HEAD" ]; then
    mkdir -p "$root/Build"
    git clone --quiet --bare https://github.com/kmnk/VRChat_Core.git "$dir" >&2 || return 1
    git -C "$dir" config remote.origin.fetch '+refs/heads/*:refs/remotes/origin/*' || return 1
  fi
  echo "$dir"
}
