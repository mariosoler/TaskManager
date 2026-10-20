/*
 * Enlazar una tarjeta en el mensaje del chat (detalle de tarjeta):
 * - el botón de enlace, encima de «Enviar», abre un pequeño buscador de tarjetas del tablero (el mismo que el de
 *   las subtareas enlazadas: buscador-tarjetas.js);
 * - ↑/↓ eligen, Enter o clic insertan [#ID] donde estaba el cursor del mensaje; Esc o pulsar fuera lo cierran;
 * - al mostrar el mensaje, [#ID] se convierte en un enlace con el ID y el título actual de la tarjeta
 *   (TableroPagina.HtmlMensaje en el servidor y Scripts/chat.js con los mensajes nuevos).
 * Los textos se insertan con textContent. Sin JavaScript se puede escribir [#ID] a mano.
 */
(function () {
    'use strict';

    var cfg = window.TaskManagerChat;
    var buscador = window.TaskManagerBuscadorTarjetas;
    if (!cfg || !cfg.tarjeta || !buscador) { return; }
    var texto = document.getElementById(cfg.texto);
    var boton = document.getElementById(cfg.boton);
    var escribir = texto && texto.closest('.chat-escribir');
    if (!texto || !boton || !escribir) { return; }

    var tarjetas = buscador.cargar(cfg.datos);
    if (tarjetas.length === 0) { return; }   // no hay otras tarjetas que enlazar

    var MAXIMO = 8;
    var SVG = 'http://www.w3.org/2000/svg';
    var opciones = [];
    var activa = -1;
    var inicio = 0, fin = 0;   // selección del mensaje al abrir el buscador

    // ----- Elementos -----
    var enlazar = document.createElement('button');
    enlazar.type = 'button';
    enlazar.className = 'chat-enlazar';
    enlazar.title = 'Enlazar una tarjeta en el mensaje';
    enlazar.setAttribute('aria-label', 'Enlazar una tarjeta en el mensaje');
    enlazar.setAttribute('aria-expanded', 'false');
    enlazar.setAttribute('aria-controls', 'chatEnlacePanel');
    // Icono de enlace de línea (color del texto), como el clip de los adjuntos.
    var icono = document.createElementNS(SVG, 'svg');
    icono.setAttribute('viewBox', '0 0 24 24');
    icono.setAttribute('aria-hidden', 'true');
    icono.setAttribute('focusable', 'false');
    icono.setAttribute('class', 'icono-enlace');
    ['M10 13a5 5 0 0 0 7.5.5l3-3a5 5 0 0 0-7-7l-1.7 1.7', 'M14 11a5 5 0 0 0-7.5-.5l-3 3a5 5 0 0 0 7 7l1.7-1.7'].forEach(function (d) {
        var trazo = document.createElementNS(SVG, 'path');
        trazo.setAttribute('d', d);
        icono.appendChild(trazo);
    });
    enlazar.appendChild(icono);
    boton.parentNode.insertBefore(enlazar, boton);

    var panel = document.createElement('div');
    panel.id = 'chatEnlacePanel';
    panel.className = 'chat-enlace-panel';
    panel.hidden = true;
    var campo = document.createElement('input');
    campo.type = 'text';
    campo.autocomplete = 'off';
    campo.spellcheck = false;
    campo.placeholder = 'Busca la tarjeta por ID, título, nº de caso o lista…';
    campo.setAttribute('aria-label', 'Buscar la tarjeta que se enlaza');
    campo.setAttribute('role', 'combobox');
    campo.setAttribute('aria-autocomplete', 'list');
    campo.setAttribute('aria-controls', 'chatEnlaceSugerencias');
    campo.setAttribute('aria-expanded', 'true');
    var lista = document.createElement('ul');
    lista.id = 'chatEnlaceSugerencias';
    lista.className = 'subtarea-sugerencias chat-enlace-sugerencias';
    lista.setAttribute('role', 'listbox');
    lista.setAttribute('aria-label', 'Tarjetas del tablero');
    panel.appendChild(campo);
    panel.appendChild(lista);
    escribir.appendChild(panel);

    // ----- Abrir y cerrar -----
    function abierto() { return !panel.hidden; }

    function abrir() {
        inicio = texto.selectionStart;
        fin = texto.selectionEnd;
        panel.hidden = false;
        enlazar.classList.add('activo');
        enlazar.setAttribute('aria-expanded', 'true');
        campo.value = '';
        sugerir();
        campo.focus();
    }

    function cerrar(volverAlMensaje) {
        if (!abierto()) { return; }
        panel.hidden = true;
        lista.textContent = '';
        opciones = [];
        activa = -1;
        campo.removeAttribute('aria-activedescendant');
        enlazar.classList.remove('activo');
        enlazar.setAttribute('aria-expanded', 'false');
        if (volverAlMensaje) {
            texto.focus();
            texto.setSelectionRange(inicio, fin);
        }
    }

    // ----- Sugerencias -----
    function resaltar(indice) {
        activa = indice;
        Array.prototype.forEach.call(lista.children, function (li, i) {
            li.setAttribute('aria-selected', i === indice ? 'true' : 'false');
            li.classList.toggle('activa', i === indice);
        });
        if (indice >= 0 && lista.children[indice]) {
            campo.setAttribute('aria-activedescendant', lista.children[indice].id);
            if (lista.children[indice].scrollIntoView) { lista.children[indice].scrollIntoView({ block: 'nearest' }); }
        } else {
            campo.removeAttribute('aria-activedescendant');
        }
    }

    function sugerir() {
        opciones = buscador.filtrar(tarjetas, campo.value, MAXIMO);
        lista.textContent = '';
        if (opciones.length === 0) { lista.appendChild(buscador.crearVacia()); }
        opciones.forEach(function (t, i) {
            var li = buscador.crearOpcion(t, lista.id + '_' + i);
            li.addEventListener('mousedown', function (e) {
                e.preventDefault();   // que el cuadro no pierda el foco
                elegir(i);
            });
            lista.appendChild(li);
        });
        resaltar(opciones.length ? 0 : -1);
    }

    /* Inserta [#ID] en el mensaje, donde estaba el cursor (separado por espacios del texto de alrededor). */
    function elegir(indice) {
        var t = opciones[indice];
        if (!t) { return; }
        var antes = texto.value.slice(0, inicio);
        var despues = texto.value.slice(fin);
        var insercion = '[#' + t.i + ']';
        if (antes && !/\s$/.test(antes)) { insercion = ' ' + insercion; }
        if (!/^\s/.test(despues)) { insercion += ' '; }
        texto.value = antes + insercion + despues;
        inicio = fin = antes.length + insercion.length;
        cerrar(true);
        texto.dispatchEvent(new Event('input', { bubbles: true }));
    }

    // ----- Eventos -----
    // mousedown sin foco: si el buscador está abierto, el cuadro no lo cierra antes de que el clic lo alterne.
    enlazar.addEventListener('mousedown', function (e) { e.preventDefault(); });
    enlazar.addEventListener('click', function () {
        if (abierto()) { cerrar(true); } else { abrir(); }
    });
    campo.addEventListener('input', sugerir);
    campo.addEventListener('blur', function () { setTimeout(function () { cerrar(false); }, 100); });
    campo.addEventListener('keydown', function (e) {
        if (e.key === 'ArrowDown' && opciones.length) {
            e.preventDefault();
            resaltar((activa + 1) % opciones.length);
        } else if (e.key === 'ArrowUp' && opciones.length) {
            e.preventDefault();
            resaltar((activa - 1 + opciones.length) % opciones.length);
        } else if (e.key === 'Enter') {
            e.preventDefault();       // nunca envía el formulario
            if (activa >= 0) { elegir(activa); }
        } else if (e.key === 'Escape') {
            e.preventDefault();
            e.stopPropagation();      // cierra el buscador, no el detalle
            cerrar(true);
        }
    });
}());
