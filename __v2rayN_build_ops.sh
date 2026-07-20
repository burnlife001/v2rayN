#!/usr/bin/env bash
# v2rayN 构建/重基操作菜单 (Avalonia)
set -e

REPO_ROOT="$(cd "$(dirname "$0")" && pwd)"
cd "$REPO_ROOT"

SLN="v2rayN/v2rayN.Desktop/v2rayN.Desktop.csproj"

# ---- Rebase 参数（可按需修改默认值） ----
REBASE_BRANCH="burnlife001"
REBASE_UPSTREAM_REMOTE="upstream"
REBASE_UPSTREAM_BRANCH="master"
REBASE_ORIGIN_REMOTE="origin"

show_menu() {
    clear
    echo -e "\e[36m===== v2rayN Ops (Avalonia) =====\e[0m"
    echo ""
    echo "  1. Build"
    echo "  2. Fetch upstream & rebase"
    echo -e "\e[33m  0. Exit\e[0m"
    echo ""
}

build() {
    echo ""

    local v2rayn_path=""
    local was_running=0

    local ps_out
    ps_out="$(powershell -Command '$p = Get-Process -Name v2rayN -ErrorAction SilentlyContinue | Select-Object -First 1; if ($p) { Write-Output "RUNNING|$($p.Path)" } else { Write-Output "STOPPED|" }')"

    local status="${ps_out%%|*}"
    v2rayn_path="${ps_out#*|}"

    # NuGet 走系统代理 127.0.0.1:10808 (v2rayN 提供)，必须在停掉 v2rayN 之前完成还原。
    echo -e "\e[32m>>> Restoring NuGet packages (proxy still up)...\e[0m"
    if ! dotnet restore "$SLN" --nologo -v q; then
        local rc=$?
        echo -e "\e[31m<<< Restore FAILED (exit: $rc). v2rayN left running; aborting build.\e[0m"
        return 1
    fi
    echo -e "\e[32m<<< Restore OK\e[0m"

    if [ "$status" = "RUNNING" ]; then
        echo -e "\e[33m>>> v2rayN is running, stopping it before build...\e[0m"
        [ -n "$v2rayn_path" ] && echo "    $v2rayn_path" && was_running=1
        powershell -Command "Stop-Process -Name v2rayN -Force -ErrorAction SilentlyContinue; exit 0"
        if [ "$was_running" -eq 0 ]; then
            echo -e "\e[33m>>> Warning: could not determine v2rayN executable path; will not auto-restart\e[0m"
        fi
    else
        echo -e "\e[33m>>> v2rayN is not running\e[0m"
    fi

    echo -e "\e[32m>>> Building Avalonia (offline, --no-restore)...\e[0m"
    local build_ok=0
    if dotnet build "$SLN" -c Release --nologo -v q --no-restore -p:NuGetAudit=false; then
        echo -e "\e[32m<<< Build OK\e[0m"
        build_ok=1
    else
        local code=$?
        echo -e "\e[31m<<< Build FAILED (exit: $code)\e[0m"
    fi

    if [ "$was_running" -eq 1 ]; then
        echo -e "\e[32m>>> Restarting v2rayN...\e[0m"
        if V2RAYN_PATH="$v2rayn_path" powershell -Command 'if (Test-Path "$env:V2RAYN_PATH") { Start-Process -FilePath "$env:V2RAYN_PATH" } else { Write-Error "v2rayN exe not found: $env:V2RAYN_PATH"; exit 1 }'; then
            echo -e "\e[32m<<< v2rayN restarted\e[0m"
        else
            echo -e "\e[31m<<< Failed to restart v2rayN\e[0m"
        fi
    fi

    return $([ "$build_ok" -eq 1 ] && echo 0 || echo 1)
}

invoke_rebase() {
    local branch="$1" upstream_remote="$2" upstream_branch="$3" origin_remote="$4"

    step() { echo "[$1/$2] $3"; }

    local current_branch
    current_branch="$(git rev-parse --abbrev-ref HEAD)"
    step 1 7 "Current branch: $current_branch"

    local dirty
    dirty="$(git status --porcelain --untracked-files=no)"
    if [ -n "$dirty" ]; then
        echo -e "\e[31mWorking tree has tracked-file changes. Commit or stash first:\e[0m"
        echo "$dirty"
        return 1
    fi
    step 2 7 "Working tree clean."

    if ! git remote get-url "$upstream_remote" &>/dev/null; then
        echo -e "\e[31mUpstream remote '$upstream_remote' not configured.\e[0m"
        echo "Run: git remote add $upstream_remote <url>"
        return 1
    fi
    if ! git remote get-url "$origin_remote" &>/dev/null; then
        echo -e "\e[31mOrigin remote '$origin_remote' not configured.\e[0m"
        return 1
    fi
    step 3 7 "Remotes '$origin_remote' / '$upstream_remote' present."

    step 4 7 "Fetching $upstream_remote..."
    git fetch "$upstream_remote"
    step 5 7 "Fetch done."

    local upstream_ref="$upstream_remote/$upstream_branch"
    local ahead_count
    ahead_count="$(git rev-list --count "$branch..$upstream_ref" 2>/dev/null || echo 0)"
    step 6 7 "$upstream_ref is $ahead_count commit(s) ahead of $branch"

    if [ "$ahead_count" -eq 0 ]; then
        echo -e "\e[33m$branch is already up-to-date with $upstream_ref.\e[0m"
        return 0
    fi

    step 7 7 "Rebasing $branch onto $upstream_ref..."
    git checkout "$branch"
    if ! git rebase "$upstream_ref"; then
        echo -e "\e[31mRebase hit conflicts.\e[0m"
        echo "Recovery:"
        echo "  1. Resolve conflicts"
        echo "  2. git add <files>"
        echo "  3. git rebase --continue"
        echo "  4. Re-run this script to push"
        return 1
    fi

    local local_head
    local_head="$(git rev-parse HEAD)"
    if ! git push --force-with-lease "$origin_remote" "$branch"; then
        echo -e "\e[31mPush failed. Local $branch at $local_head\e[0m"
        echo "Run: git push --force-with-lease $origin_remote $branch"
        return 1
    fi

    echo ""
    echo -e "\e[32mDone. $branch rebased and pushed.\e[0m"
    echo "  Local  : $local_head"
    echo "  Remote : $local_head"

    if [ "$current_branch" != "$branch" ]; then
        echo "Switching back to: $current_branch"
        git checkout "$current_branch"
    fi
    return 0
}

# ---- 主循环 ----
while true; do
    show_menu
    read -rp "Select option: " choice

    case "$choice" in
        1) build ;;
        2) invoke_rebase "$REBASE_BRANCH" "$REBASE_UPSTREAM_REMOTE" "$REBASE_UPSTREAM_BRANCH" "$REBASE_ORIGIN_REMOTE" ;;
        0) echo "Bye."; exit 0 ;;
        *) echo -e "\e[31mInvalid option: $choice\e[0m" ;;
    esac

    if [ "$choice" != "0" ]; then
        echo ""
        read -rp "Press Enter to return to menu..."
    fi
done
