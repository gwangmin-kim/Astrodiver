# Namespace and directory refactor

Branch: `codex/namespace-directory-refactor`

All 158 project scripts now use feature namespaces under `Astrodiver`.
No assembly definitions or gameplay logic changes were introduced.

Directory mapping:
- Core/SaveSystem -> Persistence
- Core/SceneManagement -> SceneManagement
- Core/ExploreSession -> Sessions
- Core/PersistentServicesBootstrap -> Bootstrap/PersistentServicesBootstrap
- CapturableObjects -> Creatures
- Data -> Definitions
- Equipments -> Equipment
- InteractableObjects -> Interaction
- Stage -> Stages
- TutorialGuides -> Tutorials
- Player/Camera -> Player/Cameras (avoids UnityEngine.Camera name collision)

Existing script GUIDs are retained. UpgradeEffect concrete classes carry
MovedFrom mappings from the global namespace in Assembly-CSharp.
Future scripts should use Astrodiver plus their feature folder namespace.
Editor scripts remain in Editor folders and use Astrodiver.Editor namespaces.
Assembly separation and responsibility changes are deferred.

Validation:
- Unity recompilation succeeded, no compilation errors.
- 45 upgrade definitions and 51 effect instances loaded and validated.
- 45 prefabs and 5 scenes inspected: no missing scripts.
- Existing save smoke test failed: expected default creature stack count 10,
  whereas the unchanged GameDataDefaults asset contains 4. This test is not passing.
- PlayMode behavior and player build were not tested.
- No commit or push performed.
