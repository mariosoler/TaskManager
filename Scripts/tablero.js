/*
 * Tablero: arrastrar y soltar tarjetas entre listas y abrir su detalle.
 * Las acciones se envían al servidor con __doPostBack sobre LinkButtons ocultos
 * (ver window.TaskManagerTablero en Tablero.aspx).
 */
(function () {
    'use strict';

    var cfg = window.TaskManagerTablero;
    if (!cfg) { return; }

    var arrastrada = null;
    var origen = null;      // { padre, siguiente, indice } para restaurar si no se suelta en una lista
    var soltada = false;

    function elemento(nodo) {
        return nodo && nodo.nodeType === 3 ? nodo.parentElement : nodo;
    }

    function cercano(nodo, selector) {
        var el = elemento(nodo);
        return el && el.closest ? el.closest(selector) : null;
    }

    function indiceEn(contenedor, tarjeta) {
        return Array.prototype.indexOf.call(contenedor.querySelectorAll('.tarjeta'), tarjeta);
    }

    function abrir(tarjeta) {
        document.getElementById(cfg.tarjeta).value = tarjeta.getAttribute('data-tarjeta-id');
        window.__doPostBack(cfg.abrir, '');
    }

    // Tarjeta delante de la cual insertar según la posición vertical del cursor.
    function tarjetaSiguiente(contenedor, y) {
        var tarjetas = contenedor.querySelectorAll('.tarjeta');
        for (var i = 0; i < tarjetas.length; i++) {
            if (tarjetas[i] === arrastrada) { continue; }
            var r = tarjetas[i].getBoundingClientRect();
            if (y < r.top + r.height / 2) { return tarjetas[i]; }
        }
        return null;
    }

    document.addEventListener('dragstart', function (e) {
        var tarjeta = cercano(e.target, '.tarjeta');
        if (!tarjeta) { return; }
        arrastrada = tarjeta;
        soltada = false;
        origen = { padre: tarjeta.parentNode, siguiente: tarjeta.nextSibling, indice: indiceEn(tarjeta.parentNode, tarjeta) };
        e.dataTransfer.effectAllowed = 'move';
        e.dataTransfer.setData('text/plain', tarjeta.getAttribute('data-tarjeta-id'));
        setTimeout(function () { tarjeta.classList.add('arrastrando'); }, 0);
    });

    document.addEventListener('dragover', function (e) {
        if (!arrastrada) { return; }
        var contenedor = cercano(e.target, '.tarjetas');
        if (!contenedor) { return; }
        e.preventDefault();
        e.dataTransfer.dropEffect = 'move';

        var siguiente = tarjetaSiguiente(contenedor, e.clientY);
        if (siguiente) {
            if (arrastrada.nextElementSibling !== siguiente) { contenedor.insertBefore(arrastrada, siguiente); }
        } else if (contenedor.lastElementChild !== arrastrada) {
            contenedor.appendChild(arrastrada);
        }
    });

    document.addEventListener('drop', function (e) {
        if (!arrastrada) { return; }
        var contenedor = cercano(e.target, '.tarjetas');
        if (!contenedor) { return; }
        e.preventDefault();
        soltada = true;

        var indice = indiceEn(contenedor, arrastrada);
        if (contenedor === origen.padre && indice === origen.indice) { return; }   // no ha cambiado

        document.getElementById(cfg.tarjeta).value = arrastrada.getAttribute('data-tarjeta-id');
        document.getElementById(cfg.lista).value = contenedor.getAttribute('data-lista-id');
        document.getElementById(cfg.indice).value = String(indice);
        window.__doPostBack(cfg.mover, '');
    });

    document.addEventListener('dragend', function () {
        if (!arrastrada) { return; }
        arrastrada.classList.remove('arrastrando');
        if (!soltada && origen) { origen.padre.insertBefore(arrastrada, origen.siguiente); }
        arrastrada = null;
        origen = null;
    });

    // ----- Desplegables de la cabecera (Fondo, Editar): solo uno abierto a la vez -----
    function desplegablesAbiertos(excepto) {
        return Array.prototype.filter.call(document.querySelectorAll('details.tablero-fondo[open]'),
            function (d) { return d !== excepto; });
    }

    function cerrarDesplegables(excepto) {
        var abiertos = desplegablesAbiertos(excepto);
        abiertos.forEach(function (d) { d.removeAttribute('open'); });
        return abiertos.length > 0;
    }

    // "toggle" no burbujea: se escucha en fase de captura.
    document.addEventListener('toggle', function (e) {
        var d = e.target;
        if (d.matches && d.matches('details.tablero-fondo') && d.open) { cerrarDesplegables(d); }
    }, true);

    document.addEventListener('click', function (e) {
        // Pulsar fuera de un desplegable abierto lo cierra.
        cerrarDesplegables(cercano(e.target, 'details.tablero-fondo'));

        var tarjeta = cercano(e.target, '.tarjeta');
        if (tarjeta) { abrir(tarjeta); }
    });

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') {
            // Esc cierra primero el desplegable abierto y devuelve el foco a su botón.
            var abierto = desplegablesAbiertos(null)[0];
            if (abierto) {
                abierto.removeAttribute('open');
                var resumen = abierto.querySelector('summary');
                if (resumen) { resumen.focus(); }
                return;
            }
            // Dentro de un campo, Esc no cierra el detalle (se perderían los cambios sin guardar).
            if (/^(INPUT|TEXTAREA|SELECT)$/.test(e.target.tagName)) { return; }
            var cerrar = document.getElementById(cfg.cerrar);
            if (cerrar) { cerrar.click(); }
            return;
        }
        if (e.key === 'Enter' && e.target.classList && e.target.classList.contains('tarjeta')) {
            e.preventDefault();
            abrir(e.target);
        }
    });

    // ----- Nombre del tablero editable al pulsar el título (solo si el servidor envía el cuadro de texto) -----
    (function () {
        var titulo = document.getElementById(cfg.titulo);
        var edicion = document.getElementById(cfg.tituloEdicion);
        var texto = document.getElementById(cfg.tituloTexto);
        var guardar = document.getElementById(cfg.tituloGuardar);
        var pulsable = titulo && titulo.querySelector('.titulo-texto.editable');
        if (!pulsable || !edicion || !texto || !guardar) { return; }

        var original = texto.value;
        var enviando = false;

        function editar() {
            texto.value = original;
            titulo.classList.add('oculto');
            edicion.classList.remove('oculto');
            texto.focus();
            texto.select();
        }

        function cancelar() {
            texto.value = original;
            edicion.classList.add('oculto');
            titulo.classList.remove('oculto');
            pulsable.focus();
        }

        function confirmar() {
            if (enviando) { return; }
            var nuevo = texto.value.trim();
            if (nuevo === '' || nuevo === original.trim()) { cancelar(); return; }
            enviando = true;
            guardar.click();
        }

        pulsable.addEventListener('click', editar);
        pulsable.addEventListener('keydown', function (e) {
            if (e.key === 'Enter' || e.key === ' ') {
                e.preventDefault();
                editar();
            }
        });
        texto.addEventListener('keydown', function (e) {
            if (e.key === 'Enter') {
                e.preventDefault();
                confirmar();
            } else if (e.key === 'Escape') {
                e.preventDefault();
                cancelar();
            }
        });
        texto.addEventListener('blur', function () {
            // Esc ya ha cerrado el cuadro: el blur posterior no debe guardar.
            if (!edicion.classList.contains('oculto')) { confirmar(); }
        });
    }());
}());
