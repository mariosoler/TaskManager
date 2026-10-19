/*
 * Subtareas del detalle de tarjeta: editar el texto y reordenarlas.
 * - Cada subtarea recibe un asa (⠿) a la izquierda: se arrastra para cambiarla de sitio; con el foco en el asa,
 *   ↑/↓ la mueven con el teclado.
 * - Pulsar el texto (o Enter con el foco en él) lo cambia por un cuadro: Enter o salir del cuadro guarda, Esc cancela.
 *   La casilla sigue marcando la subtarea. Las enlazadas (data-enlazada="1") muestran el título de su tarjeta y
 *   no se editan: su texto sigue siendo el enlace a esa tarjeta.
 * - Al editar una tarjeta existente se guarda sin recargar (Tarjeta.ashx: accion=subtarea-texto / subtareas-orden).
 *   Al crearla, las subtareas solo existen en el servidor (ViewState): se envía un postback con la acción en un
 *   campo oculto ("texto|índice|nuevo texto" u "orden|2,0,1").
 * Sin JavaScript no hay asa ni edición (pulsar el texto marca la casilla): se pueden añadir, marcar y eliminar.
 */
(function () {
    'use strict';

    var cfg = window.TaskManagerSubtareas;
    if (!cfg) { return; }
    var lista = document.querySelector('.modal ul.subtareas');
    if (!lista) { return; }

    var editando = cfg.tarjeta > 0;
    var estado = document.getElementById(cfg.estado);
    var temporizadorOrden = null;

    function mostrar(texto, clase) {
        if (!estado) { return; }
        estado.textContent = texto || '';
        estado.className = 'guardado-estado' + (clase ? ' ' + clase : '');
    }

    function filas() {
        return Array.prototype.slice.call(lista.querySelectorAll('li.subtarea[data-subtarea-id]'));
    }

    function ordenActual() {
        return filas().map(function (f) { return f.getAttribute('data-subtarea-id'); }).join(',');
    }

    /* POST a Tarjeta.ashx; se resuelve con el JSON o se rechaza con un Error con el mensaje. */
    function enviar(datos) {
        var p = new URLSearchParams();
        p.set('tablero', String(cfg.tablero));
        p.set('tarjeta', String(cfg.tarjeta));
        Object.keys(datos).forEach(function (k) { p.set(k, datos[k]); });
        return fetch(cfg.url, {
            method: 'POST',
            credentials: 'same-origin',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8', 'X-Chat-Token': cfg.token },
            body: p.toString()
        }).then(function (r) {
            return r.json().catch(function () { return {}; }).then(function (j) {
                if (!r.ok || !j.ok) { throw new Error(j.error || 'No se ha podido guardar la subtarea.'); }
                return j;
            });
        });
    }

    function guardado(datos) {
        mostrar('✓ Guardado' + (datos.modificacion ? ' · ' + datos.modificacion : ''), 'ok');
    }

    /* Tarjeta que se está creando: el servidor aplica la acción en un postback. */
    function postback(valor) {
        document.getElementById(cfg.accion).value = valor;
        window.__doPostBack(cfg.enviarAccion, '');
    }

    // ----- Editar el texto -----

    function editar(fila) {
        var etiqueta = fila.querySelector('label');
        if (!etiqueta || fila.querySelector('.subtarea-edicion')) { return; }
        var original = etiqueta.textContent.trim();
        var cuadro = document.createElement('input');
        cuadro.type = 'text';
        cuadro.className = 'subtarea-edicion';
        cuadro.maxLength = 300;
        cuadro.value = original;
        cuadro.setAttribute('aria-label', 'Texto de la subtarea');
        etiqueta.hidden = true;
        etiqueta.parentNode.insertBefore(cuadro, etiqueta.nextSibling);
        fila.classList.add('editando');
        cuadro.focus();
        cuadro.select();

        var terminado = false;
        // devolverFoco: al acabar con Enter o Esc el foco vuelve al texto (al salir del cuadro, no).
        function terminar(guardar, devolverFoco) {
            if (terminado) { return; }
            terminado = true;
            var nuevo = cuadro.value.trim();
            cuadro.remove();
            etiqueta.hidden = false;
            fila.classList.remove('editando');
            if (devolverFoco) { etiqueta.focus(); }
            if (!guardar || nuevo === '' || nuevo === original) { return; }
            if (!editando) {
                postback('texto|' + fila.getAttribute('data-subtarea-id') + '|' + nuevo);
                return;
            }
            etiqueta.textContent = nuevo;
            mostrar('Guardando…', 'guardando');
            enviar({ accion: 'subtarea-texto', subtarea: fila.getAttribute('data-subtarea-id'), texto: nuevo })
                .then(function (datos) {
                    if (datos.texto) { etiqueta.textContent = datos.texto; }
                    guardado(datos);
                })
                .catch(function (e) {
                    etiqueta.textContent = original;
                    mostrar((e && e.message) || 'No se ha podido guardar la subtarea.', 'error');
                });
        }

        cuadro.addEventListener('keydown', function (e) {
            if (e.key === 'Enter') {
                // Sin esto, Enter enviaría el formulario con el botón predeterminado del detalle.
                e.preventDefault();
                e.stopPropagation();
                terminar(true, true);
            } else if (e.key === 'Escape') {
                e.preventDefault();
                e.stopPropagation();
                terminar(false, true);
            }
        });
        // Salir del cuadro guarda.
        cuadro.addEventListener('blur', function () { terminar(true, false); });
    }

    // ----- Reordenar -----

    var ordenInicial = ordenActual();

    function guardarOrden() {
        clearTimeout(temporizadorOrden);
        var orden = ordenActual();
        if (orden === ordenInicial) { return; }
        if (!editando) {
            postback('orden|' + orden);
            return;
        }
        var anterior = ordenInicial;
        ordenInicial = orden;
        mostrar('Guardando…', 'guardando');
        enviar({ accion: 'subtareas-orden', orden: orden }).then(guardado).catch(function (e) {
            ordenInicial = anterior;
            mostrar((e && e.message) || 'No se ha podido guardar el orden de las subtareas.', 'error');
        });
    }

    // Con el teclado se espera un poco por si se mueve varias posiciones seguidas (al crear, cada envío recarga).
    function programarOrden() {
        clearTimeout(temporizadorOrden);
        temporizadorOrden = setTimeout(guardarOrden, editando ? 500 : 900);
    }

    function anunciarPosicion(fila) {
        var todas = filas();
        mostrar('Subtarea en la posición ' + (todas.indexOf(fila) + 1) + ' de ' + todas.length + '.', '');
    }

    var arrastrada = null;

    function siguienteBajo(y) {
        var candidatas = filas();
        for (var i = 0; i < candidatas.length; i++) {
            if (candidatas[i] === arrastrada) { continue; }
            var r = candidatas[i].getBoundingClientRect();
            if (y < r.top + r.height / 2) { return candidatas[i]; }
        }
        return null;
    }

    lista.addEventListener('dragover', function (e) {
        if (!arrastrada) { return; }
        e.preventDefault();
        e.dataTransfer.dropEffect = 'move';
        var siguiente = siguienteBajo(e.clientY);
        if (siguiente) {
            if (arrastrada.nextElementSibling !== siguiente) { lista.insertBefore(arrastrada, siguiente); }
        } else if (lista.lastElementChild !== arrastrada) {
            lista.appendChild(arrastrada);
        }
    });
    lista.addEventListener('drop', function (e) {
        if (arrastrada) { e.preventDefault(); }
    });

    // ----- Asa y ✎ en cada subtarea -----

    filas().forEach(function (fila) {
        // span y no button: en algunos navegadores arrastrar desde un botón no inicia el arrastre.
        var asa = document.createElement('span');
        asa.tabIndex = 0;
        asa.setAttribute('role', 'button');
        asa.className = 'subtarea-asa';
        asa.textContent = '⠿';
        asa.title = 'Arrastra para cambiar el orden (o ↑/↓)';
        asa.setAttribute('aria-label', 'Mover subtarea: flechas arriba y abajo');
        fila.insertBefore(asa, fila.firstChild);

        // Solo se arrastra desde el asa (pulsar el texto sigue marcando la casilla).
        asa.addEventListener('mousedown', function () { fila.draggable = true; });
        asa.addEventListener('mouseup', function () { if (!arrastrada) { fila.draggable = false; } });
        fila.addEventListener('dragstart', function (e) {
            if (!fila.draggable) { return; }
            arrastrada = fila;
            e.dataTransfer.effectAllowed = 'move';
            e.dataTransfer.setData('text/plain', fila.getAttribute('data-subtarea-id'));
            setTimeout(function () { fila.classList.add('arrastrando'); }, 0);
        });
        fila.addEventListener('dragend', function () {
            fila.classList.remove('arrastrando');
            fila.draggable = false;
            arrastrada = null;
            guardarOrden();
        });

        asa.addEventListener('keydown', function (e) {
            var destino = null;
            if (e.key === 'ArrowUp' && fila.previousElementSibling) {
                destino = fila.previousElementSibling;
            } else if (e.key === 'ArrowDown' && fila.nextElementSibling) {
                destino = fila.nextElementSibling.nextElementSibling;
                if (!destino) { lista.appendChild(fila); }
            } else {
                return;
            }
            e.preventDefault();
            if (destino) { lista.insertBefore(fila, destino); }
            asa.focus();
            anunciarPosicion(fila);
            programarOrden();
        });

        if (fila.getAttribute('data-enlazada') === '1') { return; }
        // Pulsar el texto lo edita (sin esto, la etiqueta marcaría la casilla).
        var texto = fila.querySelector('label');
        if (!texto) { return; }
        texto.classList.add('subtarea-texto-editable');
        texto.tabIndex = 0;
        texto.title = 'Pulsa para editar el texto';
        texto.addEventListener('click', function (e) {
            e.preventDefault();
            editar(fila);
        });
        texto.addEventListener('keydown', function (e) {
            if (e.key === 'Enter' || e.key === 'F2') {
                e.preventDefault();
                e.stopPropagation();
                editar(fila);
            }
        });
    });
}());
