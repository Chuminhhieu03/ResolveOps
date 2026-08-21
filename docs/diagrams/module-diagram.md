# Module Dependency Diagram

> This diagram shows the directed dependencies between vertical modules in the monolith.
> Communication flows left-to-right via synchronous C# abstractions, or decoupled via Integration Events.

```mermaid
flowchart LR
    subgraph Foundation
        Identity
        Tenancy
    end
    
    subgraph Core Data
        Partners
        Shipments
    end
    
    subgraph Event Stream
        Tracking
    end
    
    subgraph Operational Workflow
        Exceptions
        Workflow[Workflow Tasks & SLA]
        Documents[Evidence & Documents]
    end
    
    subgraph Financial Recovery
        Claims
    end
    
    subgraph Cross-cutting
        Notifications
        Integrations
        Reporting
        Audit
    end

    %% Foundational dependencies
    Tenancy --> Identity
    
    %% Core dependencies
    Partners -.-> Tenancy
    Shipments -.-> Tenancy
    Shipments -.-> Partners
    
    %% Tracking depends on Shipment alias
    Tracking -.-> Shipments
    
    %% Operational workflow dependencies
    Exceptions -.-> Shipments
    Exceptions -.-> Tracking
    Workflow -.-> Exceptions
    Documents -.-> Exceptions
    
    %% Financial recovery dependencies
    Claims -.-> Exceptions
    Claims -.-> Documents
    Claims -.-> Workflow
    
    %% Cross-cutting observers
    Reporting -.-> Shipments
    Reporting -.-> Exceptions
    Reporting -.-> Claims
    
    %% Event flows (decoupled)
    Shipments ==> |ShipmentCreatedEvent| Exceptions
    Tracking ==> |TrackingEventReceived| Exceptions
    Exceptions ==> |ExceptionDetectedEvent| Workflow
    Exceptions ==> |ExceptionDetectedEvent| Notifications
    Workflow ==> |SlaBreachedEvent| Notifications
    Claims ==> |ClaimApprovedEvent| Reporting
    
    classDef foundation fill:#e1f5fe,stroke:#0288d1,stroke-width:2px;
    classDef core fill:#fff9c4,stroke:#fbc02d,stroke-width:2px;
    classDef events fill:#e8f5e9,stroke:#388e3c,stroke-width:2px;
    classDef workflow fill:#fce4ec,stroke:#c2185b,stroke-width:2px;
    classDef financial fill:#ede7f6,stroke:#512da8,stroke-width:2px;
    classDef crosscutting fill:#f5f5f5,stroke:#616161,stroke-width:2px;
    
    class Identity,Tenancy foundation;
    class Partners,Shipments core;
    class Tracking events;
    class Exceptions,Workflow,Documents workflow;
    class Claims financial;
    class Notifications,Integrations,Reporting,Audit crosscutting;
```
