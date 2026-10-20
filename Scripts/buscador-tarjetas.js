/*
 * Búsqueda de tarjetas del tablero, compartida por los buscadores para enlazar una tarjeta:
 * subtareas enlazadas (subtareas-enlace.js) y enlaces en el chat (chat-enlace.js).
 * - Busca por ID, título, nº de caso o lista; varias palabras = todas; sin distinguir mayúsculas ni acentos;
 *   si un número es justo el ID de una tarjeta, esa sale la primera.
 * - Datos en <script type="application/json" id="tarjetas-enlazables"> (TableroPagina.TarjetasEnlazablesJson):
 *   i = Id, t = título, c = caso, l = lista.
 * Los textos se insertan con textContent.
 */
(function () {
    'use strict';

    // Sin mayúsculas ni acentos (los acentos se separan con NFD y se quitan las marcas).
    function normalizar(texto) {
        return String(texto || '').normalize('NFD').replace(/\p{M}/gu, '').toLocaleLowerCase();
    }

    /* Tarjetas del nodo JSON indicado, con el texto en el que se busca ya preparado ([] si no hay). */
    function cargar(idNodo) {
        var nodo = document.getElementById(idNodo);
        var tarjetas = [];
        if (nodo) {
            try { tarjetas = JSON.parse(nodo.textContent) || []; } catch (e) { tarjetas = []; }
        }
        tarjetas.forEach(function (t) { t.buscable = normalizar(t.i + ' ' + t.t + ' ' + t.c + ' ' + t.l); });
        return tarjetas;
    }

    /* Hasta «maximo» tarjetas que contienen todas las palabras escritas; la del ID exacto, la primera. */
    function filtrar(tarjetas, texto, maximo) {
        var palabras = normalizar(texto).replace(/^#/, '').split(/\s+/).filter(Boolean);
        var coinciden = tarjetas.filter(function (t) {
            return palabras.every(function (p) { return t.buscable.indexOf(p) >= 0; });
        });
        var exacta = coinciden.filter(function (t) { return palabras.indexOf(String(t.i)) >= 0; });
        return exacta.concat(coinciden.filter(function (t) { return exacta.indexOf(t) < 0; })).slice(0, maximo);
    }

    /* Opción de la lista de sugerencias: título y, a la derecha, «ID n · #caso · lista». */
    function crearOpcion(t, id) {
        var li = document.createElement('li');
        li.id = id;
        li.setAttribute('role', 'option');
        var titulo = document.createElement('span');
        titulo.className = 'subtarea-sugerencia-titulo';
        titulo.textContent = t.t;
        var nota = document.createElement('small');
        nota.textContent = 'ID ' + t.i + ' · ' + (t.c ? '#' + t.c + ' · ' : '') + t.l;
        li.appendChild(titulo);
        li.appendChild(nota);
        return li;
    }

    /* Opción «Ninguna tarjeta coincide.» */
    function crearVacia() {
        var vacia = document.createElement('li');
        vacia.className = 'vacia';
        vacia.textContent = 'Ninguna tarjeta coincide.';
        return vacia;
    }

    window.TaskManagerBuscadorTarjetas = {
        normalizar: normalizar,
        cargar: cargar,
        filtrar: filtrar,
        crearOpcion: crearOpcion,
        crearVacia: crearVacia
    };
}());
