(function () {
    const $ = (id) => document.getElementById(id);
    const IGV = 0.18;

    // estado de la pantalla (lo que el sistema muestra en la cuadrícula)
    let cliente = null;
    let dias = 0;
    let detalle = [];

    const fmt = (n) => Number(n).toFixed(2);

    function alerta(texto, tipo) {
        const a = $("alerta");
        a.className = "alert alert-" + (tipo || "info");
        a.textContent = texto;
        window.scrollTo({ top: 0, behavior: "smooth" });
    }

    async function llamar(url, opciones) {
        const res = await fetch(url, opciones);
        let data = null;
        try { data = await res.json(); } catch (e) { /* sin cuerpo */ }
        return { ok: res.ok, status: res.status, data };
    }

    // ---------- Cliente (pasos 2-4) ----------
    $("btnBuscarCliente").addEventListener("click", async () => {
        const cod = $("codCliente").value.trim();
        if (!cod) return alerta("Ingrese el código del cliente.", "warning");

        const r = await llamar("/Reserva/BuscarCliente?codigo=" + encodeURIComponent(cod));
        if (r.ok) {
            cliente = r.data;
            $("nombreCliente").value = r.data.nombreCompleto;
            $("panelNuevoCliente").classList.add("d-none");
            $("alerta").className = "alert d-none";
        } else {
            // Flujo alterno: Cliente no existe
            cliente = null;
            $("nombreCliente").value = "";
            alerta("Cliente no existe. Puede registrarlo con «Agregar Cliente».", "warning");
            mostrarPanelNuevoCliente(cod);
        }
    });

    function mostrarPanelNuevoCliente(cod) {
        $("panelNuevoCliente").classList.remove("d-none");
        $("nuevoCodigo").value = cod || $("codCliente").value;
        $("nuevoNombres").focus();
    }
    $("btnNuevoCliente").addEventListener("click", () => mostrarPanelNuevoCliente());

    $("btnGuardarCliente").addEventListener("click", async () => {
        const body = {
            codCliente: $("nuevoCodigo").value.trim(),
            nombres: $("nuevoNombres").value.trim(),
            apellidos: $("nuevoApellidos").value.trim()
        };
        const r = await llamar("/Reserva/AgregarCliente", {
            method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(body)
        });
        if (!r.ok) return alerta(r.data?.mensaje || "No se pudo registrar el cliente.", "danger");
        cliente = r.data;
        $("codCliente").value = r.data.codCliente;
        $("nombreCliente").value = r.data.nombreCompleto;
        $("panelNuevoCliente").classList.add("d-none");
        alerta("Cliente registrado correctamente.", "success");
    });

    // ---------- Fechas (pasos 5-7) ----------
    async function calcularDias() {
        const lleg = $("fechaLlegada").value, sal = $("fechaSalida").value;
        if (!lleg || !sal) return;
        const r = await llamar(`/Reserva/CalcularDias?llegada=${lleg}&salida=${sal}`);
        if (!r.ok) {
            dias = 0; $("cantDias").value = "";
            return alerta(r.data?.mensaje || "Fechas inválidas.", "warning");
        }
        dias = r.data.dias;
        $("cantDias").value = dias;
        // si ya había habitaciones, se recalculan con los nuevos días
        detalle.forEach((d) => { d.dias = dias; d.monto = d.costoDia * dias; });
        pintarDetalle();
    }
    $("fechaLlegada").addEventListener("change", calcularDias);
    $("fechaSalida").addEventListener("change", calcularDias);

    // ---------- Buscar habitación (pasos 8-10) ----------
    $("btnBuscarHab").addEventListener("click", async () => {
        if (dias <= 0) return alerta("Primero ingrese las fechas de llegada y salida.", "warning");
        const r = await llamar("/Reserva/HabitacionesDisponibles");
        const cuerpo = $("tbodyDisponibles");
        cuerpo.innerHTML = "";
        if (!r.ok) {
            $("listaDisponibles").classList.add("d-none");
            return alerta(r.data?.mensaje || "No hay habitaciones disponibles", "warning");
        }
        // no mostrar las que ya están en la cuadrícula
        const usadas = new Set(detalle.map((d) => d.numHabitacion));
        const libres = r.data.filter((h) => !usadas.has(h.numHabitacion));
        if (libres.length === 0) {
            $("listaDisponibles").classList.add("d-none");
            return alerta("No hay habitaciones disponibles", "warning");
        }
        libres.forEach((h) => {
            const tr = document.createElement("tr");
            tr.innerHTML = `<td>${h.numHabitacion}</td><td>${h.tipo}</td><td>${fmt(h.costoDia)}</td>
                            <td class="text-end"><button type="button" class="btn btn-sm btn-outline-primary">Seleccionar</button></td>`;
            tr.querySelector("button").addEventListener("click", () => {
                $("numHabitacion").value = h.numHabitacion;
                $("tipoHab").value = h.tipo;
                $("costoDia").value = fmt(h.costoDia);
                $("huesped").focus();
            });
            cuerpo.appendChild(tr);
        });
        $("listaDisponibles").classList.remove("d-none");
        $("alerta").className = "alert d-none";
    });

    // ---------- Agregar habitación (pasos 11-13) ----------
    $("btnAgregarHab").addEventListener("click", () => {
        const num = $("numHabitacion").value;
        const huesped = $("huesped").value.trim();
        if (!num) return alerta("Seleccione una habitación.", "warning");
        if (!huesped) return alerta("Ingrese el nombre del huésped.", "warning");
        if (dias <= 0) return alerta("Ingrese las fechas de la reserva.", "warning");

        const costo = parseFloat($("costoDia").value);
        detalle.push({
            numHabitacion: num, tipo: $("tipoHab").value, nombreHuesped: huesped,
            costoDia: costo, dias: dias, monto: costo * dias
        });
        $("numHabitacion").value = $("tipoHab").value = $("costoDia").value = $("huesped").value = "";
        $("listaDisponibles").classList.add("d-none");
        pintarDetalle();
    });

    // ---------- Cuadrícula y totales ----------
    function pintarDetalle() {
        const cuerpo = $("tbodyDetalle");
        cuerpo.innerHTML = "";
        if (detalle.length === 0) {
            cuerpo.innerHTML = '<tr><td colspan="7" class="text-center text-muted">Aún no hay habitaciones</td></tr>';
        }
        detalle.forEach((d, i) => {
            const tr = document.createElement("tr");
            tr.innerHTML = `<td>${d.numHabitacion}</td><td>${d.tipo}</td><td></td>
                            <td>${fmt(d.costoDia)}</td><td>${d.dias}</td><td>${fmt(d.monto)}</td>
                            <td class="text-end"><button type="button" class="btn btn-sm btn-outline-danger">Eliminar</button></td>`;
            tr.children[2].textContent = d.nombreHuesped; // textContent evita inyectar HTML
            tr.querySelector("button").addEventListener("click", () => {
                detalle.splice(i, 1);          // flujo alterno: Eliminar habitación de la cuadrícula
                pintarDetalle();
            });
            cuerpo.appendChild(tr);
        });
        const sub = detalle.reduce((s, d) => s + d.monto, 0);
        const igv = Math.round(sub * IGV * 100) / 100;
        $("subTotal").value = fmt(sub);
        $("igv").value = fmt(igv);
        $("montoTotal").value = fmt(sub + igv);
    }

    // ---------- Grabar (pasos 15-18) ----------
    $("btnGrabar").addEventListener("click", async () => {
        if (!cliente) return alerta("Busque o registre un cliente.", "warning");
        if (dias <= 0) return alerta("Ingrese las fechas de la reserva.", "warning");
        if (detalle.length === 0) return alerta("Agregue al menos una habitación.", "warning");

        const body = {
            codCliente: cliente.codCliente,
            fechaLlegada: $("fechaLlegada").value,
            fechaSalida: $("fechaSalida").value,
            detalle: detalle.map((d) => ({ numHabitacion: d.numHabitacion, nombreHuesped: d.nombreHuesped }))
        };
        const r = await llamar("/Reserva/Grabar", {
            method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(body)
        });
        if (!r.ok) return alerta(r.data?.mensaje || "No se pudo grabar la reserva.", "danger");

        alerta(r.data.mensaje, "success");   // "Reserva generada con el Nro. X"
        limpiar();
    });

    function limpiar() {
        cliente = null; dias = 0; detalle = [];
        ["codCliente", "nombreCliente", "fechaLlegada", "fechaSalida", "cantDias",
         "numHabitacion", "tipoHab", "costoDia", "huesped"].forEach((id) => ($(id).value = ""));
        $("listaDisponibles").classList.add("d-none");
        $("panelNuevoCliente").classList.add("d-none");
        pintarDetalle();
    }

    pintarDetalle();
})();
