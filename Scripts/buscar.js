/*
 * Buscador (Buscar.aspx):
 * - «Listas»: desplegable con casillas; el resumen («Todas las listas», el nombre o «N listas») se actualiza al marcar.
 * - «Columnas»: muestra u oculta al momento las columnas de los resultados (atributo data-ocultas de la tabla;
 *   el CSS oculta las celdas data-col) y las recuerda en una cookie, que el servidor lee al cargar la página.
 *   Las casillas viajan con el siguiente envío, así que también se guardan con la búsqueda.
 * - Los desplegables se cierran al pulsar fuera o con Esc.
 * Sin JavaScript las casillas siguen funcionando: las listas al buscar y las columnas con el botón «Aplicar».
 */
(function () {
    'use strict';

    var cfg = window.TaskManagerBuscar;
    if (!cfg) { return; }
    document.documentElement.classList.add('con-js');

    // ----- Listas -----
    var listas = document.getElementById(cfg.listas);
    var resumen = document.getElementById(cfg.resumenListas);

    function resumirListas() {
        if (!listas || !resumen) { return; }
        var casillas = listas.querySelectorAll('input[type=checkbox]');
        if (!casillas.length) { return; }
        var marcadas = Array.prototype.filter.call(casillas, function (c) { return c.checked; });
        if (marcadas.length === 0) {
            resumen.textContent = 'Todas las listas';
        } else if (marcadas.length === 1) {
            var etiqueta = listas.querySelector('label[for="' + marcadas[0].id + '"]');
            resumen.textContent = etiqueta ? etiqueta.textContent : '1 lista';
        } else {
            resumen.textContent = marcadas.length + ' listas';
        }
    }

    if (listas) {
        listas.addEventListener('change', resumirListas);
        // Sin listas (no hay tablero elegido) el desplegable no se abre.
        listas.addEventListener('toggle', function () {
            if (listas.classList.contains('deshabilitado') && listas.open) { listas.open = false; }
        });
    }

    // ----- Columnas -----
    var columnas = document.getElementById(cfg.columnas);
    var tabla = document.getElementById(cfg.tabla);

    function aplicarColumnas() {
        var casillas = columnas.querySelectorAll('input[type=checkbox]');
        var visibles = [], ocultas = [];
        Array.prototype.forEach.call(casillas, function (c) { (c.checked ? visibles : ocultas).push(c.value); });
        if (tabla) { tabla.setAttribute('data-ocultas', ocultas.join(' ')); }
        var valor = visibles.length ? visibles.join('.') : '-';
        document.cookie = cfg.cookie + '=' + valor + '; path=/; max-age=31536000; samesite=lax' +
            (location.protocol === 'https:' ? '; secure' : '');
    }

    if (columnas) { columnas.addEventListener('change', aplicarColumnas); }

    // ----- Cerrar desplegables al pulsar fuera o con Esc -----
    var SELECTOR = 'details.desplegable-checks, details.selector-columnas';

    document.addEventListener('click', function (e) {
        Array.prototype.forEach.call(document.querySelectorAll(SELECTOR), function (d) {
            if (d.open && !d.contains(e.target)) { d.open = false; }
        });
    });
    document.addEventListener('keydown', function (e) {
        if (e.key !== 'Escape') { return; }
        Array.prototype.forEach.call(document.querySelectorAll(SELECTOR), function (d) {
            if (!d.open) { return; }
            d.open = false;
            var s = d.querySelector('summary');
            if (s) { s.focus(); }
        });
    });
}());
