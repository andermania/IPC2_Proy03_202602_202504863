# IPC2_Proy03_202602_2020504863

Proyecto 3 - Servicio HTTP para la autorización de Documentos Tributarios Electrónicos (DTE)
de la Superintendencia de Administración Tributaria (SAT), desarrollado en C# (.NET 10) con
programación orientada a objetos.

## Estructura del repositorio

```
Proy03.sln                      Solución con los dos programas
src/
├── Sat.Api/                    Servicio 2 - API HTTP (.NET Core / ASP.NET Core)
│   ├── Controllers/            Endpoints del servicio
│   ├── Dominio/                Validación de NIT, extracción de fechas y correlativo
│   ├── Entidades/              Clases del modelo (POO)
│   ├── Datos/                  Capa de datos con persistencia en archivos JSON
│   ├── TDA/                    Tipos de dato abstracto propios (lista enlazada, tabla hash)
│   ├── Servicios/              Reglas de negocio (RTU y autorizaciones)
│   ├── Infraestructura/        Lectura de los mensajes JSON de entrada
│   └── App_Data/               Archivos generados: rtu.json, facturas.json, correlativo.json
└── Sat.Web/                    Programa 1 - Aplicación web Razor Pages (frontend simulador)
pruebas/                        Archivos JSON de ejemplo para probar el servicio
```

## Requisitos

- .NET SDK 10.0 o superior.

## Cómo ejecutar

```bash
# Terminal 1: Servicio 2 (API) - http://localhost:5200
dotnet run --project src/Sat.Api/Sat.Api.csproj

# Terminal 2: Programa 1 (Frontend) - http://localhost:5300
dotnet run --project src/Sat.Web/Sat.Web.csproj
```

## Endpoints del API

| Método | Ruta                              | Descripción                                            |
|--------|-----------------------------------|--------------------------------------------------------|
| POST   | `/api/rtu`                        | Actualiza el RTU (crea, actualiza o ignora NITs)       |
| GET    | `/api/rtu`                        | Consulta los contribuyentes registrados                |
| POST   | `/api/autorizaciones`             | Solicita la autorización de las facturas               |
| POST   | `/api/sistema/inicializar`        | Devuelve el sistema al estado inicial (sin datos)      |
| GET    | `/api/sistema/salud`              | Verifica que el servicio esté operativo                |
| GET    | `/api/reportes/errores`           | Reporte de facturas y errores por rango de fechas      |
| GET    | `/api/reportes/contribuyente`     | Facturas autorizadas y monto por fecha de un NIT       |
| GET    | `/api/reportes/facturas`          | Listado de facturas emitidas o recibidas de un NIT     |
| GET    | `/api/reportes/iva`               | IVA emitido y recibido por NIT en una fecha            |
| GET    | `/api/reportes/rango`             | Valor autorizado por fecha (con IVA o sin IVA)         |
| GET    | `/api/reportes/nits`              | Catálogo de contribuyentes del RTU                     |

Parámetros de las consultas: `desde` y `hasta` en formato `dd/MM/yyyy` (también se acepta
`yyyy-MM-dd`), `tipo` = `emitidas` | `recibidas` | `ambas`, `fecha` = `dd/MM/yyyy` y
`tipoValores` = `total` | `sinIva`.

### Ejemplo con Postman o curl

```bash
curl -X POST http://localhost:5200/api/autorizaciones \
  -H "Content-Type: application/json" \
  -d @pruebas/autorizaciones_validas.json
```

## Reglas implementadas

- **NIT**: formato `XXXXXXV` (1 a 20 dígitos) y dígito verificador calculado con el algoritmo
  del enunciado (multiplicación por posición desde la derecha, módulo 11, 11 menos el resto,
  10 = `K`). El modo de validación se configura en `src/Sat.Api/appsettings.json`
  (`ValidacionNIT:Modo`: `Enunciado`, `SAT` o `Ambos`; por defecto `Ambos`, que acepta el
  NIT si cumple cualquiera de los dos algoritmos). La configuración existe porque las NIT de
  ejemplo del enunciado no cumplen el algoritmo descrito en el propio enunciado.
- **Fecha**: extraída con expresiones regulares de textos como
  `Guatemala, 15/01/2025 15:25 hrs.`, descartando el nombre del lugar sin considerarlo error.
- **IVA**: `REDONDEAR(VALOR * 0.12, 2)`. **TOTAL**: `VALOR + IVA`.
- **REFERENCIA**: única (la primera gana), máximo 40 posiciones.
- **Números de autorización**: estructura `yyyymmdd########`, correlativo que inicia en 1
  cada día y no se repite (persistido en `correlativo.json`).
- **RTU**: el NIT inválido no tiene efecto; un NIT válido crea o actualiza el contribuyente.
- **Persistencia**: archivos JSON (`rtu.json`, `facturas.json`, `correlativo.json`).

## Pruebas incluidas

- `pruebas/rtu_validos.json`: RTU con 2 NITs válidos y 1 inválido.
- `pruebas/autorizaciones_validas.json`: facturas correctas (generan código de autorización).
- `pruebas/autorizaciones_casos_error.json`: emisor/receptor inexistente, IVA y TOTAL mal
  calculados, referencia duplicada, fecha inválida y NIT inválido.
- `pruebas/reportes_escenario.json`: escenario de 10 facturas (2 fechas, todas las
  categorías de error) para verificar los reportes de `/api/reportes/*`.
- `pruebas/*_muestra_enunciado.json`: mensajes tal como aparecen en el enunciado.
