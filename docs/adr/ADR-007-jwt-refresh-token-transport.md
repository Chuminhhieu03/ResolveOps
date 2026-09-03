# ADR 007: JWT Refresh Token Transport

## Status
Accepted

## Context
In a modular monolith architecture with a decoupled frontend (e.g., SPA/React), secure transport of authentication tokens is critical. We use JWTs for short-lived authorization and refresh tokens for rotating access without requiring frequent re-authentication.

We needed to decide how to deliver and store the refresh token to minimize the risk of XSS (Cross-Site Scripting) and CSRF (Cross-Site Request Forgery) attacks.

## Decision
We will use **HttpOnly, SameSite=Strict, Secure cookies** to transport the refresh token, while returning the short-lived access token in the JSON response body.

## Consequences
### Positive
- **Mitigates XSS**: The refresh token cannot be accessed by JavaScript (HttpOnly). Even if the SPA is compromised via XSS, the attacker cannot steal the refresh token.
- **Mitigates CSRF**: The SameSite=Strict attribute prevents the browser from sending the cookie in cross-origin requests.

### Negative
- **API and Client coupling**: The backend must reside on the same site/domain (or a subdomain with SameSite=Lax) as the frontend to allow cookie delivery.
- **Client implementation complexity**: The frontend SPA must explicitly configure its HTTP client (e.g., fetch, axios) to include credentials (`credentials: 'include'`) for the `/auth/refresh` endpoint.
