# Manager API + Front (one container)

## Run Manager front
npm run dev (in web folder)
   ```cmd
   cd Manager/Web
   npm run dev
   ```
## Dev run:
   - **API**: .NET Project run config → `App.Api`.
   - **Web**: npm run config → `dev` in `Manager/Web`.
## Docker (prod-like, 1 container):
   ```bash
   docker build -t manager:latest .
   docker run --rm -p 8080:8080 manager:latest
   # Front: http://localhost:8080  |  API: /api/hello  |  /health
   ```
