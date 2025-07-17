# Infonetica - Configurable Workflow Engine (State Machine API)

This is a minimal backend API built with .NET 8 that lets clients:

- Define workflows (as state machines)
- Start workflow instances from definitions
- Execute actions to move between states with validation
- Query definitions and instances

---

## Quick Start

### Prerequisites

- .NET 8 SDK installed
- Linux/macOS/Windows terminal

### Run the Server

```bash
dotnet run
```

### 1. Create Workflow Definition
- POST /workflow-definitions
Request body:
{
  "id": "approval-workflow",
  "name": "Approval Workflow",
  "states": [
    {
      "id": "draft", "name": "Draft", "isInitial": true, "isFinal": false, "enabled": true
    },
    {
      "id": "approved", "name": "Approved", "isInitial": false, "isFinal": true, "enabled": true
    }
  ],
  "actions": [
    {
      "id": "approve", "name": "Approve", "enabled": true,
      "fromStates": ["draft"], "toState": "approved"
    }
  ]
}
### 2. Get Workflow Definition
- GET /workflow-definitions/{id}
### 3. Start New Instance
- POST /workflow-instances
Request body:
{
  "definitionId": "approval-workflow"
}
### 4. Execute Action
- POST /workflow-instances/{instanceId}/execute
Request body:
{
  "actionId": "approve"
}
### 5. Get Instance Status
- GET /workflow-instances/{instanceId}
  
### Assumptions & Shortcuts
- Uses in-memory storage (resets on restart)

- No authentication

- No concurrency control

- Minimal API style used (no controllers)

## Author: Pracheta Saha (Roll No.: 22CS30042)
