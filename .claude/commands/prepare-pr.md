---
description: Self-review the branch and draft the pull request
---
1. Run `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, and the frontend lint, build and tests. Fix anything that fails.
2. Use the security-reviewer and ui-reviewer subagents on the diff versus main; fix confirmed issues.
3. Check the code review checklist in @docs/standards/coding-standards.md and the definition of done in @docs/standards/development-workflow.md, and list any unmet item.
4. Draft a PR title (Conventional Commits) and description: summary, screenshots needed, how it was tested, risks. Do not push or open the PR; show me the draft.
