#!/bin/bash
set -e

echo "🔧 Claude Code Development Container Initialization"
echo "=================================================="

# 1. Git configuration
echo "✓ Configuring Git..."
mkdir -p "$(dirname "$GIT_CONFIG_GLOBAL")"
touch "$GIT_CONFIG_GLOBAL"
git config --global --add safe.directory "${containerWorkspaceFolder:-.}"

# 2. Claude Code runtime directory
echo "✓ Setting up Claude Code runtime..."
mkdir -p "$CLAUDE_RUNTIME_DIR"
chmod 755 "$CLAUDE_RUNTIME_DIR"

# 3. Project-local Claude configuration
echo "✓ Configuring project-local Claude settings..."
mkdir -p "$CLAUDE_PROJECT_CONFIG"
chmod 755 "$CLAUDE_PROJECT_CONFIG"

# 4. Agents configuration
echo "✓ Setting up agents configuration..."
mkdir -p "$CLAUDE_AGENTS_CONFIG"
chmod 755 "$CLAUDE_AGENTS_CONFIG"

# 5. User home Claude configuration (from Windows mount, if available)
echo "✓ Linking user-level Claude configuration..."
if [ -d "/mnt/windows-user/.claude" ]; then
    mkdir -p "$HOME/.claude"
    # Copy Windows user settings if they exist and we don't have project overrides
    if [ -f "/mnt/windows-user/.claude/settings.json" ] && [ ! -f "$HOME/.claude/settings.json" ]; then
        cp "/mnt/windows-user/.claude/settings.json" "$HOME/.claude/settings.json"
        echo "  • Copied Windows user settings.json"
    fi
    if [ -d "/mnt/windows-user/.claude/skills" ]; then
        cp -r "/mnt/windows-user/.claude/skills" "$HOME/.claude/" 2>/dev/null || true
        echo "  • Synced Windows user skills"
    fi
fi

if [ -d "/mnt/windows-user/.agents" ]; then
    mkdir -p "$HOME/.agents"
    if [ -d "/mnt/windows-user/.agents/skills" ]; then
        cp -r "/mnt/windows-user/.agents/skills" "$HOME/.agents/" 2>/dev/null || true
        echo "  • Synced Windows user agents"
    fi
fi

# 6. Make project .claude and .agents accessible to VSCode
echo "✓ Configuring project accessibility..."
# Create symlinks for IDE access if running in VSCode
if [ -n "$TERM_PROGRAM" ] || [ "$REMOTE_CONTAINERS" = "true" ]; then
    mkdir -p "$HOME/.config/code-server"
    # Ensure project config is discoverable
    ln -sf "$CLAUDE_PROJECT_CONFIG" "$HOME/.claude/project-config" 2>/dev/null || true
    ln -sf "$CLAUDE_AGENTS_CONFIG" "$HOME/.agents/project-config" 2>/dev/null || true
fi

# 7. Verify setup
echo ""
echo "📁 Configuration Status:"
echo "  • Claude Runtime: $CLAUDE_RUNTIME_DIR"
echo "  • User Config: $HOME/.claude"
echo "  • Project Config: $CLAUDE_PROJECT_CONFIG"
echo "  • Project Agents: $CLAUDE_AGENTS_CONFIG"
echo ""
echo "✨ Container initialized successfully!"
echo ""
echo "Next steps:"
echo "  1. Run: cd /workspaces/interlocutor"
echo "  2. Run: /reload-skills (in Claude Code to reload project skills)"
echo "  3. Verify skills are loaded with 'list skills'"
