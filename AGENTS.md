# AGENTS.md

## Project overview
- This repository is a Unity game project.
- Main language: C#
- Explain all results in Korean.

## Safety rules
- Do not modify Scene, Prefab, ProjectSettings, Packages, or Animator-related asset files unless explicitly requested.
- Prefer editing only .cs files.
- Do not rename files, classes, namespaces, or folders unless explicitly requested.
- Do not change input mappings unless explicitly requested.

## Workflow
- For any non-trivial task, analyze first and make a short plan before editing.
- Before editing, list which files you intend to modify.
- After editing, explain:
  1. what changed
  2. why it changed
  3. how to test it in Unity

## Code style
- Keep the existing naming conventions.
- Keep public fields minimal unless Unity Inspector setup requires them.
- Avoid broad refactoring unless specifically requested.

## Done criteria
- The code should compile without obvious syntax issues.
- Existing gameplay behavior should remain unchanged except for the requested fix.
- Provide manual Unity test steps after each task.