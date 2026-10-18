/*
 * Cuadro de etiquetas (#PLANIFICADOR_CABINAS #urgente) del detalle de tarjeta y del buscador:
 * - las etiquetas se pintan con su color dentro del cuadro, cada una con ✕ para quitarla (Retroceso con el
 *   cuadro vacío quita la última). Espacio, coma, Enter, Tab o salir del cuadro añaden lo escrito;
 * - mientras se escribe sugiere las etiquetas que ya existen (las más usadas primero; en el tablero, solo las
 *   suyas: no se comparten entre tableros); flechas para elegir, Enter/Tab para añadir, Esc para cerrar;
 * - en el detalle de tarjeta, una etiqueta que no existe abre un cuadro para confirmar que se crea y elegir su
 *   color: se crea en el tablero con Tarjeta.ashx (accion=etiqueta) y se asigna a la tarjeta al guardarla;
 * - con cfg.modo = 'filtro' (buscador) no se crean etiquetas: las que no existen se marcan «no existe».
 * El TextBox original queda oculto con "#A #B " (lo que leen el servidor y el guardado automático). Sin
 * JavaScript se escribe en él tal cual, y las etiquetas nuevas toman el siguiente color de la paleta al guardar.
 * Datos en <script type="application/json"> (TableroPagina.EtiquetasJson y PaletaJson; BuscarPagina.EtiquetasJson).
 * Los textos se insertan con textContent y los colores se validan antes de usarlos.
 */
(function () {
    'use strict';

    var cfg = window.TaskManagerEtiquetas;
    if (!cfg) { return; }
    var campo = document.getElementById(cfg.campo);
    var nodoDatos = document.getElementById(cfg.datos);
    if (!campo || !nodoDatos) { return; }   // el detalle de tarjeta está cerrado

    var MAXIMO = 10;
    var MAXIMO_SUGERENCIAS = 8;
    var NOMBRE = /^[\p{L}\p{N}_-]{1,50}$/u;
    var COLOR = /^#[0-9a-f]{6}$/i;
    var SEPARADOR = /[\s,;]+/;

    function leerJson(nodo) {
        try { return (nodo && JSON.parse(nodo.textContent)) || []; } catch (e) { return []; }
    }
    function clave(nombre) { return nombre.toLocaleLowerCase(); }
    function sinAlmohadilla(texto) { return texto.replace(/^#+/, ''); }

    var esFiltro = cfg.modo === 'filtro';
    var datos = leerJson(nodoDatos);
    var paleta = esFiltro ? [] : leerJson(document.getElementById(cfg.paleta)).filter(function (p) {
        return COLOR.test(p.c) && COLOR.test(p.t);
    });
    // Sin paleta o sin dirección no se puede preguntar: las nuevas se crean al guardar, como sin JavaScript.
    var puedeCrear = !esFiltro && paleta.length > 0 && !!cfg.url;

    var porNombre = {};
    datos.forEach(function (d) { porNombre[clave(d.n)] = d; });

    // Etiquetas del cuadro, en el orden escrito.
    var nombres = [];
    campo.value.split(SEPARADOR).map(sinAlmohadilla).forEach(function (n) {
        if (n && !tiene(n)) { nombres.push(n); }
    });

    function tiene(nombre) {
        var c = clave(nombre);
        return nombres.some(function (n) { return clave(n) === c; });
    }

    // ----- Elementos -----
    var caja = document.createElement('div');
    caja.className = 'etiquetas-caja';

    var entrada = document.createElement('input');
    entrada.type = 'text';
    entrada.id = campo.id + '_entrada';
    entrada.className = 'etiquetas-entrada';
    entrada.autocomplete = 'off';
    entrada.spellcheck = false;
    entrada.maxLength = 600;
    if (campo.getAttribute('aria-describedby')) {
        entrada.setAttribute('aria-describedby', campo.getAttribute('aria-describedby'));
    }
    caja.appendChild(entrada);

    var lista = document.createElement('ul');
    lista.id = campo.id + '_sugerencias';
    lista.className = 'etiquetas-sugerencias oculto';
    lista.setAttribute('role', 'listbox');
    lista.setAttribute('aria-label', 'Etiquetas sugeridas');

    var aviso = document.createElement('small');
    aviso.className = 'etiquetas-aviso';
    aviso.setAttribute('role', 'status');

    entrada.setAttribute('role', 'combobox');
    entrada.setAttribute('aria-autocomplete', 'list');
    entrada.setAttribute('aria-expanded', 'false');
    entrada.setAttribute('aria-controls', lista.id);

    // El TextBox queda oculto (se sigue enviando con el formulario); la etiqueta del campo apunta al nuevo cuadro.
    var rotulo = document.querySelector('label[for="' + campo.id + '"]');
    if (rotulo) { rotulo.htmlFor = entrada.id; }
    campo.classList.add('oculto');
    campo.setAttribute('aria-hidden', 'true');
    campo.tabIndex = -1;
    campo.parentNode.insertBefore(caja, campo.nextSibling);
    caja.parentNode.insertBefore(lista, caja.nextSibling);
    lista.parentNode.insertBefore(aviso, lista.nextSibling);

    // Pulsar en el hueco del cuadro lleva al campo de escritura.
    caja.addEventListener('mousedown', function (e) {
        if (e.target === caja) {
            e.preventDefault();
            entrada.focus();
        }
    });

    function avisar(texto) { aviso.textContent = texto || ''; }

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

    // ----- Etiquetas del cuadro -----
    function pintar() {
        Array.prototype.slice.call(caja.querySelectorAll('.etiqueta-tag')).forEach(function (n) { caja.removeChild(n); });
        nombres.forEach(function (nombre, i) {
            var dato = porNombre[clave(nombre)];
            var span;
            if (!NOMBRE.test(nombre)) {
                span = chip('', null, 'invalida');
                span.title = 'No válida: solo letras, números, _ y -';
            } else if (dato) {
                span = chip('', dato);
            } else if (esFiltro) {
                span = chip('', null, 'desconocida');
                span.title = 'No hay ninguna etiqueta con ese nombre';
            } else {
                span = chip('', null, 'nueva');
                span.title = 'Nueva en este tablero: el color se asigna al guardar';
            }
            var texto = document.createElement('span');
            texto.className = 'etiqueta-nombre';
            texto.textContent = dato ? dato.n : nombre;
            span.appendChild(texto);

            var quitar = document.createElement('button');
            quitar.type = 'button';
            quitar.className = 'etiqueta-quitar';
            quitar.tabIndex = -1;          // con teclado: Retroceso en el cuadro vacío
            quitar.textContent = '✕';
            quitar.setAttribute('aria-label', 'Quitar la etiqueta ' + (dato ? dato.n : nombre));
            // mousedown sin foco: si no, el cuadro pierde el foco, se vuelve a pintar y el clic no llega.
            quitar.addEventListener('mousedown', function (e) { e.preventDefault(); });
            quitar.addEventListener('click', function () {
                nombres.splice(i, 1);
                cambiado();
                avisar('');
                entrada.focus();
            });
            span.appendChild(quitar);
            caja.insertBefore(span, entrada);
        });
        entrada.placeholder = nombres.length ? '' : campo.placeholder;
    }

    // Copia las etiquetas al TextBox oculto y avisa (el guardado automático escucha input/change).
    function cambiado() {
        pintar();
        var texto = nombres.map(function (n) { return '#' + n; }).join(' ');
        campo.value = texto ? texto + ' ' : '';
        campo.dispatchEvent(new Event('input', { bubbles: true }));
        campo.dispatchEvent(new Event('change', { bubbles: true }));
    }

    function hayHueco() {
        if (esFiltro || nombres.length + cola.length < MAXIMO) { return true; }
        avisar('Como mucho ' + MAXIMO + ' etiquetas por tarjeta.');
        return false;
    }

    /* Añade una etiqueta escrita. Devuelve false si no se ha podido (se deja en el cuadro para corregirla). */
    function anadir(texto) {
        var nombre = sinAlmohadilla(texto);
        if (!nombre) { return true; }
        if (!NOMBRE.test(nombre)) {
            avisar('«' + nombre + '» no es válida: solo letras, números, _ y - (hasta 50 caracteres).');
            return false;
        }
        if (tiene(nombre) || enCola(nombre)) { return true; }
        if (!hayHueco()) { return false; }
        avisar('');
        var dato = porNombre[clave(nombre)];
        if (dato || !puedeCrear) {
            nombres.push(dato ? dato.n : nombre);
            cambiado();
        } else {
            pedirCreacion(nombre);
        }
        return true;
    }

    /* Añade lo escrito en el cuadro: todo (todo = true) o solo lo que ya va seguido de un separador. */
    function procesarEntrada(todo) {
        var partes = entrada.value.split(SEPARADOR);
        var resto = todo ? '' : partes.pop();   // lo que se sigue escribiendo
        for (var i = 0; i < partes.length; i++) {
            if (!anadir(partes[i])) {
                resto = partes.slice(i).filter(Boolean).join(' ') + (resto ? ' ' + resto : '');
                break;
            }
        }
        if (entrada.value !== resto) { entrada.value = resto; }
    }

    // ----- Sugerencias -----
    var opciones = [];     // sugerencias visibles
    var activa = -1;       // índice de la opción resaltada

    function cerrar() {
        lista.classList.add('oculto');
        lista.textContent = '';
        opciones = [];
        activa = -1;
        entrada.setAttribute('aria-expanded', 'false');
        entrada.removeAttribute('aria-activedescendant');
    }

    function resaltar(indice) {
        activa = indice;
        Array.prototype.forEach.call(lista.children, function (li, i) {
            li.setAttribute('aria-selected', i === indice ? 'true' : 'false');
            li.classList.toggle('activa', i === indice);
        });
        if (indice >= 0) {
            entrada.setAttribute('aria-activedescendant', lista.children[indice].id);
            lista.children[indice].scrollIntoView({ block: 'nearest' });
        } else {
            entrada.removeAttribute('aria-activedescendant');
        }
    }

    function sugerir() {
        if (!entrada.value || SEPARADOR.test(entrada.value)) { cerrar(); return; }
        var buscado = clave(sinAlmohadilla(entrada.value));

        var empiezan = [], contienen = [];
        datos.forEach(function (d) {
            var n = clave(d.n);
            if (tiene(d.n)) { return; }
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
        entrada.setAttribute('aria-expanded', 'true');
        resaltar(0);
    }

    function elegir(indice) {
        var d = opciones[indice];
        cerrar();
        if (!d) { return; }
        entrada.value = '';
        if (!tiene(d.n) && hayHueco()) {
            avisar('');
            nombres.push(d.n);
            cambiado();
        }
    }

    // ----- Etiqueta nueva: confirmar y elegir color (solo en el detalle de tarjeta) -----
    var cola = [];         // etiquetas nuevas pendientes de confirmar (se preguntan de una en una)
    var elegido = 0;       // índice del color elegido en la paleta
    var enviando = false;
    var dialogo, vistaNueva, colores, errorNueva, botonCrear, botonCancelar;

    function enCola(nombre) {
        var c = clave(nombre);
        return cola.some(function (n) { return clave(n) === c; });
    }

    function crearDialogo() {
        dialogo = document.createElement('div');
        dialogo.className = 'etiqueta-nueva oculto';
        dialogo.setAttribute('role', 'dialog');
        dialogo.setAttribute('aria-labelledby', campo.id + '_nueva_titulo');

        var titulo = document.createElement('p');
        titulo.id = campo.id + '_nueva_titulo';
        titulo.className = 'etiqueta-nueva-titulo';
        titulo.textContent = 'Crear una etiqueta nueva en el tablero';
        dialogo.appendChild(titulo);

        vistaNueva = document.createElement('div');
        vistaNueva.className = 'etiqueta-nueva-vista';
        dialogo.appendChild(vistaNueva);

        colores = document.createElement('div');
        colores.className = 'etiqueta-nueva-colores';
        colores.setAttribute('role', 'radiogroup');
        colores.setAttribute('aria-label', 'Color de la etiqueta');
        paleta.forEach(function (p, i) {
            var b = document.createElement('button');
            b.type = 'button';
            b.className = 'etiqueta-color';
            b.setAttribute('role', 'radio');
            b.setAttribute('aria-label', p.n);
            b.title = p.n;
            b.style.backgroundColor = p.c;
            b.style.color = p.t;
            b.addEventListener('click', function () { marcarColor(i, true); });
            colores.appendChild(b);
        });
        dialogo.appendChild(colores);

        errorNueva = document.createElement('p');
        errorNueva.className = 'etiqueta-nueva-error';
        errorNueva.setAttribute('role', 'alert');
        dialogo.appendChild(errorNueva);

        var acciones = document.createElement('div');
        acciones.className = 'etiqueta-nueva-acciones';
        botonCancelar = document.createElement('button');
        botonCancelar.type = 'button';
        botonCancelar.className = 'boton boton-claro';
        botonCancelar.textContent = 'Cancelar';
        botonCancelar.addEventListener('click', function () { siguienteDeCola(); });
        botonCrear = document.createElement('button');
        botonCrear.type = 'button';
        botonCrear.className = 'boton';
        botonCrear.textContent = 'Crear etiqueta';
        botonCrear.addEventListener('click', confirmarCreacion);
        acciones.appendChild(botonCancelar);
        acciones.appendChild(botonCrear);
        dialogo.appendChild(acciones);

        dialogo.addEventListener('keydown', function (e) {
            if (e.key === 'Escape') {
                e.preventDefault();
                e.stopPropagation();   // cierra este cuadro, no el detalle
                siguienteDeCola();
            } else if (e.key === 'Enter') {
                if (e.target === botonCancelar) { return; }
                e.preventDefault();    // Enter crea la etiqueta (no envía el formulario)
                confirmarCreacion();
            } else if (/^Arrow(Left|Right|Up|Down)$/.test(e.key) && colores.contains(e.target)) {
                e.preventDefault();
                var paso = (e.key === 'ArrowLeft' || e.key === 'ArrowUp') ? -1 : 1;
                marcarColor((elegido + paso + paleta.length) % paleta.length, true);
            }
        });

        caja.parentNode.insertBefore(dialogo, caja.nextSibling);
    }

    function marcarColor(indice, enfocar) {
        elegido = indice;
        Array.prototype.forEach.call(colores.children, function (b, i) {
            var marcado = i === indice;
            b.setAttribute('aria-checked', marcado ? 'true' : 'false');
            b.tabIndex = marcado ? 0 : -1;
            b.textContent = marcado ? '✓' : '';
        });
        vistaNueva.textContent = '';
        vistaNueva.appendChild(chip(cola[0] || '', paleta[indice]));
        if (enfocar) { colores.children[indice].focus(); }
    }

    function pedirCreacion(nombre) {
        cola.push(nombre);
        if (cola.length === 1) { abrirDialogo(); }
    }

    function abrirDialogo() {
        if (!dialogo) { crearDialogo(); }
        cerrar();
        errorNueva.textContent = '';
        // Propone el mismo color que asignaría el servidor (el siguiente de la paleta en este tablero).
        marcarColor(datos.length % paleta.length, false);
        dialogo.classList.remove('oculto');
        colores.children[elegido].focus();
    }

    /* Quita la primera de la cola (creada o cancelada) y pregunta por la siguiente, si hay. */
    function siguienteDeCola() {
        cola.shift();
        if (cola.length > 0) {
            abrirDialogo();
        } else {
            dialogo.classList.add('oculto');
            entrada.focus();
        }
    }

    function confirmarCreacion() {
        if (enviando || cola.length === 0) { return; }
        enviando = true;
        botonCrear.disabled = true;
        errorNueva.textContent = '';

        var p = new URLSearchParams();
        p.set('accion', 'etiqueta');
        p.set('tablero', String(cfg.tablero));
        p.set('tarjeta', String(cfg.tarjeta || 0));
        p.set('nombre', cola[0]);
        p.set('color', paleta[elegido].c);
        fetch(cfg.url, {
            method: 'POST',
            credentials: 'same-origin',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8', 'X-Chat-Token': cfg.token },
            body: p.toString()
        }).then(function (r) {
            return r.json().catch(function () { return {}; }).then(function (d) {
                if (!r.ok || !d.ok || !d.etiqueta) { throw new Error(d.error || 'No se ha podido crear la etiqueta.'); }
                return d.etiqueta;
            });
        }).then(function (e) {
            // Si ya existía (creada por otra persona entretanto) se usa esa, con su color.
            if (!porNombre[clave(e.n)]) { datos.push(e); }
            porNombre[clave(e.n)] = e;
            if (!tiene(e.n)) {
                nombres.push(e.n);
                cambiado();
            }
            siguienteDeCola();
        }).catch(function (error) {
            errorNueva.textContent = (error && error.message) || 'No se ha podido crear la etiqueta.';
        }).then(function () {
            enviando = false;
            botonCrear.disabled = false;
        });
    }

    // ----- Eventos del cuadro -----
    entrada.addEventListener('input', function () {
        if (SEPARADOR.test(entrada.value)) { procesarEntrada(false); }
        sugerir();
    });
    entrada.addEventListener('click', sugerir);
    entrada.addEventListener('blur', function () {
        cerrar();
        procesarEntrada(true);
    });
    entrada.addEventListener('keydown', function (e) {
        if (opciones.length > 0) {
            if (e.key === 'ArrowDown') {
                e.preventDefault();
                resaltar((activa + 1) % opciones.length);
                return;
            } else if (e.key === 'ArrowUp') {
                e.preventDefault();
                resaltar((activa - 1 + opciones.length) % opciones.length);
                return;
            } else if ((e.key === 'Enter' || e.key === 'Tab') && activa >= 0) {
                e.preventDefault();   // Enter no envía el formulario mientras hay sugerencias
                elegir(activa);
                return;
            } else if (e.key === 'Escape') {
                e.preventDefault();
                e.stopPropagation();  // cierra las sugerencias, no el detalle
                cerrar();
                return;
            }
        }
        if (e.key === 'Enter' && entrada.value.trim() !== '') {
            e.preventDefault();       // añade lo escrito (sin enviar el formulario)
            procesarEntrada(true);
        } else if (e.key === 'Backspace' && entrada.value === '' && nombres.length > 0) {
            e.preventDefault();
            nombres.pop();
            cambiado();
            avisar('');
        }
    });

    // Al enviar el formulario (Crear tarjeta, Buscar...) va también lo que quede escrito o pendiente de color:
    // el servidor crea esas etiquetas con el siguiente color de la paleta.
    if (campo.form) {
        campo.form.addEventListener('submit', function () {
            var extra = cola.concat(entrada.value.split(SEPARADOR).map(sinAlmohadilla)).filter(function (n) {
                return n && !tiene(n);
            });
            if (extra.length > 0) {
                campo.value = nombres.concat(extra).map(function (n) { return '#' + n; }).join(' ') + ' ';
            }
        });
    }

    pintar();
}());
