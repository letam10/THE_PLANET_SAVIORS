# THE PLANET SAVIORS - Core Repository

Welcome to the core repository for **The Planet Saviors** (TPS). This project is built in Unity, designed to deliver a deep, inter-connected RPG loop (Shop/Combat/Party/Rest/Travel).

## Indie Benchmark References
We draw heavy inspiration from industry-leading indie titles to guide our design, pacing, and systemic interactions.

### 2D / Pixel Art Foundations
- **Stardew Valley:** Inspiration for our daily loop, systemic resting, and UI minimalism. We map our *Day/Night Cycle* and *Energy/Rest Systems* closely to the player-driven pacing found here.
- **CrossCode:** Guides our *Combat Clarity* and *World Readability*. We aim for fast-paced, highly responsive feedback during encounters, with clear telegraphs and satisfying damage numbers.
- **Terraria:** Influences our approach to *Asset Modularity* and *Item Progression*. Crafting and inventory scaling are designed to be vast but easily navigable.

### 3D / Voxel & Survival Evolution
- **My Time at Portia:** A benchmark for *Quest & Social loops*. How the player interacts with the town, takes commissions, and builds out their base/party.
- **Valheim:** Informs our *World Expansion* and *Risk/Reward Exploration*. The separation of biomes and the consequence of pushing too far from a unified base camp.
- **Dinkum:** Used as a reference for *Systemic UI*—specifically how inventory, toolbelt, and interactions blend seamlessly into the world without overwhelming the player.

## Gameplay Loops & System Mappings
The core of TPS is the systemic integration of the following modules:
1. **World Readability:** Environment layout ensures safe zones (towns, inns) are distinct from danger zones (dungeons, wilds).
2. **Combat Clarity:** Battle logic is strictly decoupled from UI rendering. We utilize Unity's uGUI Canvas (via `RuntimeMenuCanvasController`) to deliver clear turn order, action costs, targeting feedback, and prompt resolutions, bypassing legacy immediate-mode GUI limitations.
3. **UI Loop:** A unified, Canvas-first systemic UI handles everything from Quality/Language Settings to Inventory/Equipment management, ensuring a consistent UX language across all game states.

## Production Workflow & Asset Sourcing
TPS utilizes a rapid, open-source pipeline for content generation:
- **Asset Downloader (`AssetDownloader.cs`):** A custom editor tool leveraging `curl/wget` to fetch CC0/Open Source 3D models, audio (`.wav`, `.ogg`), and textures directly into the Unity project.
- **Attribution & Licensing:** All downloaded assets are automatically cataloged in `SOURCES.md` alongside their origin URLs and licensing tags (e.g., CC-BY, CC0) to ensure compliance.
- **Scalable Worktrees:** Gameplay mechanics are isolated into domains (`TPS.Runtime.Combat`, `TPS.Runtime.Core`, etc.), allowing features to be built and tested safely in separate git branches (e.g., `codex/feature-name`) before integrating via `GameStateManager`.

---
*Built iteratively via Codex and Antigravity AI.*