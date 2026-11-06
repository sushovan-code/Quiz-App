---
description: Regenerate architecture diagrams from the current code
---
1. Read the solution, the Aspire AppHost, infra/ Terraform and the message contracts. List every service, data store, queue, cache and external system and how they connect. Show me the list first.
2. After I confirm, update `docs/architecture/workspace.dsl` (Structurizr C4 context and container views) and the Mermaid sequence diagrams for the main flows in `docs/architecture/*.md`.
3. Use the accent-plus-grey style from @docs/standards/ui-and-theming.md, a legend on each view, at most about 12 boxes per view.
4. Render them if the renderers are installed, and report what changed.
