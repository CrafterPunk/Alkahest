# Incidentes conocidos

## INC-001 · Congelación asimétrica de sesión Steam (NO REPRODUCIDO)

- **Fecha**: agosto 2026, durante las pruebas reales entre 2 PCs / 2 cuentas (App ID 480).
- **Frecuencia**: 1 vez en muchas sesiones de prueba. No se ha podido reproducir.
- **Síntomas**: en una sesión ya establecida y funcionando, el host dejó de ver al cliente
  (su cápsula desapareció para el host) mientras el cliente seguía viendo al host pero congelado.
  Ninguno de los dos recibió un evento de desconexión visible. Cerrar y recrear la sesión lo
  resolvió; desde el siguiente intento todo volvió a funcionar con normalidad.
- **Lectura técnica de los síntomas**: son consistentes con un corte asimétrico de la conexión
  SteamNetworkingSockets — el lado del host dio la conexión por perdida (despawn del jugador
  remoto) mientras el lado del cliente siguió considerándola viva y quedó a la espera de tráfico
  que ya no llegaba. Causas plausibles: blip de red / cambio de ruta del relay SDR, o cierre con
  `bEnableLinger=false` cuyo aviso de cierre no llegó al peer. **No hay evidencia de un bug en el
  código de la plantilla**, y no se hizo ningún cambio especulativo de networking.
- **Acción tomada**: se añadió `Networking/NetDiagnostics.cs` (solo desarrollo — editor y
  development builds), que registra con timestamp: cambios de estado de SteamNetworkingSockets
  con `endReason`/`endDebug` de Valve, callbacks de conexión/desconexión y `OnTransportFailure`
  de NGO, SteamIDs y relay POP, y un sondeo periódico (ping/calidad/bytes pendientes) que indica
  si el transporte todavía consideraba conectado al peer. El transporte vendorizado no registraba
  nada de esto por defecto (solo con LogLevel=Developer, y aun así sin el motivo de cierre).
- **Si vuelve a ocurrir**: recoger `Player.log` de AMBAS máquinas
  (`%USERPROFILE%\AppData\LocalLow\<Company>\<Producto>\Player.log`) y buscar las líneas
  `[FriendsLoop][DIAG ...]` alrededor del momento de la congelación. Con el `endReason` de Valve
  y el último sondeo de cada lado, el origen (timeout local, cierre del peer, fallo de relay)
  queda identificado sin ambigüedad.
