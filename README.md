# THE_PLANET_SAVIORS (TPS)

![Status](https://img.shields.io/badge/Status-In%20Development-yellow)
![Unity](https://img.shields.io/badge/Unity-6000.4.0f1-lightgrey)
![Architecture](https://img.shields.io/badge/Architecture-Event--Driven%20Systemic-blue)

**The Planet Saviors** (TPS) is a systems-driven RPG prototype developed in Unity. The core objective is delivering a deep, inter-connected gameplay loop that cycles through **Exploration, Combat, Trading, Party Management, and Rest**.

## 📖 Design Vision & Benchmarks

We draw heavy inspiration from industry-leading indie titles to guide our design, pacing, and systemic interactions:

### Systemic Flow & Minimalism
- **Stardew Valley**: Inspires our daily loop, systemic resting, and UI minimalism. The *Day/Night Cycle* and *Energy/Rest Systems* are directly informed by its pacing.
- **Dinkum**: Serves as a reference for *Systemic UI*—how inventory, trading, and system settings blend seamlessly into the runtime without overwhelming the player.

### Combat & Action Clarity
- **CrossCode**: Guides our *Combat Clarity* and *World Readability*. We aim for high-response feedback during encounters with clear telegraphs, action costs, and distinct turn orders.

### Modularity & World Expansion
- **Terraria**: Influences our *Asset Modularity* and *Item Progression*.
- **Valheim**: Informs our *Risk/Reward Exploration* separation between safe havens (Towns/Inns) and hostile perimeters (Dungeons).

---

## 🏗 Systemic Architecture

TPS uses an event-driven, decoupled architectural pattern designed for scalability and strict separation of concerns.

### 1. Gameplay Loop Decoupling
The core modules (Shop, Combat, Party, Rest, Travel) operate fully integrated *and* in sandbox isolation.
- **Combat Bridge**: Battle logic (`BattleWorldBridge`) is strictly decoupled from UI rendering. It communicates exclusively via C# Actions/Delegates and EventBus.
- **Runtime Systemic UI**: A robust uGUI system (`RuntimeMenuCanvasController`) manages all state-driven interfaces (Inventory, Equipment, System Settings, Shop) using an uncoupled state-refresh pattern (No `OnGUI`, no `Update` polling drops).

### 2. Data-Driven Scaffolding
- **ScriptableObjects (SO)**: Serve solely as immutable data templates (Items, Encounters, Dialogues).
- **Runtime POCOs**: Mutable state (Inventory quantities, Combat HP, Quest progress) is pushed to isolated `SaveData` structures managed by the `GameStateManager`.

### 3. Procedural World Generation Tools
Included inside the Editor context (`TPS.Editor`):
- **SceneExpansionTool**: A procedural Multi-Layer Scaffolding injector designed to safely expand environment bounds (Base, Mid Walkways, High Towers) across all Settlement and Dungeon scenes seamlessly without breaking binary serialization.

---

## 🛠 Production Workflow & Mandatory Rules

TPS utilizes a strict, modular pipeline for code integration, asset acquisition, and agent-driven development.

### 1. Mandatory Development Workflow
- **Planning First:** Any non-trivial task requires a markdown planning table (Goal, Files to change, Risks, Validation, Commit message) prior to execution, waiting for user approval.
- **Codex Checkpoint Model:** Development must occur on `codex/*` branches. Work is broken into small, logical checkpoints. 
- **Commit & Integrity:** 
  - Validate code (zero compiler errors) before any commit. 
  - **CRITICAL:** Always stage corresponding `.meta` files alongside modified assets/scripts.
  - **NEVER** commit `.unity` or `.prefab` files if only C# scripts were modified, to prevent accidental serialization conflicts.

### 2. Architecture Constraints
- **Project Structure & Namespaces:** Code must reside under `Assets/_TPS/Scripts/...` and use the `TPS.[Module]` namespace convention (e.g., `TPS.Runtime.Core`). Do not edit `Library`, `PackageCache`, etc.
- **Data vs State:** `ScriptableObjects` (SOs) are strictly templates (Read-Only at runtime). True runtime states reside in POCO classes managed via `GameStateManager`.
- **Event Architecture:** `GameEventBus` is strictly for cross-domain/macro events (e.g., `OnHourChanged`). Local mechanics should use explicit direct references (`[SerializeField]`) or C# Actions.

### 3. Asset Management
- **Asset Downloader (`AssetDownloader.cs`)**: Custom Editor tool to fetch CC0/Open Source assets. Must catalog in `SOURCES.md`.

---

## 🗺 Quick Start

1. Open `Assets/_TPS/Scenes/Bootstrap.unity` (The central initializer).
2. Press Play. The `Bootstrap` handles persistent dependency injection and safely loads into the `World_Demo` or town scenes.
3. Use the `Tab` key to toggle the Cursor and Systemic UI.
4. Interact using `E`, toggle menus with `I`, `K`, `C`, `J`, `P`.

*Built iteratively via Codex and Antigravity AI.*