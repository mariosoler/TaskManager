/*
 * Buscador (Buscar.aspx):
 * - «Listas»: desplegable con casillas; el resumen («Todas las listas», el nombre o «N listas») se actualiza al marcar.
 * - «Columnas»: muestra u oculta al momento las columnas de los resultados (atributo data-ocultas de la tabla;
 *   el CSS oculta las celdas data-col) y las recuerda en una cookie, que el servidor lee al cargar la página.
 *   Las casillas viajan con el siguiente envío, así que también se guardan con la búsqueda.
 * - «Copiar en portapapeles»: copia las columnas visibles de los resultados (valores de data-copia de cada fila,
 *   columnas en #copia-columnas) como HTML y como texto con tabuladores, listos para pegar en Excel.
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

    // ----- Copiar en portapapeles (columnas visibles, lista para pegar en Excel) -----
    var copiar = document.getElementById(cfg.copiar);
    var filas = tabla && tabla.tBodies.length ? Array.prototype.slice.call(tabla.tBodies[0].rows) : [];
    var columnasCopia = [];
    try { columnasCopia = JSON.parse(document.getElementById(cfg.copiaColumnas).textContent) || []; } catch (e) { columnasCopia = []; }

    function escaparHtml(texto) {
        return texto.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
    }

    // Texto plano (valores separados por tabuladores): sin saltos de línea dentro de una celda y, en las columnas de
    // texto, con apóstrofo delante si empieza como una fórmula (Excel lo toma como texto y no lo muestra).
    function celdaTexto(valor, esTexto) {
        var v = String(valor == null ? '' : valor).replace(/[\t\r\n]+/g, ' ');
        return esTexto && /^[=+\-@]/.test(v) ? "'" + v : v;
    }

    // HTML: Excel respeta mso-number-format "@" (texto) al pegar: conserva los ceros del caso y nunca evalúa fórmulas.
    function celdaHtml(etiqueta, valor, esTexto) {
        return '<' + etiqueta + (esTexto ? ' style="mso-number-format:\'\\@\'"' : '') + '>' +
            escaparHtml(String(valor == null ? '' : valor)) + '</' + etiqueta + '>';
    }

    function contenidoCopia() {
        var ocultas = (tabla.getAttribute('data-ocultas') || '').split(/\s+/).filter(Boolean);
        // El título siempre; el resto, si su columna está visible.
        var indices = [];
        columnasCopia.forEach(function (c, i) {
            if (c.k === 'titulo' || ocultas.indexOf(c.k) < 0) { indices.push(i); }
        });
        var datos = filas.map(function (tr) {
            try { return JSON.parse(tr.getAttribute('data-copia') || '[]'); } catch (e) { return []; }
        });

        var texto = [indices.map(function (i) { return celdaTexto(columnasCopia[i].n, true); }).join('\t')];
        var html = ['<tr>' + indices.map(function (i) { return celdaHtml('th', columnasCopia[i].n, true); }).join('') + '</tr>'];
        datos.forEach(function (valores) {
            texto.push(indices.map(function (i) { return celdaTexto(valores[i], columnasCopia[i].t); }).join('\t'));
            html.push('<tr>' + indices.map(function (i) { return celdaHtml('td', valores[i], columnasCopia[i].t); }).join('') + '</tr>');
        });
        return {
            texto: texto.join('\r\n'),
            html: '<html><head><meta charset="utf-8"></head><body><table>' + html.join('') + '</table></body></html>'
        };
    }

    // Sin la API del portapapeles (p. ej. por http://): evento copy con los dos formatos.
    function copiarConEvento(contenido) {
        var alCopiar = function (e) {
            e.clipboardData.setData('text/plain', contenido.texto);
            e.clipboardData.setData('text/html', contenido.html);
            e.preventDefault();
        };
        document.addEventListener('copy', alCopiar);
        try {
            return document.execCommand('copy') ? Promise.resolve() : Promise.reject(new Error('copy'));
        } finally {
            document.removeEventListener('copy', alCopiar);
        }
    }

    var textoCopiar = copiar ? copiar.textContent : '';
    var temporizadorCopia = null;

    function avisoCopia(mensaje) {
        copiar.textContent = mensaje;
        clearTimeout(temporizadorCopia);
        temporizadorCopia = setTimeout(function () { copiar.textContent = textoCopiar; }, 2500);
    }

    if (copiar && filas.length && columnasCopia.length) {
        copiar.hidden = false;
        copiar.setAttribute('aria-live', 'polite');
        copiar.addEventListener('click', function () {
            var contenido = contenidoCopia();
            var promesa;
            if (navigator.clipboard && navigator.clipboard.write && window.ClipboardItem) {
                promesa = navigator.clipboard.write([new ClipboardItem({
                    'text/plain': new Blob([contenido.texto], { type: 'text/plain' }),
                    'text/html': new Blob([contenido.html], { type: 'text/html' })
                })]).catch(function () { return copiarConEvento(contenido); });
            } else {
                promesa = copiarConEvento(contenido);
            }
            promesa.then(function () {
                avisoCopia('✓ Copiadas ' + filas.length + (filas.length === 1 ? ' tarea' : ' tareas'));
            }, function () {
                avisoCopia('No se ha podido copiar');
            });
        });
    }

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
