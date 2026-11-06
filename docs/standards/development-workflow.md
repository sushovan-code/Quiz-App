# Development workflow

Work happens in two stages. Stage 2 is optional per change.

## Stage 1: local, tested, merged

1. **Plan**: in plan mode, propose the slice (endpoints, UI, data, tests). Wait for approval.
2. **Branch**: `feature/<issue-number>-<short-kebab-description>` (or `fix/`, `chore/`) from `main`.
3. **Build it to work locally end to end**: `aspire run` starts the API, frontend and every local dependency (SQL Server, Postgres with pgvector, Redis, Azurite, Service Bus emulator, Keycloak, Seq as the repo needs). Cloud-only services (Auth0, LLM APIs, Atlas) read their values from user secrets.
4. **Add tests** once the slice works: backend unit and integration tests, frontend unit tests (see `testing.md`). All must pass locally.
5. **Self-review**: run `/prepare-pr`; fix what it finds.
6. **Pull request** to `main` (trunk-based, short-lived branches). CI runs build, tests, lint and security scans on every PR; this needs no cloud resources, so it always runs.
7. **Merge** with squash once checks are green.

### Definition of done (stage 1)
- [ ] Runs end to end locally with `aspire run`, in light and dark themes
- [ ] Unit and integration tests for backend and frontend, all passing
- [ ] No new warnings; `dotnet format` and lint clean
- [ ] README and docs updated (endpoints, screenshots, ADR if a decision was made)
- [ ] No secrets in the diff

## Stage 2 (optional): deploy

Deployment workflows are manual (`workflow_dispatch`), so skipping stage 2 never blocks a merge.
- Infrastructure: `platform-infra` Terraform (`infra-plan`, `infra-up`, `infra-down`).
- App: the repo's `deploy.yml` builds the image, pushes to GHCR and deploys to the chosen environment.
- Use `/deploy-plan` to have Claude prepare the change; it never applies infrastructure itself.
