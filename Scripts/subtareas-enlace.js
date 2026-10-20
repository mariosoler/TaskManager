/*
 * Subtareas enlazadas a otra tarjeta del tablero (detalle de tarjeta):
 * - el botón «🔗 Enlazar tarjeta», junto a «Añadir», cambia el cuadro de nueva subtarea a modo búsqueda;
 * - al escribir sugiere tarjetas del tablero por ID, título, nº de caso o lista (varias palabras = todas; sin
 *   distinguir mayúsculas ni acentos; si un número es justo el ID de una tarjeta, esa sale la primera); ↑/↓ eligen, Enter o clic enlazan, Esc vuelve a la subtarea normal;
 * - enlazar pone el Id en el HiddenField y pulsa «Añadir» (postback normal: el servidor comprueba que la tarjeta
 *   es de este tablero y usa su título). La subtarea muestra el título actual de la tarjeta, subrayado.
 * Datos en <script type="application/json" id="tarjetas-enlazables"> (TableroPagina.TarjetasEnlazablesJson);
 * la búsqueda es la de buscador-tarjetas.js (compartida con los enlaces del chat).
 * Los textos se insertan con textContent. Sin JavaScript solo se pueden añadir subtareas normales.
 */
(function () {
    'use strict';

    var cfg = window.TaskManagerEnlaceSubtarea;
    var buscador = window.TaskManagerBuscadorTarjetas;
    if (!cfg || !buscador) { return; }
    var campo = document.getElementById(cfg.campo);
    var boton = document.getElementById(cfg.boton);
    var enlace = document.getElementById(cfg.enlace);
    if (!campo || !boton || !enlace) { return; }

    var tarjetas = buscador.cargar(cfg.datos);
    if (tarjetas.length === 0) { return; }   // no hay otras tarjetas que enlazar

    var MAXIMO = 8;
    var placeholderNormal = campo.placeholder;
    var buscando = false;
    var opciones = [];
    var activa = -1;

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
        campo.placeholder = activo ? 'Busca la tarjeta por ID, título, nº de caso o lista…' : placeholderNormal;
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
