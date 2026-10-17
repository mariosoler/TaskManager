/*
 * Detalle de tarjeta (modal):
 * - Al editar una tarjeta existente, los cambios se guardan solos mientras se escribe
 *   (POST a Tarjeta.ashx tras una breve pausa); el botón Guardar se oculta.
 * - Pulsar fuera del modal lo cierra (como la ✕ o Esc). Antes de cerrar se guarda lo pendiente;
 *   si se está creando una tarjeta con datos escritos, se pide confirmación para descartarla.
 * - Marcar una subtarea no recarga la página: al editar se guarda con Tarjeta.ashx (accion=subtarea);
 *   al crear, el estado viaja con el siguiente envío del formulario (CheckedChanged en el servidor).
 * - Tras un postback dentro del detalle (añadir/eliminar subtarea) se recupera su desplazamiento.
 * Sin JavaScript todo sigue funcionando con el botón Guardar (postback).
 */
(function () {
    'use strict';

    var cfg = window.TaskManagerDetalle;
    if (!cfg) { return; }
    var fondo = document.getElementById(cfg.modal);
    var cerrarBoton = document.getElementById(cfg.cerrar);
    if (!fondo || !cerrarBoton) { return; }

    var PAUSA = 800;            // ms sin escribir antes de guardar
    var editando = cfg.tarjeta > 0;
    var campos = {};
    Object.keys(cfg.campos).forEach(function (nombre) { campos[nombre] = document.getElementById(cfg.campos[nombre]); });

    var estado = document.getElementById(cfg.estado);
    var guardadoValores = null; // últimos valores guardados (cuerpo de la petición)
    var temporizador = null;
    var enCurso = null;         // promesa del guardado en marcha
    var cerrando = false;

    function valor(nombre) { return campos[nombre] ? campos[nombre].value : ''; }

    function cuerpo() {
        var p = new URLSearchParams();
        p.set('tablero', String(cfg.tablero));
        p.set('tarjeta', String(cfg.tarjeta));
        Object.keys(campos).forEach(function (nombre) { p.set(nombre, valor(nombre)); });
        return p.toString();
    }

    function mostrar(texto, clase) {
        if (!estado) { return; }
        estado.textContent = texto || '';
        estado.className = 'guardado-estado' + (clase ? ' ' + clase : '');
    }

    // Validación mínima en el cliente (el servidor vuelve a validar todo).
    function errorLocal() {
        if (valor('titulo').trim() === '') { return 'Escribe un título para guardar los cambios.'; }
        if (!/^\s*[0-9]{0,20}\s*$/.test(valor('caso'))) { return 'El caso solo admite números.'; }
        return null;
    }

    /* POST a Tarjeta.ashx. Se resuelve con la respuesta JSON o se rechaza con un Error con el mensaje. */
    function enviar(texto, keepalive) {
        return fetch(cfg.url, {
            method: 'POST',
            credentials: 'same-origin',
            // keepalive deja terminar la petición aunque se recargue la página (máx. 64 KB de cuerpo).
            keepalive: !!keepalive && texto.length < 60000,
            headers: { 'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8', 'X-Chat-Token': cfg.token },
            body: texto
        }).then(function (r) {
            return r.json().catch(function () { return {}; }).then(function (datos) {
                if (!r.ok || !datos.ok) { throw new Error(datos.error || 'No se han podido guardar los cambios.'); }
                return datos;
            });
        });
    }

    function mostrarGuardado(datos) {
        mostrar('✓ Guardado' + (datos.modificacion ? ' · ' + datos.modificacion : ''), 'ok');
    }

    function hayCambios() { return guardadoValores !== null && cuerpo() !== guardadoValores; }

    /* Guarda si hay cambios. Devuelve una promesa que se resuelve con null (bien) o con el texto del error. */
    function guardar(alSalir) {
        clearTimeout(temporizador);
        if (enCurso) {
            // Al terminar el guardado en marcha se comprueba si quedan cambios por enviar.
            return enCurso.then(function () { return guardar(alSalir); });
        }
        if (!hayCambios()) { return Promise.resolve(null); }
        var error = errorLocal();
        if (error) {
            mostrar(error, 'error');
            return Promise.resolve(error);
        }

        var enviado = cuerpo();
        mostrar('Guardando…', 'guardando');
        enCurso = enviar(enviado, alSalir).then(function (datos) {
            guardadoValores = enviado;
            if (datos.aviso) { mostrar(datos.aviso, 'error'); } else { mostrarGuardado(datos); }
            return null;
        }).catch(function (e) {
            mostrar((e && e.message) || 'No se han podido guardar los cambios.', 'error');
            return (e && e.message) || 'error';
        }).then(function (resultado) {
            enCurso = null;
            return resultado;
        });
        return enCurso;
    }

    function programar() {
        if (!editando || cerrando) { return; }
        clearTimeout(temporizador);
        temporizador = setTimeout(function () {
            if (hayCambios()) { guardar(false); }
        }, PAUSA);
    }

    // ----- Cerrar: ✕, Esc (tablero.js pulsa la ✕) o clic fuera -----

    function nuevaConDatos() {
        return ['titulo', 'caso', 'descripcion', 'etiquetas', 'codigo'].some(function (n) { return valor(n).trim() !== ''; });
    }

    function cerrar() {
        if (cerrando) { return; }
        if (!editando) {
            if (nuevaConDatos() && !window.confirm('¿Descartar la tarjeta nueva? No se ha creado todavía.')) { return; }
            cerrando = true;
            window.__doPostBack(cfg.cerrarUnico, '');
            return;
        }
        cerrando = true;
        guardar(false).then(function (error) {
            if (error && !window.confirm('No se han podido guardar los últimos cambios:\n' + error + '\n\n¿Cerrar igualmente?')) {
                cerrando = false;
                return;
            }
            window.__doPostBack(cfg.cerrarUnico, '');
        });
    }

    // La ✕ (y Esc, que la pulsa) pasan por cerrar() para guardar antes lo pendiente.
    cerrarBoton.addEventListener('click', function (e) {
        e.preventDefault();
        cerrar();
    });

    // Clic fuera: solo si el botón se pulsa y se suelta sobre el fondo (no al acabar de seleccionar texto fuera).
    var pulsadoEnFondo = false;
    fondo.addEventListener('mousedown', function (e) { pulsadoEnFondo = e.target === fondo; });
    fondo.addEventListener('click', function (e) {
        if (pulsadoEnFondo && e.target === fondo) { cerrar(); }
        pulsadoEnFondo = false;
    });

    // ----- Subtareas: marcar/desmarcar sin recargar -----
    var progreso = document.getElementById('progresoSubtareas');

    function actualizarProgreso() {
        if (!progreso) { return; }
        var casillas = fondo.querySelectorAll('.subtarea input[type=checkbox]');
        var hechas = Array.prototype.filter.call(casillas, function (c) { return c.checked; }).length;
        progreso.textContent = casillas.length ? hechas + '/' + casillas.length : '';
    }

    Array.prototype.forEach.call(fondo.querySelectorAll('.subtarea[data-subtarea-id]'), function (fila) {
        var casilla = fila.querySelector('input[type=checkbox]');
        if (!casilla) { return; }
        // Quita el AutoPostBack (onclick="setTimeout('__doPostBack(...)')"): sin JS se sigue usando.
        casilla.removeAttribute('onclick');
        casilla.addEventListener('change', function () {
            var marcada = casilla.checked;
            fila.classList.toggle('completada', marcada);
            actualizarProgreso();
            if (!editando) { return; }   // al crear, el servidor lo lee en el siguiente envío

            var p = new URLSearchParams();
            p.set('accion', 'subtarea');
            p.set('tablero', String(cfg.tablero));
            p.set('tarjeta', String(cfg.tarjeta));
            p.set('subtarea', fila.getAttribute('data-subtarea-id'));
            p.set('completada', marcada ? '1' : '0');
            enviar(p.toString(), false).then(mostrarGuardado).catch(function (e) {
                // No se ha guardado: se deshace en pantalla.
                casilla.checked = !marcada;
                fila.classList.toggle('completada', !marcada);
                actualizarProgreso();
                mostrar((e && e.message) || 'No se ha podido guardar la subtarea.', 'error');
            });
        });
    });

    // ----- Desplazamiento del detalle tras un postback dentro de él -----
    var CLAVE_SCROLL = 'TaskManager.detalle.scroll';
    var claveDetalle = cfg.tablero + '/' + cfg.tarjeta;
    try {
        var previo = JSON.parse(sessionStorage.getItem(CLAVE_SCROLL) || 'null');
        sessionStorage.removeItem(CLAVE_SCROLL);
        // Solo si el servidor indica que es el mismo detalle (clase sin-animacion).
        if (previo && previo.clave === claveDetalle && fondo.classList.contains('sin-animacion')) {
            fondo.scrollTop = previo.top;
        }
    } catch (e) { /* sessionStorage no disponible: no pasa nada */ }

    window.addEventListener('pagehide', function () {
        if (cerrando) { return; }
        try {
            sessionStorage.setItem(CLAVE_SCROLL, JSON.stringify({ clave: claveDetalle, top: fondo.scrollTop }));
        } catch (e) { /* sin sessionStorage */ }
    });

    if (!editando) { return; }

    // ----- Guardado automático (solo tarjetas existentes) -----
    guardadoValores = cuerpo();
    var botonGuardar = document.getElementById(cfg.guardar);
    if (botonGuardar) { botonGuardar.classList.add('oculto'); }
    mostrar('Los cambios se guardan automáticamente.', '');

    var principal = fondo.querySelector('.modal-principal') || fondo;
    // input/change cubren lo que escribe el usuario; click/keyup, los cambios que hacen los scripts
    // (barra del editor markdown, sugerencias de etiquetas). Si nada cambia, no se envía nada.
    ['input', 'change', 'click', 'keyup'].forEach(function (tipo) {
        principal.addEventListener(tipo, programar);
    });
    // Al salir del campo se guarda sin esperar la pausa.
    principal.addEventListener('focusout', function () {
        if (!cerrando && hayCambios()) { guardar(false); }
    });

    // Otras acciones del detalle recargan la página (subtareas, Eliminar…): guardar lo pendiente al salir.
    window.addEventListener('pagehide', function () {
        if (!cerrando && hayCambios() && !errorLocal()) { guardar(true); }
    });
}());
