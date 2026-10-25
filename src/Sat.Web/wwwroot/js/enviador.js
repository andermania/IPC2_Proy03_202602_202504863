// Programa 1 (Frontend): envío de mensajes JSON al Servicio 2 (API).
(function () {
    "use strict";

    const form = document.getElementById("formEnvio");
    if (!form) return;

    const apiBaseUrl = form.dataset.api;
    const tipoMensaje = document.getElementById("tipoMensaje");
    const archivoJson = document.getElementById("archivoJson");
    const btnEnviar = document.getElementById("btnEnviar");
    const btnInicializar = document.getElementById("btnInicializar");
    const cajaEstado = document.getElementById("estado");
    const cajaRespuesta = document.getElementById("respuesta");
    const cajaResumen = document.getElementById("resumen");

    const rutas = {
        rtu: "/api/rtu",
        autorizaciones: "/api/autorizaciones",
    };

    // Los códigos de aprobación (yyyymmdd########) superan la precisión de
    // Number en JavaScript: si se parsea el JSON y se vuelve a serializar, se
    // pierden los últimos dígitos. Por eso el texto crudo se formatea aquí,
    // sin convertir los números a valores numéricos.
    function formatear(json) {
        let salida = "";
        let nivel = 0;
        let enCadena = false;
        let escapado = false;

        const sangria = () => "  ".repeat(Math.max(0, nivel));

        for (let i = 0; i < json.length; i++) {
            const c = json[i];

            if (enCadena) {
                salida += c;
                if (escapado) escapado = false;
                else if (c === "\\") escapado = true;
                else if (c === '"') enCadena = false;
                continue;
            }

            if (c === '"') {
                enCadena = true;
                salida += c;
            } else if (c === "{" || c === "[") {
                nivel++;
                salida += c + "\n" + sangria();
            } else if (c === "}" || c === "]") {
                nivel--;
                salida += "\n" + sangria() + c;
            } else if (c === ",") {
                salida += c + "\n" + sangria();
            } else if (c === ":") {
                salida += ": ";
            } else if (c === " " || c === "\n" || c === "\r" || c === "\t") {
                // El espacio fuera de las cadenas es prescindible.
            } else {
                salida += c;
            }
        }

        return salida;
    }

    function mostrarEstado(mensaje, tipo) {
        cajaEstado.hidden = false;
        cajaEstado.textContent = mensaje;
        cajaEstado.className = "estado" + (tipo ? " " + tipo : "");
    }

    function tarjeta(etiqueta, valor) {
        const div = document.createElement("div");
        div.className = "dato";
        const b = document.createElement("b");
        b.textContent = String(valor);
        div.appendChild(b);
        div.appendChild(document.createTextNode(etiqueta));
        return div;
    }

    function mostrarResumen(datos) {
        cajaResumen.innerHTML = "";
        cajaResumen.hidden = false;

        if (datos && datos.resultadoAutorizaciones) {
            const r = datos.resultadoAutorizaciones;
            cajaResumen.appendChild(tarjeta("Autorizaciones aprobadas", r.cantidadAutorizacionesAprobadas ?? 0));
            cajaResumen.appendChild(
                tarjeta("Rechazadas por emisor inexistente", r.cantidadAutorizacionesRechazadasPorNitEmisorInexistente ?? 0)
            );
            cajaResumen.appendChild(
                tarjeta("Rechazadas por receptor inexistente", r.cantidadAutorizacionesRechazadasPorNitReceptorInexistente ?? 0)
            );
            if (Array.isArray(r.detalleAprobaciones)) {
                cajaResumen.appendChild(tarjeta("Códigos emitidos", r.detalleAprobaciones.length));
            }
            return;
        }

        if (datos && typeof datos === "object" && "CONTRIBUYENTES_NUEVOS" in datos) {
            cajaResumen.appendChild(tarjeta("Contribuyentes nuevos", datos.CONTRIBUYENTES_NUEVOS));
            cajaResumen.appendChild(tarjeta("Contribuyentes actualizados", datos.CONTRIBUYENTES_ACTUALIZADOS));
            cajaResumen.appendChild(tarjeta("NITs inválidos", datos.NITS_INVALIDOS));
            return;
        }

        if (datos && datos.estado) {
            cajaResumen.appendChild(tarjeta("Estado del sistema", datos.estado));
        } else {
            cajaResumen.hidden = true;
        }
    }

    // Muestra la respuesta del API: el texto crudo (con sus cifras exactas)
    // y, cuando es JSON válido, el resumen con los indicadores principales.
    function mostrarRespuesta(texto) {
        cajaRespuesta.textContent = texto && texto.length
            ? formatear(texto)
            : "(sin contenido)";

        let datos = null;
        try {
            datos = JSON.parse(texto);
        } catch (error) {
            datos = null;
        }

        mostrarResumen(datos);
        return datos;
    }

    form.addEventListener("submit", async function (evento) {
        evento.preventDefault();

        const archivo = archivoJson.files[0];
        if (!archivo) {
            mostrarEstado("Seleccione un archivo JSON antes de enviar.", "error");
            return;
        }

        let texto;
        try {
            texto = await archivo.text();
            JSON.parse(texto);
        } catch (error) {
            mostrarEstado("El archivo seleccionado no contiene JSON válido: " + error.message, "error");
            return;
        }

        const ruta = rutas[tipoMensaje.value];
        btnEnviar.disabled = true;
        mostrarEstado("Enviando mensaje al servicio...");

        try {
            const respuesta = await fetch(apiBaseUrl + ruta, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: texto,
            });

            const cuerpo = await respuesta.text();

            if (!respuesta.ok) {
                mostrarEstado("El servicio rechazó el mensaje (HTTP " + respuesta.status + ").", "error");
                mostrarRespuesta(cuerpo);
                return;
            }

            mostrarEstado("Mensaje procesado correctamente.", "exito");
            mostrarRespuesta(cuerpo);
        } catch (error) {
            mostrarEstado("No fue posible comunicarse con el servicio: " + error.message, "error");
        } finally {
            btnEnviar.disabled = false;
        }
    });

    btnInicializar.addEventListener("click", async function () {
        const confirmar = window.confirm(
            "Esta acción devolverá el sistema al estado inicial (sin RTU y sin facturas). ¿Desea continuar?"
        );
        if (!confirmar) return;

        btnInicializar.disabled = true;
        try {
            const respuesta = await fetch(apiBaseUrl + "/api/sistema/inicializar", { method: "POST" });
            const cuerpo = await respuesta.text();

            if (!respuesta.ok) {
                mostrarEstado("No fue posible inicializar el sistema.", "error");
                return;
            }

            mostrarEstado("Sistema inicializado: sin datos.", "exito");
            mostrarRespuesta(cuerpo);
        } catch (error) {
            mostrarEstado("No fue posible comunicarse con el servicio: " + error.message, "error");
        } finally {
            btnInicializar.disabled = false;
        }
    });
})();
