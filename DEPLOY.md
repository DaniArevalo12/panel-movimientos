# Despliegue gratuito en Render

La app es una Blazor Web App en modo **Interactive Server**: mantiene una conexión SignalR persistente con el servidor, así que necesita un host que ejecute .NET real (no sirve un hosting estático de archivos). Render ofrece un plan free de "Web Service" con contenedores Docker, que es lo que usan los `Dockerfile` y el `render.yaml` ya incluidos en este repo.

El repo despliega **dos servicios** (ambos en el plan free):

1. `pruebatecnica-mockapi` — la API REST simulada.
2. `pruebatecnica-web` — la aplicación Blazor, configurada para consumir el servicio anterior.

## Pasos (requieren tu cuenta, no se pueden automatizar desde aquí)

1. **Sube este repositorio a GitHub** (repo público o privado):
   ```bash
   git remote add origin https://github.com/<tu-usuario>/<tu-repo>.git
   git push -u origin main
   ```
2. Entra a [render.com](https://render.com) y crea una cuenta gratuita (no requiere tarjeta).
3. En el dashboard: **New → Blueprint**, selecciona el repositorio que acabas de subir. Render detecta `render.yaml` automáticamente y propone crear los dos servicios (`pruebatecnica-mockapi` y `pruebatecnica-web`) en el plan free.
4. Aplica el blueprint. El primer build tarda unos minutos (compila ambos `Dockerfile`).
5. Una vez desplegado `pruebatecnica-mockapi`, copia su URL pública (`https://pruebatecnica-mockapi-XXXX.onrender.com`, Render añade un sufijo si el nombre ya estaba tomado).
6. En el servicio `pruebatecnica-web` → **Environment**, ajusta la variable `ApiSettings__BaseUrl` con esa URL exacta (el valor en `render.yaml` es solo un valor por defecto optimista) y vuelve a desplegar.
7. Abre la URL de `pruebatecnica-web` — esa es la URL pública final para compartir.

## Notas

- El plan free de Render "duerme" un servicio tras ~15 min de inactividad; la primera petición tras estar dormido tarda 30-60 s en responder (cold start). Es normal.
- No se necesita HTTPS manual: Render lo provee automáticamente en el borde. `Program.cs` detecta la variable de entorno `PORT` (que Render inyecta) y desactiva `UseHttpsRedirection` en ese escenario para evitar bucles de redirección detrás del proxy.
- Cuando tengas la URL real de la API de la prueba técnica, puedes apuntar `ApiSettings__BaseUrl` directamente a ella y prescindir de `pruebatecnica-mockapi`.
