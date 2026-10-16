/*
 * Editor markdown sencillo y sin dependencias.
 *
 * - TaskManagerMarkdown.renderizar(texto): convierte markdown a HTML. Primero se escapa TODO
 *   el HTML del usuario y después se aplican las marcas, así que la vista previa no puede
 *   inyectar etiquetas ni scripts. Los enlaces solo admiten http(s) y mailto.
 * - Cualquier elemento [data-editor-md] con un <textarea> y un .editor-md-vista se convierte en
 *   editor con barra de herramientas y pestañas Escribir / Vista previa.
 */
(function () {
    'use strict';

    function escapar(texto) {
        return texto.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
                    .replace(/"/g, '&quot;').replace(/'/g, '&#39;');
    }

    // ----- Renderizado -----

    // Formato dentro de una línea. Recibe texto SIN escapar.
    function enLinea(crudo) {
        var guardados = [];
        function guardar(html) {
            guardados.push(html);
            return '\u0000' + (guardados.length - 1) + '\u0000';
        }

        var texto = escapar(crudo.replace(/\u0000/g, ''));

        // Código en línea y enlaces se protegen para que no les afecten las demás marcas.
        texto = texto.replace(/`([^`]+)`/g, function (_, codigo) {
            return guardar('<code>' + codigo + '</code>');
        });
        texto = texto.replace(/\[([^\]]+)\]\(([^)\s]+)\)/g, function (original, etiqueta, url) {
            if (!/^(https?:\/\/|mailto:)/i.test(url)) { return original; }
            return guardar('<a href="' + url + '" target="_blank" rel="noopener noreferrer">' + etiqueta + '</a>');
        });

        texto = texto
            .replace(/\*\*([^*]+)\*\*/g, '<strong>$1</strong>')
            .replace(/__([^_]+)__/g, '<strong>$1</strong>')
            .replace(/\*([^*\s][^*]*)\*/g, '<em>$1</em>')
            .replace(/(^|[^\w])_([^_\s][^_]*)_(?=[^\w]|$)/g, '$1<em>$2</em>')
            .replace(/~~([^~]+)~~/g, '<del>$1</del>');

        return texto.replace(/\u0000(\d+)\u0000/g, function (_, i) { return guardados[+i]; });
    }

    function renderizar(markdown) {
        var lineas = (markdown || '').replace(/\r\n?/g, '\n').split('\n');
        var html = [];
        var parrafo = [];
        var lista = null;   // 'ul' | 'ol'
        var i = 0;
        var m;

        function cerrarParrafo() {
            if (parrafo.length) {
                html.push('<p>' + parrafo.map(enLinea).join('<br>') + '</p>');
                parrafo = [];
            }
        }
        function cerrarLista() {
            if (lista) {
                html.push('</' + lista + '>');
                lista = null;
            }
        }
        function abrirLista(tipo) {
            if (lista !== tipo) {
                cerrarLista();
                html.push('<' + tipo + '>');
                lista = tipo;
            }
        }

        while (i < lineas.length) {
            var linea = lineas[i];

            // Bloque de código ``` (con lenguaje opcional)
            if ((m = /^\s*```\s*([\w#+.-]*)\s*$/.exec(linea))) {
                cerrarParrafo();
                cerrarLista();
                var codigo = [];
                i++;
                while (i < lineas.length && !/^\s*```\s*$/.test(lineas[i])) {
                    codigo.push(lineas[i]);
                    i++;
                }
                i++;   // cierre ```
                var clase = m[1] ? ' class="lenguaje-' + escapar(m[1]) + '"' : '';
                var etiqueta = m[1] ? '<span class="md-lenguaje">' + escapar(m[1]) + '</span>' : '';
                html.push('<pre>' + etiqueta + '<code' + clase + '>' + escapar(codigo.join('\n')) + '</code></pre>');
                continue;
            }

            if (/^\s*$/.test(linea)) {
                cerrarParrafo();
                cerrarLista();
            } else if ((m = /^(#{1,6})\s+(.*)$/.exec(linea))) {
                cerrarParrafo();
                cerrarLista();
                html.push('<h' + m[1].length + '>' + enLinea(m[2]) + '</h' + m[1].length + '>');
            } else if (/^\s*([-*_])(\s*\1){2,}\s*$/.test(linea)) {
                cerrarParrafo();
                cerrarLista();
                html.push('<hr>');
            } else if (/^\s*>/.test(linea)) {
                cerrarParrafo();
                cerrarLista();
                var cita = [];
                while (i < lineas.length && (m = /^\s*>\s?(.*)$/.exec(lineas[i]))) {
                    cita.push(enLinea(m[1]));
                    i++;
                }
                html.push('<blockquote>' + cita.join('<br>') + '</blockquote>');
                continue;
            } else if ((m = /^\s*[-*+]\s+(?:\[([ xX])\]\s+)?(.*)$/.exec(linea))) {
                cerrarParrafo();
                abrirLista('ul');
                var casilla = m[1] ? '<input type="checkbox" disabled' + (m[1] === ' ' ? '' : ' checked') + '> ' : '';
                html.push('<li>' + casilla + enLinea(m[2]) + '</li>');
            } else if ((m = /^\s*\d+[.)]\s+(.*)$/.exec(linea))) {
                cerrarParrafo();
                abrirLista('ol');
                html.push('<li>' + enLinea(m[1]) + '</li>');
            } else {
                cerrarLista();
                parrafo.push(linea);
            }
            i++;
        }
        cerrarParrafo();
        cerrarLista();
        return html.join('\n');
    }

    // ----- Edición -----

    // Rodea la selección con "antes"/"despues" (o inserta un texto de ejemplo si no hay selección).
    function envolver(area, antes, despues, ejemplo) {
        var ini = area.selectionStart, fin = area.selectionEnd;
        var seleccion = area.value.substring(ini, fin) || ejemplo;
        area.setRangeText(antes + seleccion + despues, ini, fin, 'end');
        area.setSelectionRange(ini + antes.length, ini + antes.length + seleccion.length);
    }

    // Añade un prefijo a cada línea seleccionada (listas, citas, títulos).
    function prefijar(area, prefijo) {
        var valor = area.value;
        var ini = valor.lastIndexOf('\n', area.selectionStart - 1) + 1;
        var fin = area.selectionEnd;
        var bloque = valor.substring(ini, fin);
        var n = 0;
        var nuevo = bloque.split('\n').map(function (l) {
            n++;
            return (typeof prefijo === 'function' ? prefijo(n) : prefijo) + l;
        }).join('\n');
        area.setRangeText(nuevo, ini, fin, 'select');
    }

    var acciones = {
        titulo: function (a) { prefijar(a, '## '); },
        negrita: function (a) { envolver(a, '**', '**', 'texto'); },
        cursiva: function (a) { envolver(a, '*', '*', 'texto'); },
        tachado: function (a) { envolver(a, '~~', '~~', 'texto'); },
        codigo: function (a) { envolver(a, '`', '`', 'codigo'); },
        bloque: function (a) {
            var antes = (a.selectionStart > 0 && a.value[a.selectionStart - 1] !== '\n') ? '\n' : '';
            envolver(a, antes + '```\n', '\n```\n', 'codigo');
        },
        lista: function (a) { prefijar(a, '- '); },
        numerada: function (a) { prefijar(a, function (n) { return n + '. '; }); },
        cita: function (a) { prefijar(a, '> '); },
        enlace: function (a) {
            var ini = a.selectionStart;
            var etiqueta = a.value.substring(ini, a.selectionEnd) || 'texto';
            a.setRangeText('[' + etiqueta + '](https://)', ini, a.selectionEnd, 'end');
            a.setSelectionRange(ini + etiqueta.length + 3, ini + etiqueta.length + 11);
        }
    };

    function iniciarEditor(raiz) {
        var area = raiz.querySelector('textarea');
        var vista = raiz.querySelector('.editor-md-vista');
        if (!area || !vista) { return; }

        function modo(nombre, enfocar) {
            var verVista = nombre === 'vista';
            if (verVista) { vista.innerHTML = renderizar(area.value) || '<p class="md-vacio">Nada que mostrar.</p>'; }
            vista.hidden = !verVista;
            area.hidden = verVista;
            raiz.classList.toggle('editor-md-en-vista', verVista);
            Array.prototype.forEach.call(raiz.querySelectorAll('[data-modo]'), function (b) {
                b.classList.toggle('activa', b.getAttribute('data-modo') === nombre);
            });
            if (!verVista && enfocar) { area.focus(); }
        }

        raiz.addEventListener('click', function (e) {
            var boton = e.target.closest('button');
            if (!boton || !raiz.contains(boton)) { return; }
            if (boton.hasAttribute('data-modo')) {
                modo(boton.getAttribute('data-modo'), true);
            } else if (acciones[boton.getAttribute('data-md')]) {
                if (area.hidden) { modo('escribir', true); }
                area.focus();
                acciones[boton.getAttribute('data-md')](area);
            }
        });

        area.addEventListener('keydown', function (e) {
            if (e.key === 'Tab' && !e.shiftKey) {        // sangría con 4 espacios
                e.preventDefault();
                area.setRangeText('    ', area.selectionStart, area.selectionEnd, 'end');
            } else if ((e.ctrlKey || e.metaKey) && (e.key === 'b' || e.key === 'B')) {
                e.preventDefault();
                acciones.negrita(area);
            } else if ((e.ctrlKey || e.metaKey) && (e.key === 'i' || e.key === 'I')) {
                e.preventDefault();
                acciones.cursiva(area);
            }
        });

        // Si ya hay contenido se muestra renderizado; si no, listo para escribir.
        modo(area.value.trim() ? 'vista' : 'escribir');
    }

    window.TaskManagerMarkdown = { renderizar: renderizar };

    Array.prototype.forEach.call(document.querySelectorAll('[data-editor-md]'), iniciarEditor);
}());
