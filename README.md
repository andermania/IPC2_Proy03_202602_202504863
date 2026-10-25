# IPC2_Proy03_202602_202504863

Proyecto 3 - Servicio HTTP para la autorización de Documentos Tributarios Electrónicos (DTE)
de la Superintendencia de Administración Tributaria (SAT), desarrollado en C# (.NET 10) con
programación orientada a objetos.

- **Estudiante:** Kevin Ánderson Montejo Martínez
- **Carné:** 202504863
- **Repositorio:** https://github.com/andermania/IPC2_Proy03_202602_202504863
- **Documentación (ensayo, diagramas):** carpeta [`docs/`](docs/)

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
│   ├── Servicios/              Reglas de negocio (RTU, autorizaciones y reportes)
│   ├── Infraestructura/        Lectura de los mensajes JSON de entrada
│   └── App_Data/               Archivos generados: rtu.json, facturas.json, correlativo.json
└── Sat.Web/                    Programa 1 - Aplicación web Razor Pages (frontend simulador)
    ├── Pages/                  Enviar mensaje, Ayuda y página de error
    ├── Pages/Peticiones/       Consultar, resúmenes, gráficas y reporte en PDF
    └── Servicios/              Cliente del API, gráficas SVG y generador de PDF
pruebas/                        Archivos JSON de ejemplo para probar el servicio
docs/                           Documentación del proyecto (ensayo y diagramas)
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

La información se persiste en la carpeta `App_Data/` en la raíz del repositorio
(`rtu.json`, `facturas.json` y `correlativo.json`). La ruta se resuelve a partir de la
solución, por lo que la información es la misma tanto si se ejecuta con `dotnet run`
como si se ejecuta el `.exe` compilado en `bin/`.

> Para borrar todos los datos se utiliza el botón **Inicializar sistema** del frontend o
> `POST /api/sistema/inicializar`.

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
# Los mensajes de solicitud son SIEMPRE un arreglo de objetos; cualquier otra
# estructura responde HTTP 400 con un mensaje de error en español.
curl -X POST http://localhost:5200/api/autorizaciones \
  -H "Content-Type: application/json" \
  -d @pruebas/04_facturas_bordes_formato.json
```

## Peticiones del Programa 1 (frontend)

| Pantalla | Descripción |
|----------|-------------|
| Enviar mensaje | Envía archivos `.json` de actualización del RTU o de autorizaciones y muestra la respuesta del API. Incluye el botón **Inicializar sistema**. |
| Consultar contribuyente | Facturas emitidas o recibidas de un NIT en un rango: cantidad autorizada y monto por fecha, más el total del rango. |
| Resumen de IVA por fecha y NIT | IVA emitido e IVA recibido de cada NIT en una fecha. |
| Resumen por rango de fechas | Valor autorizado por fecha, con IVA (totales) o sin IVA. |
| Reporte de facturas y errores | Facturas recibidas, errores por categoría, facturas sin errores, emisores y receptores distintos. |
| Gráficas | Gráficas de barras de los dos resúmenes anteriores (SVG generados por el servidor, sin conexión a Internet). |
| Reporte en PDF | Documento PDF con los cuatro reportes anteriores (generado en C#, sin librerías de terceros). |
| Ayuda | Información del estudiante y documentación del programa. |

## Reglas implementadas

- **Mensajes de entrada:** los dos mensajes de solicitud son arreglos de objetos JSON,
  tal como los define el enunciado; no se modifican. Cualquier otra estructura (un objeto
  suelto, un arreglo vacío o un arreglo de objetos sin las propiedades del mensaje) se
  rechaza con `HTTP 400`.
- **NIT**: formato `XXXXXXV` (1 a 20 dígitos) y dígito verificador calculado con el algoritmo
  del enunciado (multiplicación por posición desde la derecha, módulo 11, 11 menos el resto,
  10 = `K`). El modo de validación se configura en `src/Sat.Api/appsettings.json`
  (`ValidacionNIT:Modo`: `Enunciado`, `SAT` o `Ambos`; por defecto `Ambos`, que acepta el
  NIT si cumple cualquiera de los dos algoritmos). La configuración existe porque las NIT de
  ejemplo del enunciado no cumplen el algoritmo descrito en el propio enunciado:
  `7378106` y `8338817` sólo cumplen el algoritmo de la SAT, mientras que `5123456` sólo
  cumple el del enunciado.
- **Fecha**: extraída con expresiones regulares de textos como
  `Guatemala, 15/01/2025 15:25 hrs.`, descartando el nombre del lugar sin considerarlo error.
- **IVA**: `REDONDEAR(VALOR * 0.12, 2)`. **TOTAL**: `VALOR + IVA`.
- **REFERENCIA**: única (la primera gana, verificación y reserva atómicas), máximo 40 posiciones.
- **Número de autorización**: estructura `yyyymmdd########`, correlativo que inicia en 1
  cada día y no se repite (persistido en `correlativo.json`). El prefijo `yyyymmdd` corresponde
  a la fecha en que **se solicita** la autorización (reloj del servidor), tal como lo indica el
  enunciado.
- **RTU**: el NIT inválido no tiene efecto; un NIT válido crea o actualiza al contribuyente y
  un mensaje sin `nombre` no borra la razón social ya registrada.
- **Persistencia**: archivos JSON (`rtu.json`, `facturas.json`, `correlativo.json`).
- **Errores**: una factura puede acumular varios errores, por lo que la suma de las categorías
  puede superar a la cantidad de facturas rechazadas. Las facturas cuya fecha no pudo
  extraerse se agrupan bajo la fila `sin fecha` para que no desaparezcan del reporte.

## Pruebas incluidas

| Archivo | Contenido |
|---------|-----------|
| `pruebas/01_rtu_completo.json` | RTU con NIT nuevos, repetidos e inválidos (8 nuevos, 1 actualizado, 4 inválidos). |
| `pruebas/02_facturas_marzo_graficas.json` | Facturas válidas repartidas en varias fechas, para los resúmenes y las gráficas. |
| `pruebas/03_facturas_todos_los_errores.json` | Una factura por cada categoría de error (emisor/receptor inexistente e inválido, IVA, TOTAL, referencia duplicada y fecha inválida). |
| `pruebas/04_facturas_bordes_formato.json` | Límites de formato: NIT de 1 y de 20 dígitos, hora 23:59, montos con 2 decimales. |
| `pruebas/05a_json_malformado.json` | JSON sintácticamente inválido → HTTP 400. |
| `pruebas/05b_arreglo_vacio.json` | Arreglo vacío → HTTP 400. |
| `pruebas/05c_objeto_suelto.json` | Objeto suelto en lugar de un arreglo → HTTP 400. |
| `pruebas/05d_sin_llaves_conocidas.json` | Objetos sin las propiedades del mensaje → HTTP 400. |
| `pruebas/06_carga_100_facturas.json` | Prueba de carga: 100 facturas en un solo mensaje. |

## Pruebas recomendadas

```bash
curl -X POST http://localhost:5200/api/sistema/inicializar            # estado inicial
curl -X POST http://localhost:5200/api/rtu -H "Content-Type: application/json" -d @pruebas/01_rtu_completo.json
curl -X POST http://localhost:5200/api/autorizaciones -H "Content-Type: application/json" -d @pruebas/03_facturas_todos_los_errores.json
curl "http://localhost:5200/api/reportes/errores?desde=01/01/2020&hasta=31/12/2030"
```
