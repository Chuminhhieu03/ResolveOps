# ADR-005 — MinIO for Evidence Object Storage

- **Date:** 2026-08-21
- **Status:** Accepted

---

## Context

ResolveOps must store and retrieve evidence documents (PODs, damage photos, claim packages). These files can be large and should not be stored in the relational database.

## Decision

Use **MinIO** for object storage.
- Client: `AWSSDK.S3` (.NET client).
- MinIO is 100% S3-API compatible.

## Alternatives Considered

| Alternative | Reason Not Chosen |
|---|---|
| Azure Blob Storage | Proprietary, Azure credit exhausted. |
| AWS S3 | Requires cloud account and costs. MinIO provides local and self-hosted S3 compatibility for free. |
| SQL Server `VARBINARY(MAX)` | Bloats database size, degrades backup/restore performance, and complicates streaming. |
| Local File System | Does not scale horizontally, complicates containerization and backup strategies. |

## Consequences

### Positive
- Open-source, Docker-native, free.
- S3 compatibility means we can swap to AWS S3 or other S3-compatible cloud storage in the future with zero code changes.
- Supports pre-signed URLs for secure, short-lived upload/download access.

### Negative / Trade-offs
- Requires managing an additional container in the stack.

## References

- Master Specification Section 13.3
