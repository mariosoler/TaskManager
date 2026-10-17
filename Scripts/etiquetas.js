/*
 * Cuadro de etiquetas del detalle de tarjeta (#PLANIFICADOR_CABINAS #urgente):
 * - al escribir "#" sugiere las etiquetas ya creadas en este tablero (las más usadas primero; las etiquetas
 *   no se comparten entre tableros); flechas para elegir, Enter/Tab para insertar, Esc para cerrar;
 * - debajo muestra cómo quedarán, con su color (las nuevas, punteadas: el color se asigna al guardar).
 * Datos en <script type="application/json" id="etiquetas-datos"> (ver TableroPagina.EtiquetasJson).
 * Con cfg.modo = 'filtro' (buscador) los nombres que no existen se marcan como «no existe» en vez de «nueva».
 * Los textos se insertan con textContent y los colores se validan antes de usarlos.
 */
(function () {
    'use strict';

    var cfg = window.TaskManagerEtiquetas;
    if (!cfg) { return; }
    var campo = document.getElementById(cfg.campo);
    var nodoDatos = document.getElementById(cfg.datos);
    if (!campo || !nodoDatos) { return; }   // el detalle de tarjeta está cerrado

    var esFiltro = cfg.modo === 'filtro';
    var datos = [];
    try { datos = JSON.parse(nodoDatos.textContent) || []; } catch (e) { datos = []; }

    var MAXIMO_SUGERENCIAS = 8;
    var NOMBRE = /^[\p{L}\p{N}_-]{1,50}$/u;
    var COLOR = /^#[0-9a-f]{6}$/i;
    // Trozo que se está escribiendo justo antes del cursor, si empieza por "#".
    var TROZO_ACTUAL = /(^|[\s,;])#([\p{L}\p{N}_-]*)$/u;

    var porNombre = {};
    datos.forEach(function (d) { porNombre[d.n.toLocaleLowerCase()] = d; });

    // ----- Elementos -----
    var lista = document.createElement('ul');
    lista.id = campo.id + '_sugerencias';
    lista.className = 'etiquetas-sugerencias oculto';
    lista.setAttribute('role', 'listbox');
    lista.setAttribute('aria-label', 'Etiquetas sugeridas');

    var vista = document.createElement('div');
    vista.className = 'etiquetas-vista';

    campo.setAttribute('role', 'combobox');
    campo.setAttribute('aria-autocomplete', 'list');
    campo.setAttribute('aria-expanded', 'false');
    campo.setAttribute('aria-controls', lista.id);
    campo.parentNode.insertBefore(lista, campo.nextSibling);
    campo.parentNode.insertBefore(vista, lista.nextSibling);

    var opciones = [];     // sugerencias visibles
    var activa = -1;       // índice de la opción resaltada

    function chip(texto, dato, clase) {
        var span = document.createElement('span');
        span.className = 'etiqueta-tag' + (clase ? ' ' + clase : '');
        span.textContent = texto;
        if (dato && COLOR.test(dato.c) && COLOR.test(dato.t)) {
            span.style.backgroundColor = dato.c;
            span.style.color = dato.t;
        }
        return span;
    }

    function trozos() {
        return campo.value.split(/[\s,;]+/).map(function (t) { return t.replace(/^#+/, ''); }).filter(Boolean);
    }

    // ----- Vista previa -----
    function pintarVista() {
        vista.textContent = '';
        var vistos = {};
        trozos().forEach(function (nombre) {
            var clave = nombre.toLocaleLowerCase();
            if (vistos[clave]) { return; }
            vistos[clave] = true;
            if (!NOMBRE.test(nombre)) {
                vista.appendChild(chip(nombre, null, 'invalida')).title = 'No válida: solo letras, números, _ y -';
                return;
            }
            var dato = porNombre[clave];
            if (dato) {
                vista.appendChild(chip(dato.n, dato));
            } else if (esFiltro) {
                vista.appendChild(chip(nombre, null, 'desconocida')).title = 'No hay ninguna etiqueta con ese nombre';
            } else {
                vista.appendChild(chip(nombre, null, 'nueva')).title = 'Nueva en este tablero: el color se asigna al guardar';
            }
        });
    }

    // ----- Sugerencias -----
    function trozoActual() {
        var antes = campo.value.slice(0, campo.selectionStart || 0);
        var m = TROZO_ACTUAL.exec(antes);
        return m ? { inicio: antes.length - m[2].length - 1, texto: m[2] } : null;
    }

    function cerrar() {
        lista.classList.add('oculto');
        lista.textContent = '';
        opciones = [];
        activa = -1;
        campo.setAttribute('aria-expanded', 'false');
        campo.removeAttribute('aria-activedescendant');
    }

    function resaltar(indice) {
        activa = indice;
        Array.prototype.forEach.call(lista.children, function (li, i) {
            li.setAttribute('aria-selected', i === indice ? 'true' : 'false');
            li.classList.toggle('activa', i === indice);
        });
        if (indice >= 0) {
            campo.setAttribute('aria-activedescendant', lista.children[indice].id);
            lista.children[indice].scrollIntoView({ block: 'nearest' });
        } else {
            campo.removeAttribute('aria-activedescendant');
        }
    }

    function sugerir() {
        var trozo = trozoActual();
        if (!trozo) { cerrar(); return; }

        var buscado = trozo.texto.toLocaleLowerCase();
        var usadas = {};
        trozos().forEach(function (t) { usadas[t.toLocaleLowerCase()] = true; });
        delete usadas[buscado];   // la que se está escribiendo no cuenta como ya puesta

        var empiezan = [], contienen = [];
        datos.forEach(function (d) {
            var n = d.n.toLocaleLowerCase();
            if (usadas[n]) { return; }
            if (n.indexOf(buscado) === 0) { empiezan.push(d); } else if (buscado && n.indexOf(buscado) > 0) { contienen.push(d); }
        });
        opciones = empiezan.concat(contienen).slice(0, MAXIMO_SUGERENCIAS);
        if (opciones.length === 0) { cerrar(); return; }

        lista.textContent = '';
        opciones.forEach(function (d, i) {
            var li = document.createElement('li');
            li.id = lista.id + '_' + i;
            li.setAttribute('role', 'option');
            li.appendChild(chip(d.n, d));
            var nota = document.createElement('small');
            nota.textContent = d.u === 0 ? 'sin usar' : (d.u === 1 ? '1 tarjeta' : d.u + ' tarjetas');
            li.appendChild(nota);
            li.addEventListener('mousedown', function (e) {
                e.preventDefault();   // que el cuadro no pierda el foco
                elegir(i);
            });
            lista.appendChild(li);
        });
        lista.classList.remove('oculto');
        campo.setAttribute('aria-expanded', 'true');
        resaltar(0);
    }

    function elegir(indice) {
        var d = opciones[indice];
        var trozo = trozoActual();
        if (!d || !trozo) { cerrar(); return; }
        var cursor = campo.selectionStart || 0;
        var antes = campo.value.slice(0, trozo.inicio);
        var despues = campo.value.slice(cursor).replace(/^[\p{L}\p{N}_-]*\s*/u, '');
        var insertado = '#' + d.n + ' ';
        campo.value = antes + insertado + despues;
        var posicion = antes.length + insertado.length;
        campo.setSelectionRange(posicion, posicion);
        cerrar();
        pintarVista();
    }

    campo.addEventListener('input', function () { sugerir(); pintarVista(); });
    campo.addEventListener('click', sugerir);
    campo.addEventListener('blur', function () { setTimeout(cerrar, 100); });
    campo.addEventListener('keydown', function (e) {
        if (opciones.length === 0) { return; }
        if (e.key === 'ArrowDown') {
            e.preventDefault();
            resaltar((activa + 1) % opciones.length);
        } else if (e.key === 'ArrowUp') {
            e.preventDefault();
            resaltar((activa - 1 + opciones.length) % opciones.length);
        } else if ((e.key === 'Enter' || e.key === 'Tab') && activa >= 0) {
            e.preventDefault();   // Enter no envía el formulario mientras hay sugerencias
            elegir(activa);
        } else if (e.key === 'Escape') {
            e.preventDefault();
            e.stopPropagation();  // cierra las sugerencias, no el detalle
            cerrar();
        }
    });

    pintarVista();
}());
