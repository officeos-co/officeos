#!/usr/bin/env bash
# Sync ALL skill packages to their public GitHub repos under officeos-co.
# Auto-discovers skills from packages/skills/*/skill.ts.
# Auto-creates the public repo if it doesn't exist yet.
#
# Usage:
#   ./scripts/sync-skill-repos.sh              # sync all skills
#   ./scripts/sync-skill-repos.sh github       # sync just one

set -euo pipefail

ORG="officeos-co"
SKILLS_DIR="$(cd "$(dirname "$0")/../packages/skills" && pwd)"
WORK="$(mktemp -d)"
trap 'rm -rf "$WORK"' EXIT

# Auto-discover all skills that have a skill.ts
if [ $# -gt 0 ]; then
  SKILLS=("$@")
else
  SKILLS=()
  for dir in "$SKILLS_DIR"/*/; do
    [ -f "$dir/skill.ts" ] && SKILLS+=("$(basename "$dir")")
  done
fi

echo "Skills to sync: ${SKILLS[*]}"

for skill in "${SKILLS[@]}"; do
  echo "--- $skill ---"
  REPO="skill-$skill"
  SRC="$SKILLS_DIR/$skill"

  if [ ! -f "$SRC/skill.ts" ]; then
    echo "  SKIP: no skill.ts in $SRC"
    continue
  fi

  # Create the public repo if it doesn't exist
  if ! gh repo view "$ORG/$REPO" &>/dev/null; then
    echo "  Creating repo $ORG/$REPO..."
    # Extract description from skill.ts if possible
    DESC=$(node -e "console.log(JSON.parse(require('fs').readFileSync('$SRC/skill.json','utf-8')).description)" 2>/dev/null || echo "OfficeOS skill: $skill")
    gh repo create "$ORG/$REPO" --public --description "$DESC" --license mit
    sleep 1
  fi

  # Clone the public repo
  if [ -n "${GH_TOKEN:-}" ]; then
    git clone --depth=1 "https://x-access-token:${GH_TOKEN}@github.com/$ORG/$REPO.git" "$WORK/$REPO" 2>/dev/null
  else
    git clone --depth=1 "https://github.com/$ORG/$REPO.git" "$WORK/$REPO" 2>/dev/null
  fi

  # Copy all skill files except node_modules, lockfiles, and build artifacts
  rsync -a --delete \
    --exclude='node_modules' \
    --exclude='package-lock.json' \
    --exclude='dist' \
    --exclude='.git' \
    "$SRC/" "$WORK/$REPO/src/"

  # Move key files to repo root for discoverability, keep src/ as well
  for f in skill.ts skill.json SKILL.md README.md CHANGELOG.md LICENSE icon.svg icon.png; do
    [ -f "$SRC/$f" ] && cp "$SRC/$f" "$WORK/$REPO/$f"
  done

  # Generate package.json for the public repo if it doesn't exist there
  if [ ! -f "$WORK/$REPO/package.json" ] || ! grep -q "@officeos" "$WORK/$REPO/package.json"; then
    cat > "$WORK/$REPO/package.json" <<PKGJSON
{
  "name": "@officeos/skill-$skill",
  "version": "0.1.0",
  "type": "module",
  "main": "skill.ts",
  "repository": {
    "type": "git",
    "url": "https://github.com/$ORG/$REPO"
  },
  "license": "MIT",
  "dependencies": {
    "@officeos/skill-sdk": "*"
  }
}
PKGJSON
  fi


  # Check for changes
  cd "$WORK/$REPO"
  git config user.name "officeos-bot"
  git config user.email "bot@officeos.co"

  git add -A
  if git diff --cached --quiet; then
    echo "  No changes"
    cd - > /dev/null
    continue
  fi

  HASH="${GITHUB_SHA:-$(cd "$SKILLS_DIR" && git rev-parse --short HEAD 2>/dev/null || echo "local")}"
  git commit -m "sync: update from monorepo ($HASH)"
  git push origin main
  echo "  Pushed"
  cd - > /dev/null
done

echo "Done. Synced ${#SKILLS[@]} skill(s)."
