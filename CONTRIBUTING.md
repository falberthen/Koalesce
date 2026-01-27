# Contributing to Koalesce

Thank you for your interest in contributing!

## Getting Started

1. Fork the repository
2. Create a feature branch from `master`
3. Make your changes
4. Submit a Pull Request

## Pull Request Guidelines

- **One concern per PR** - Keep PRs focused and atomic
- **Write tests** - New features and bug fixes should include tests
- **Follow code style** - Match existing patterns in the codebase
- **Update documentation** - If your change affects public APIs

## Branch Naming

- `feat/description` - New features
- `fix/description` - Bug fixes
- `refactor/description` - Code restructuring without behavior changes
- `chore/description` - Maintenance tasks (dependencies, CI, etc.)
- `docs/description` - Documentation only

## Commit Messages

Follow [Conventional Commits](https://www.conventionalcommits.org/):

```
feat: add support for OpenAPI 3.1
fix: resolve schema conflict in nested objects
refactor: reorganize test project structure
docs: update README with new examples
```

## Code Review

All PRs require review before merging. Please be patient and responsive to feedback.

## Releases

Package releases to NuGet are managed exclusively by the maintainer.
Do not bump version numbers in PRs.
