# ERPv2 Frontend Delivery Layer

## Overview
This folder contains the deployable frontend bundle served by the backend (`ERP.API`) as static files.

In this repository state, frontend source code is not included here. Instead, compiled assets are published into `wwwroot` and exposed by ASP.NET Core.

## What Is Inside
- Hashed JavaScript/CSS chunks for the client app
- Static media files
- `downloads/` assets used by the application
- `index.html` entrypoint used for SPA routing fallback

## Runtime Behavior
`ERP.API` serves this folder using:
- `UseDefaultFiles()`
- `UseStaticFiles()`
- `MapFallbackToFile("index.html")`

This enables direct navigation to client routes in production.

## Deployment Notes
- No separate Node runtime is required at API runtime
- Deploying backend with Docker also deploys these frontend assets
- Keep bundle and API version aligned when publishing
- For Vercel static hosting, set project root to `src/ERP.API/wwwroot`
- `vercel.json` is included to support SPA route fallback to `index.html`

## How to Update Frontend Assets
1. Build frontend in its source repository/workspace.
2. Copy produced build artifacts into `src/ERP.API/wwwroot`.
3. Ensure `index.html` and chunk references are valid.
4. Commit and deploy backend.

## Environment and API Connectivity
- Frontend should target the same host where `ERP.API` is published, unless proxy/gateway is configured
- CORS and auth settings are managed by backend configuration
- Current bundle API base URL is configured in `chunk-NWVCUJ5O.js` (`apiUrl`)

## Validation Checklist
- Open `/` and confirm UI loads
- Refresh a deep-link route and verify fallback works
- Test authenticated API calls
- Verify file downloads under `/downloads`
