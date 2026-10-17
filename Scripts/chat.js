/*
 * Chat de la tarjeta abierta (panel derecho del detalle).
 * - Envía con fetch a Chat.ashx sin recargar la página (Enter envía, Mayús+Enter salta de línea).
 * - Mientras el detalle está abierto pide los mensajes nuevos cada pocos segundos.
 * - Si el envío por AJAX falla, recurre al botón "Enviar" normal (postback).
 * Los textos de los mensajes se insertan con textContent.
 */
(function () {
    'use strict';

    var cfg = window.TaskManagerChat;
    var lista = document.getElementById('chatMensajes');
    if (!cfg || !lista || !cfg.tarjeta) { return; }

    var texto = document.getElementById(cfg.texto);
    var boton = document.getElementById(cfg.boton);
    var aviso = document.getElementById('chatAviso');
    var contador = document.getElementById('chatContador');

    var INTERVALO = 4000;
    var MAX_FALLOS = 3;
    var temporizador = null;
    var enviando = false;
    var fallos = 0;
    var usarPostback = false;

    function mensajes() { return lista.querySelectorAll('.chat-mensaje'); }

    function ultimoId() {
        var m = mensajes();
        return m.length ? parseInt(m[m.length - 1].getAttribute('data-id'), 10) : 0;
    }

    function irAlFinal() { lista.scrollTop = lista.scrollHeight; }

    function cercaDelFinal() { return lista.scrollHeight - lista.scrollTop - lista.clientHeight < 80; }

    function actualizarContador() {
        var n = mensajes().length;
        contador.textContent = n === 0 ? '' : (n === 1 ? '1 mensaje' : n + ' mensajes');
    }

    function mostrarAviso(textoAviso) {
        aviso.textContent = textoAviso || '';
        aviso.hidden = !textoAviso;
    }

    function el(etiqueta, clase, contenido) {
        var e = document.createElement(etiqueta);
        if (clase) { e.className = clase; }
        if (contenido !== undefined) { e.textContent = contenido; }
        return e;
    }

    function pintar(m) {
        if (lista.querySelector('.chat-mensaje[data-id="' + Number(m.id) + '"]')) { return; }   // ya está
        var fila = el('div', 'chat-mensaje' + (m.propio ? ' propio' : ''));
        fila.setAttribute('data-id', String(Number(m.id)));
        var avatar = el('span', 'avatar avatar-pequeno', m.iniciales);
        avatar.title = m.autor;
        var burbuja = el('div', 'chat-burbuja');
        var meta = el('div', 'chat-meta');
        meta.appendChild(el('strong', null, m.autor));
        meta.appendChild(document.createTextNode(' '));
        meta.appendChild(el('span', 'fecha', m.fecha));
        burbuja.appendChild(meta);
        burbuja.appendChild(el('p', null, m.texto));
        fila.appendChild(avatar);
        fila.appendChild(burbuja);
        lista.appendChild(fila);
    }

    function recibir(datos, forzarFinal) {
        var seguirAbajo = forzarFinal || cercaDelFinal();
        (datos.mensajes || []).forEach(pintar);
        if (seguirAbajo) { irAlFinal(); }
        actualizarContador();
    }

    // Respuesta JSON o error con el mensaje del servidor (si la sesión caducó llega HTML del login).
    function leerJson(respuesta) {
        var tipo = respuesta.headers.get('Content-Type') || '';
        if (tipo.indexOf('application/json') < 0) {
            return Promise.reject(new Error('Tu sesión ha caducado. Recarga la página.'));
        }
        return respuesta.json().then(function (datos) {
            if (!respuesta.ok) { throw new Error(datos.error || 'No se ha podido conectar con el chat.'); }
            return datos;
        });
    }

    function parametros(extra) {
        var p = new URLSearchParams({ tablero: cfg.tablero, tarjeta: cfg.tarjeta, desde: ultimoId() });
        Object.keys(extra || {}).forEach(function (k) { p.append(k, extra[k]); });
        return p;
    }

    function programar() {
        clearTimeout(temporizador);
        if (fallos < MAX_FALLOS) { temporizador = setTimeout(consultar, INTERVALO); }
    }

    function consultar() {
        if (document.hidden || enviando) { programar(); return; }
        fetch(cfg.url + '?' + parametros().toString(), { credentials: 'same-origin', headers: { 'Accept': 'application/json' } })
            .then(leerJson)
            .then(function (datos) { fallos = 0; mostrarAviso(''); recibir(datos, false); })
            .catch(function (error) {
                fallos++;
                if (fallos >= MAX_FALLOS) { mostrarAviso(error.message + ' (el chat ha dejado de actualizarse)'); }
            })
            .then(programar);
    }

    function enviar() {
        var valor = texto.value.trim();
        if (!valor || enviando) { return; }
        enviando = true;
        boton.disabled = true;
        fetch(cfg.url, {
            method: 'POST',
            credentials: 'same-origin',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded;charset=UTF-8',
                'Accept': 'application/json',
                'X-Chat-Token': cfg.token
            },
            body: parametros({ texto: valor }).toString()
        })
            .then(leerJson)
            .then(function (datos) {
                texto.value = '';
                fallos = 0;
                mostrarAviso('');
                recibir(datos, true);
                enviando = false;
                boton.disabled = false;
                texto.focus();
                programar();
            })
            .catch(function () {
                // Plan B: envío normal del formulario (el servidor guarda el mensaje igualmente).
                enviando = false;
                boton.disabled = false;
                usarPostback = true;
                boton.click();
            });
    }

    boton.addEventListener('click', function (e) {
        if (usarPostback) { return; }
        e.preventDefault();
        enviar();
    });

    texto.addEventListener('keydown', function (e) {
        if (e.key === 'Enter' && !e.shiftKey && !e.isComposing) {
            e.preventDefault();
            enviar();
        }
    });

    document.addEventListener('visibilitychange', function () {
        if (!document.hidden) { consultar(); }
    });

    actualizarContador();
    irAlFinal();
    programar();
}());
