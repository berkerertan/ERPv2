# StokNet Project Status

Last updated: 2026-05-03

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
- Inventory count sessions and history
- Purchase recommendations endpoint and UI integration
- Buyer risk summary notifications and buyer risk screen
- Purchase/sales order approval history and rejection flow
- Mobile counting UX tightening
- Purchase recommendations 2.0:
  - supplier lead-time aware target stock calculation
  - supplier lead-time field on supplier records
  - supplier grouping in recommendation output
  - split recommendations into multiple supplier draft orders
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
5. Approval history + rejection flow
6. Mobile counting UX tightening
7. Purchase recommendations 2.0

## Next backlog
1. Better multi-supplier recommendation refinement rules
2. More robust mobile barcode batching
3. Deeper approval timeline UI polish
4. Offline queue + later sync for counting

## Current caution
- Angular repo contains `src/app/features/auth/verify-email/` and it is in use.
- Keep backend and Angular changes aligned before the next push.


