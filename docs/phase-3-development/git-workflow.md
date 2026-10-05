# Git workflow

> **Status:** Checked against the local code; team review pending
>
> **Owner:** HTS / HowToSoftware maintainers
>
> **Last updated:** 2026-10-05

Repository: `HowTo-Software/howtoosoftware-hosting-prototype`. The local redesign branch is `feat/hosting-frontend-redesign`; the recorded baseline is `a7ac383`. That record does not guarantee the remote still points to this commit.

Before publishing the redesign on October 5, 2026, the branch was fast-forwarded to `277e7b1`, incorporating the latest main dependency updates. Release validation uses those dependency versions; `a7ac383` remains the historical starting point.

## Inspect and preserve

```powershell
git status --short
git branch --show-current
git remote -v
git fetch origin
git log --oneline --decorate -8
```

Review changes before switching bases. When necessary, create a local commit or stash including untracked files; choose a name describing the work. Do not use reset/clean to “synchronize” a checkout with pending work.

During redesign, `preserve-local-before-hosting-redesign-2026-10-04` was kept as a backup after application. Do not remove it before verifying its content is preserved in commits.

## New change

With the working tree preserved and main free of local modifications:

```powershell
git switch main
git pull --ff-only origin main
git switch -c feat/change-name
```

Review `git diff`, stage selected files, and commit with the problem, behavior, validation, and configuration described. Never include `.env`, dumps, or artifacts containing personal data.

## Publishing and PR

When the change is ready to share:

```powershell
git push -u origin feat/change-name
```

Open a PR to main with scope and evidence. A branch needs published commits for GitHub to compare its content. With no commit difference from the base, there is nothing to merge.

Merging into main triggers the pipeline; production approval, if configured, belongs to GitHub's Environment. Creating/pushing a feature branch is separate from deployment. These commands are instructions, not actions executed by the documentation review.

## Conflicts

Fetch and review base changes. Resolve conflicts while preserving commercial rules, resources, and migrations. Rerun relevant checks after resolution. Avoid force pushes to shared branches; agree on merge/rebase strategy with maintainers.
