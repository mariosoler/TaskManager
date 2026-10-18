/*
 * Subtareas enlazadas a otra tarjeta del tablero (detalle de tarjeta):
 * - el botón «🔗 Enlazar tarjeta», junto a «Añadir», cambia el cuadro de nueva subtarea a modo búsqueda;
 * - al escribir sugiere tarjetas del tablero por título, nº de caso o lista (varias palabras = todas; sin
 *   distinguir mayúsculas ni acentos); ↑/↓ eligen, Enter o clic enlazan, Esc vuelve a la subtarea normal;
 * - enlazar pone el Id en el HiddenField y pulsa «Añadir» (postback normal: el servidor comprueba que la tarjeta
 *   es de este tablero y usa su título). La subtarea muestra el título actual de la tarjeta, subrayado.
 * Datos en <script type="application/json" id="tarjetas-enlazables"> (TableroPagina.TarjetasEnlazablesJson).
 * Los textos se insertan con textContent. Sin JavaScript solo se pueden añadir subtareas normales.
 */
(function () {
    'use strict';

    var cfg = window.TaskManagerEnlaceSubtarea;
    if (!cfg) { return; }
    var campo = document.getElementById(cfg.campo);
    var boton = document.getElementById(cfg.boton);
    var enlace = document.getElementById(cfg.enlace);
    var nodoDatos = document.getElementById(cfg.datos);
    if (!campo || !boton || !enlace || !nodoDatos) { return; }

    var tarjetas = [];
    try { tarjetas = JSON.parse(nodoDatos.textContent) || []; } catch (e) { tarjetas = []; }
    if (tarjetas.length === 0) { return; }   // no hay otras tarjetas que enlazar

    var MAXIMO = 8;
    var placeholderNormal = campo.placeholder;
    var buscando = false;
    var opciones = [];
    var activa = -1;

    // Sin mayúsculas ni acentos (los acentos se separan con NFD y se quitan las marcas).
    function normalizar(texto) {
        return String(texto || '').normalize('NFD').replace(/\p{M}/gu, '').toLocaleLowerCase();
    }
    tarjetas.forEach(function (t) { t.buscable = normalizar(t.t + ' ' + t.c + ' ' + t.l); });

    // ----- Elementos -----
    var enlazar = document.createElement('button');
    enlazar.type = 'button';
    enlazar.className = 'boton boton-claro subtarea-enlazar';
    enlazar.textContent = '🔗 Enlazar tarjeta';
    enlazar.setAttribute('aria-pressed', 'false');
    boton.parentNode.insertBefore(enlazar, boton.nextSibling);

    var lista = document.createElement('ul');
    lista.id = campo.id + '_tarjetas';
    lista.className = 'subtarea-sugerencias oculto';
    lista.setAttribute('role', 'listbox');
    lista.setAttribute('aria-label', 'Tarjetas del tablero');
    boton.parentNode.appendChild(lista);

    function cerrar() {
        lista.classList.add('oculto');
        lista.textContent = '';
        opciones = [];
        activa = -1;
        campo.setAttribute('aria-expanded', 'false');
        campo.removeAttribute('aria-activedescendant');
    }

    function modoBusqueda(activo) {
        buscando = activo;
        enlace.value = '';
        campo.value = '';
        enlazar.setAttribute('aria-pressed', activo ? 'true' : 'false');
        enlazar.classList.toggle('activo', activo);
        boton.classList.toggle('oculto', activo);   // en modo búsqueda no se añade texto libre
        campo.placeholder = activo ? 'Busca la tarjeta por título, nº de caso o lista…' : placeholderNormal;
        if (activo) {
            campo.setAttribute('role', 'combobox');
            campo.setAttribute('aria-autocomplete', 'list');
            campo.setAttribute('aria-controls', lista.id);
            sugerir();
        } else {
            cerrar();
            ['role', 'aria-autocomplete', 'aria-controls', 'aria-expanded'].forEach(function (a) { campo.removeAttribute(a); });
        }
        campo.focus();
    }

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
        var palabras = normalizar(campo.value).replace(/^#/, '').split(/\s+/).filter(Boolean);
        opciones = tarjetas.filter(function (t) {
            return palabras.every(function (p) { return t.buscable.indexOf(p) >= 0; });
        }).slice(0, MAXIMO);

        lista.textContent = '';
        if (opciones.length === 0) {
            var vacia = document.createElement('li');
            vacia.className = 'vacia';
            vacia.textContent = 'Ninguna tarjeta coincide.';
            lista.appendChild(vacia);
        }
        opciones.forEach(function (t, i) {
            var li = document.createElement('li');
            li.id = lista.id + '_' + i;
            li.setAttribute('role', 'option');
            var titulo = document.createElement('span');
            titulo.className = 'subtarea-sugerencia-titulo';
            titulo.textContent = t.t;
            var nota = document.createElement('small');
            nota.textContent = (t.c ? '#' + t.c + ' · ' : '') + t.l;
            li.appendChild(titulo);
            li.appendChild(nota);
            li.addEventListener('mousedown', function (e) {
                e.preventDefault();   // que el cuadro no pierda el foco
                elegir(i);
            });
            lista.appendChild(li);
        });
        lista.classList.remove('oculto');
        campo.setAttribute('aria-expanded', 'true');
        resaltar(opciones.length ? 0 : -1);
    }

    function elegir(indice) {
        var t = opciones[indice];
        if (!t) { return; }
        enlace.value = String(t.i);
        campo.value = t.t;
        cerrar();
        boton.click();
    }

    enlazar.addEventListener('click', function () { modoBusqueda(!buscando); });
    campo.addEventListener('input', function () {
        if (buscando) { sugerir(); }
    });
    campo.addEventListener('focus', function () {
        if (buscando && lista.classList.contains('oculto')) { sugerir(); }
    });
    campo.addEventListener('blur', function () { setTimeout(cerrar, 100); });
    campo.addEventListener('keydown', function (e) {
        if (!buscando) { return; }
        if (e.key === 'ArrowDown' && opciones.length) {
            e.preventDefault();
            if (lista.classList.contains('oculto')) { sugerir(); } else { resaltar((activa + 1) % opciones.length); }
        } else if (e.key === 'ArrowUp' && opciones.length) {
            e.preventDefault();
            resaltar((activa - 1 + opciones.length) % opciones.length);
        } else if (e.key === 'Enter') {
            e.preventDefault();       // nunca añade el texto de búsqueda como subtarea
            if (activa >= 0) { elegir(activa); }
        } else if (e.key === 'Escape') {
            e.preventDefault();
            e.stopPropagation();      // vuelve a la subtarea normal, no cierra el detalle
            modoBusqueda(false);
        }
    });
}());
