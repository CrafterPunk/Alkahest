# FriendsLoop — Template multiplayer Unity + Steam

Plantilla base reutilizable para prototipos cooperativos ("Friends Loops") publicables en Steam.
Sesiones creadas por jugadores, un jugador es host (listen server), amigos se unen vía Steam,
conectividad P2P protegida por el relay de Valve (SteamNetworkingSockets / SDR). Sin servidores dedicados.

> **Estado**: `v0.1-steam-verified` · Unity 6000.5.7f1 · URP · verificado con host y cliente reales
> en 2 PCs / 2 cuentas de Steam (App ID 480), múltiples sesiones. Ver `docs/INCIDENTES.md`.

**Este repositorio es un MOLDE, no un lugar de desarrollo.** Cada juego nuevo nace como
repositorio independiente con "Use this template" en GitHub; aquí solo se congela infraestructura.

## Arquitectura

```
Assets/FriendsLoop/
  Platform/    SteamBootstrap        ← ÚNICA capa que inicializa/apaga la Steam API
  Session/     SteamLobbyService     ← lobbies (crear / unirse / invitar), metadata FL_GAME / FL_HOST
  Networking/  SessionCoordinator    ← FACHADA PÚBLICA: StartHost / JoinLocal / JoinSteamLobby / Disconnect
               TransportMode         ← LocalLoopback (desarrollo) ↔ Steam (juego real)
  Voice/       IVoiceService         ← seam de voz (stub NullVoiceService, sin proveedor todavía)
  DemoTest/    escena técnica        ← BORRABLE: player, HUD, cubo compartido; no sostiene la infraestructura
  Editor/      herramientas          ← genera escena demo, build, steam_appid.txt automático
Packages/com.community.netcode.transport.steamnetworkingsockets/   ← transporte Steam VENDORIZADO (es código nuestro)
```

Regla de dependencia: el gameplay futuro habla **solo** con `SessionCoordinator` (y eventos de
`SessionEvents`). Nada fuera de `Platform/` y `Session/` debe hacer `using Steamworks;`.

## Dependencias

| Paquete | Versión | Origen |
|---|---|---|
| com.unity.netcode.gameobjects (NGO) | 2.13.1 | registro Unity |
| com.rlabrecque.steamworks.net | 2025.164.1 | git URL en `Packages/manifest.json` |
| Transporte SteamNetworkingSockets p/ NGO | 1.0.1 | **vendorizado** en `Packages/` |
| Unity Transport (loopback local) | dependencia de NGO | automático |

Decisiones registradas: NGO (first-party, mantenimiento mensual, target Unity 6) sobre Mirror/FishNet;
Steamworks.NET (SDK 1.64, activo) sobre Facepunch (SDK 1.61, transporte NGO abandonado);
client-host clásico gratis sobre Distributed Authority (de pago, cloud); sin Heathen (capa comercial innecesaria).

## Cómo arrancar

1. Abrir el proyecto en Unity 6000.5+. Los paquetes se resuelven solos (necesita red la primera vez).
2. `steam_appid.txt` con `480` (Spacewar, App ID de desarrollo de Valve) se crea solo en la raíz del
   proyecto al abrir el editor. **Steam debe estar corriendo y con sesión iniciada.**
3. Menú **FriendsLoop → 1. Generar escena demo** (idempotente) si la escena no existe.
4. Play en `FL_DemoScene`.

## Cómo probar host/cliente

**Desarrollo diario (una sola PC, sin Steam):** transporte LocalLoopback.
- Instancia A (editor): HUD → **Host (Local)**.
- Instancia B (build: FriendsLoop → 2. Build demo Windows): **Unirse (Local)**.
- Ambas ven 2 cápsulas con nombre; "Alternar cubo" (o tecla **E** cerca del cubo) cambia el cubo
  verde/rojo y elevado/bajado **en las dos instancias a la vez** (estado autoritativo del host).
- Nota: dos instancias en la misma PC no pueden usar el transporte Steam (una sola sesión de Steam por máquina).

**Prueba real Steam (dos PCs, dos cuentas):**
- Host: HUD → **Host (Steam)** → aparece el **ID de lobby** en el HUD (y botón "Invitar amigos").
- Cliente: pegar el ID → **Unirse (Steam)**, o aceptar la invitación / "Unirse a partida" desde la lista de amigos de Steam.

### Qué necesita el probador externo (NO necesita Unity)

1. La carpeta `Builds/FriendsLoopDemo/` completa (incluye `steam_appid.txt` — imprescindible).
2. Steam instalado, corriendo y con sesión iniciada (cualquier cuenta válida; con App 480 no hace falta poseer ningún juego).
3. Ser amigo del host en Steam si se usa invitación (con ID de lobby pegado no hace falta).

Instrucciones ultracortas —
**Host:** abrir Steam → abrir `FriendsLoopDemo.exe` → `Host (Steam)` → pasar el ID de lobby al otro jugador.
**Cliente:** abrir Steam → abrir `FriendsLoopDemo.exe` → pegar ID → `Unirse (Steam)`.

## Dónde agregar gameplay

- Nuevas escenas/sistemas: crear carpetas hermanas de `DemoTest/` (p. ej. `Game/`) dentro del asmdef
  `FriendsLoop.Runtime`, o un asmdef propio que referencie `FriendsLoop.Runtime`.
- Copiar el patrón de `FL_Network` (NetworkManager + transportes + SessionCoordinator + SteamBootstrap +
  SteamLobbyService) a tu escena, o mover ese objeto a un prefab compartido.
- El prefab de jugador se registra en `NetworkManager → PlayerPrefab`; sustituye `FL_Player` por tu personaje.
- `DemoTest/` entero puede borrarse cuando ya no sirva como smoke test — nada fuera de él lo referencia.

## Dónde integrar voz

`Assets/FriendsLoop/Voice/`. Implementar `IVoiceService` con el proveedor elegido
(Steam Voice / Vivox / Dissonance) y registrarlo en `VoiceServices.Register(...)` durante el bootstrap.
El resto del juego solo usa la interfaz (`JoinSessionChannel`, `SetMuted`, push-to-talk, evento
`OnSpeakingChanged`). Mientras tanto `NullVoiceService` mantiene todo compilando y funcionando sin voz.

## Qué es exclusivo de Steam

- `Platform/SteamBootstrap` y `Session/SteamLobbyService` (guardados por `#if !DISABLESTEAMWORKS && STEAMWORKSNET`).
- El transporte vendorizado en `Packages/`.
- `steam_appid.txt` (solo desarrollo; **no** debe ir en la build que se sube a la tienda — Steam la lanza con el App ID real).
- Todo lo demás (NGO, transporte local, demo, voz) funciona sin Steam: el template entero corre en modo
  LocalLoopback aunque Steam no esté instalado.

Para el lanzamiento real: reemplazar 480 por el App ID propio en `steam_appid.txt` (dev) y publicar la
build vía Steamworks; nada del código cambia salvo la clave `FL_GAME` (campo serializado en `SteamLobbyService`).

## Configuración que DEBE cambiar cada proyecto nuevo

Al crear un juego desde esta plantilla, revisa esta lista antes de la primera build:

1. **Nombre del producto y compañía**: `Project Settings → Player` (Product Name, Company Name).
   También definen la ruta del `Player.log`.
2. **App ID real de Steam**: sustituir `480` en `steam_appid.txt` (se autogenera; editar
   `Editor/SteamAppIdWriter.cs` para que escriba el ID real) y recordar que ese archivo NUNCA
   se incluye en la build subida a la tienda.
3. **Clave de lobby `FL_GAME`**: campo serializado `gameKey` en `SteamLobbyService`
   (`FL_Network` de la escena). Cambiarla a algo único del juego (p. ej. `com.tuestudio.tujuego`)
   para no cruzarte con lobbies de otros proyectos bajo el mismo App ID de desarrollo.
4. **Nombre del repositorio/carpeta** del proyecto Unity si procede.
5. **Escena inicial**: la demo (`FL_DemoScene`) queda como smoke test; el juego real registra sus
   propias escenas en Build Settings delante o en lugar de ella.
6. **Diagnóstico**: `NetDiagnostics` (en `FL_Network`) solo actúa en editor/development builds;
   déjalo como está salvo que quieras activarlo en release para una prueba puntual.

## Nota sobre Git LFS y GitHub Templates

`.gitattributes` envía binarios pesados (imágenes, modelos, audio…) a Git LFS — correcto para los
juegos. Pero **GitHub no permite marcar como Template Repository un repo con archivos en LFS**:
por eso esta plantilla no contiene ninguno (se eliminó el único, un icono del tutorial de Unity).
Si añades assets binarios a la PLANTILLA misma, dejará de poder usarse como template en GitHub.
En los repos de juegos generados desde ella no hay restricción: usa LFS con normalidad.

## Smoke test rápido (¿se rompió networking?)

1. Abrir `FL_DemoScene` → Play → consola debe decir `Steamworks inicializado correctamente` (o el aviso claro de que Steam no está — también es un estado válido).
2. `Host (Local)` → aparece tu cápsula con nombre y `Estado: Hosting`.
3. `Alternar cubo` → el cubo cambia de color/altura.
4. `Desconectar` → consola: `Sesión finalizada` sin errores.
Si esos 4 pasos pasan, la infraestructura está sana. (Con 2 PCs: repetir con Host (Steam) + Unirse (Steam).)
