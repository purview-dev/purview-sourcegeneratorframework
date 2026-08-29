# Agent Instructions

This repository contains custom build rules, analysers, and source generators. Agents working in this codebase should follow the guidelines below.

## Warnings and Suggestions

Do **not** suppress or mute compiler, analyser, or build warnings by adding `<NoWarn>` entries, `#pragma warning disable`, or similar directives in code or project files without explicit user direction. Warnings and suggestions are the responsibility of the developer/user to evaluate and mute. If a warning is raised, surface it to the user and let them decide whether to suppress it.
