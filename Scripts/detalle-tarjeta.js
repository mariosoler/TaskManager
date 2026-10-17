/*
 * Detalle de tarjeta (modal):
 * - Al editar una tarjeta existente, los cambios se guardan solos mientras se escribe
 *   (POST a Tarjeta.ashx tras una breve pausa); el botón Guardar se oculta.
 * - Pulsar fuera del modal lo cierra (como la ✕ o Esc). Antes de cerrar se guarda lo pendiente;
 *   si se está creando una tarjeta con datos escritos, se pide confirmación para descartarla.
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
        enCurso = fetch(cfg.url, {
            method: 'POST',
            credentials: 'same-origin',
            // keepalive deja terminar la petición aunque se recargue la página (máx. 64 KB de cuerpo).
            keepalive: !!alSalir && enviado.length < 60000,
            headers: { 'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8', 'X-Chat-Token': cfg.token },
            body: enviado
        }).then(function (r) {
            return r.json().catch(function () { return {}; }).then(function (datos) {
                if (!r.ok || !datos.ok) { throw new Error(datos.error || 'No se han podido guardar los cambios.'); }
                guardadoValores = enviado;
                if (datos.aviso) {
                    mostrar(datos.aviso, 'error');
                } else {
                    mostrar('✓ Guardado' + (datos.modificacion ? ' · ' + datos.modificacion : ''), 'ok');
                }
                return null;
            });
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
