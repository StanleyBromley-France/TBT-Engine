# unity_adapter

```mermaid
classDiagram

    class UnityGameController {
        +IEngineFacade Engine
        +void Start()
        +void Update()
        +void OnPlayerCommand(ActionChoice action)
    }

    UnityGameController ..> IEngineFacade

```
