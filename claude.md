## Project Memory & Changelog

Claude Code must maintain two files at the project root at all times:

### PROJECT_MEMORY.md
This file is the source of truth for what has been built. Update it after
every completed feature or significant change. It must always contain:

- Current project structure (directory tree)
- All implemented endpoints with HTTP verb, route, auth requirement, and status
- All MongoDB collections and their current fields
- All services and their public methods
- All dependencies and NuGet packages in use
- Any known issues or pending decisions
- Seed data credentials

Claude Code must READ this file at the start of every new session before
doing anything, and UPDATE it at the end of every completed task.

### CHANGELOG.md
Append an entry every time a feature, fix, or change is completed.
Format:

## [YYYY-MM-DD] — Short title
- What was built or changed
- Files created or modified
- Tests added
- Any deviations from the original plan and why

---

## Session Start Checklist
At the beginning of every new Claude Code session, before writing any code:
1. Read CLAUDE.md (this file)
2. Read PROJECT_MEMORY.md — understand what is already built
3. Read CHANGELOG.md — understand the recent history
4. Confirm understanding with a short summary before proceeding