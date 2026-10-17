/*
 * «Añadir miembro» del tablero: sugiere usuarios mientras se escribe (nombre o email, como mucho 4).
 * - Pide las sugerencias a Usuarios.ashx tras una breve pausa (la última respuesta manda: las anteriores se cancelan).
 * - ↑/↓ recorren la lista, Enter o clic añaden al usuario elegido, Esc la cierra.
 * - Elegir una sugerencia pone su email en el cuadro y pulsa «Añadir» (postback normal, que valida todo de nuevo).
 * Los textos se insertan con textContent. Sin JavaScript el cuadro sigue funcionando escribiendo el email.
 */
(function () {
    'use strict';

    var cfg = window.TaskManagerMiembros;
    if (!cfg) { return; }
    var campo = document.getElementById(cfg.campo);
    var boton = document.getElementById(cfg.boton);
    var lista = document.getElementById(cfg.lista);
    if (!campo || !boton || !lista) { return; }

    var PAUSA = 180;
    var MIN = 2;
    var temporizador = null;
    var peticion = null;       // AbortController de la petición en curso
    var sugerencias = [];
    var indice = -1;

    function el(etiqueta, clase, texto) {
        var e = document.createElement(etiqueta);
        if (clase) { e.className = clase; }
        if (texto !== undefined) { e.textContent = texto; }
        return e;
    }

    function cerrar() {
        lista.hidden = true;
        lista.textContent = '';
        sugerencias = [];
        indice = -1;
        campo.setAttribute('aria-expanded', 'false');
        campo.removeAttribute('aria-activedescendant');
    }

    function marcar(i) {
        indice = i;
        Array.prototype.forEach.call(lista.children, function (li, j) {
            var activa = j === i;
            li.classList.toggle('activa', activa);
            li.setAttribute('aria-selected', activa ? 'true' : 'false');
        });
        if (i >= 0) { campo.setAttribute('aria-activedescendant', 'miembroSugerencia' + i); }
        else { campo.removeAttribute('aria-activedescendant'); }
    }

    function elegir(i) {
        var u = sugerencias[i];
        if (!u) { return; }
        campo.value = u.email;
        cerrar();
        boton.click();
    }

    function pintar(usuarios, texto) {
        lista.textContent = '';
        sugerencias = usuarios;
        indice = -1;
        if (!usuarios.length) {
            var vacio = el('li', 'miembro-sugerencias-vacio', 'Ningún usuario coincide con «' + texto + '».');
            vacio.setAttribute('role', 'option');
            vacio.setAttribute('aria-disabled', 'true');
            lista.appendChild(vacio);
        }
        usuarios.forEach(function (u, i) {
            var li = el('li');
            li.id = 'miembroSugerencia' + i;
            li.setAttribute('role', 'option');
            li.setAttribute('aria-selected', 'false');
            li.appendChild(el('span', 'avatar avatar-pequeno', u.iniciales));
            var textos = el('span', 'miembro-sugerencia-textos');
            textos.appendChild(el('span', 'miembro-sugerencia-nombre', u.nombre));
            textos.appendChild(el('span', 'miembro-sugerencia-email', u.email));
            li.appendChild(textos);
            // mousedown (y no click): se elige antes de que el cuadro pierda el foco.
            li.addEventListener('mousedown', function (e) {
                e.preventDefault();
                elegir(i);
            });
            li.addEventListener('mousemove', function () { if (indice !== i) { marcar(i); } });
            lista.appendChild(li);
        });
        lista.hidden = false;
        campo.setAttribute('aria-expanded', 'true');
    }

    function buscar() {
        var texto = campo.value.trim();
        if (peticion) { peticion.abort(); peticion = null; }
        if (texto.length < MIN) {
            cerrar();
            return;
        }
        var control = window.AbortController ? new AbortController() : null;
        peticion = control;
        var url = cfg.url + '?tablero=' + encodeURIComponent(cfg.tablero) + '&q=' + encodeURIComponent(texto);
        fetch(url, { credentials: 'same-origin', signal: control ? control.signal : undefined })
            .then(function (r) { return r.ok ? r.json() : { usuarios: [] }; })
            .then(function (datos) {
                if (peticion !== control) { return; }   // llegó tarde: ya hay otra búsqueda
                peticion = null;
                if (campo.value.trim() !== texto || document.activeElement !== campo) { return; }
                pintar(datos.usuarios || [], texto);
            })
            .catch(function () { /* cancelada o sin conexión: se puede seguir escribiendo el email */ });
    }

    campo.addEventListener('input', function () {
        clearTimeout(temporizador);
        temporizador = setTimeout(buscar, PAUSA);
    });

    campo.addEventListener('keydown', function (e) {
        var abierta = !lista.hidden && sugerencias.length > 0;
        if (e.key === 'ArrowDown' && abierta) {
            e.preventDefault();
            marcar((indice + 1) % sugerencias.length);
        } else if (e.key === 'ArrowUp' && abierta) {
            e.preventDefault();
            marcar(indice <= 0 ? sugerencias.length - 1 : indice - 1);
        } else if (e.key === 'Enter' && abierta && indice >= 0) {
            e.preventDefault();          // sin esto, el DefaultButton enviaría el texto escrito
            e.stopPropagation();
            elegir(indice);
        } else if (e.key === 'Escape' && !lista.hidden) {
            e.preventDefault();
            e.stopPropagation();         // que Esc no cierre también otras cosas de la página
            cerrar();
        }
    });

    campo.addEventListener('blur', function () { setTimeout(cerrar, 0); });
    campo.addEventListener('focus', function () {
        if (campo.value.trim().length >= MIN) { buscar(); }
    });
}());
