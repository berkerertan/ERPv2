# StokNet Project Status

Last updated: 2026-05-01

## Repo layout
- Backend/API repo: `C:\Users\User\Documents\ERPv2`
- Frontend/Angular repo: `C:\Users\User\Documents\ERPv2 Angular`
- Azure App Service serves the API.
- Vercel serves the Angular frontend.

## Completed
- Azure SQL + Azure deploy stabilized.
- Demo login works on live backend.
- Offline package download published on backend.
- Production demo-account seeding added.
- Email verification flow exists in Angular and backend.
- Fire/Waste stock movement flow added.
- Open cart / draft cart work was merged earlier.

## Synced full-stack features
- Barcode-assisted inventory counting
- Bulk inventory count apply endpoint
- Inventory count session backend foundation:
  - start session endpoint
  - session list/detail endpoints
  - persisted session items/history
  - stock movement to session linkage
- Angular inventory count session UI:
  - session start
  - open-session list
  - resume active session
  - session-aware count apply
- Inventory count history improvements:
  - difference summary
  - completed-session history
  - session detail modal
  - item-level location + counted-by trace
- Purchase recommendations endpoint and UI integration
- Notification center backed by `/api/notifications`
- Buyer risk summary notifications via `/api/cari-accounts/buyers/risk-summary`
- Landing/demo CTA alignment
- Demo login without exposing credentials in inputs

## Known repo split rule
- Do not treat `src/ERP.API/wwwroot` as the primary frontend source.
- Real frontend source is the Angular repo.
- `wwwroot` is only a built/static fallback copy on the API side.

## Current staged roadmap
1. Inventory count sessions backend foundation
2. Inventory count session UI, session resume flow
3. Inventory count history + difference summary
4. Shelf/location visibility + per-user count trace
5. Mobile counting UX tightening

## Next backlog
1. Buyer list risk badges/columns
2. Approval timeline UI details
3. Supplier lead-time aware purchase recommendations
4. Multi-supplier recommendation split into separate drafts
5. More robust mobile barcode UX and repeated scan batching

## Current caution
- Angular repo contains `src/app/features/auth/verify-email/` and it is in use.
- Keep backend and Angular changes aligned before the next push.
