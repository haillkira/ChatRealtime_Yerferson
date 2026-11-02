# Chat en tiempo real

**Estudiante:** Yerferson Rosario Peña  
**Matricula:** 100444062  
**Asignatura:** Programacion IV  
**Framework:** .NET 10

Chat desarrollado con ASP.NET Core, Razor Pages y SignalR.

## Funciones

- Envio y recepcion de mensajes en tiempo real.
- Reserva de alias entre usuarios conectados.
- Colores automaticos para los nombres.
- Historial de los ultimos 50 mensajes guardado en SQLite.
- Reconexion automatica.

Los alias se reservan mientras la conexion esta activa.
No representan cuentas con contrasena.

## Proyectos

- Chat.Server: servidor SignalR y almacenamiento con Dapper y SQLite.
- Chat.Client: interfaz web con Razor Pages y JavaScript.
- Chat.Shared: contrato compartido de los mensajes.

## Ejecucion local

Desde la carpeta principal, ejecutar en terminales separadas:

```powershell
dotnet run --project .\Chat.Server --no-launch-profile --urls "https://localhost:7101"
```

```powershell
dotnet run --project .\Chat.Client --no-launch-profile --urls "https://localhost:7002"
```

Abrir https://localhost:7002.

La base de datos se crea automaticamente al iniciar el servidor.