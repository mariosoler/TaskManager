/*
 * Búsqueda rápida del tablero: oculta, mientras se escribe, las tarjetas que no coinciden.
 * - "#texto": busca en el número de caso y en las etiquetas (#1045 → caso 104512; #IT → IT_CASES).
 * - Sin "#": busca en título, descripción, persona asignada, caso y etiquetas.
 * - Varias palabras: tienen que coincidir todas. No distingue mayúsculas ni acentos.
 * Los datos salen de los atributos data-caso, data-etiquetas y data-texto de cada tarjeta.
 * La búsqueda se recuerda durante la sesión (sobrevive a los postbacks del tablero).
 * Atajos: "/" pone el foco en la búsqueda; Esc la borra.
 */
(function () {
    'use strict';

    var cfg = window.TaskManagerBusquedaRapida;
    if (!cfg) { return; }
    var campo = document.getElementById(cfg.campo);
    var resultado = document.getElementById(cfg.resultado);
    if (!campo) { return; }

    var claveSesion = 'busquedaRapida:' + cfg.tablero;

    function normalizar(texto) {
        return (texto || '').normalize('NFD').replace(/[̀-ͯ]/g, '').toLowerCase();
    }

    // Datos de búsqueda de cada tarjeta, preparados una vez (el tablero no cambia sin recargar la página).
    var tarjetas = Array.prototype.map.call(document.querySelectorAll('.tablero .tarjeta'), function (el) {
        return {
            el: el,
            caso: normalizar(el.getAttribute('data-caso')),
            etiquetas: normalizar(el.getAttribute('data-etiquetas')).split(' ').filter(Boolean),
            texto: normalizar(el.getAttribute('data-texto'))
        };
    });

    function coincide(tarjeta, termino) {
        var porCasoOEtiqueta = termino.charAt(0) === '#';
        var buscado = porCasoOEtiqueta ? termino.slice(1) : termino;
        if (!buscado) { return true; }   // un "#" suelto todavía no filtra
        var enCasoOEtiqueta = tarjeta.caso.indexOf(buscado) >= 0 ||
            tarjeta.etiquetas.some(function (e) { return e.indexOf(buscado) >= 0; });
        return porCasoOEtiqueta ? enCasoOEtiqueta : (enCasoOEtiqueta || tarjeta.texto.indexOf(buscado) >= 0);
    }

    function aplicar() {
        var terminos = normalizar(campo.value).split(/\s+/).filter(Boolean);
        var filtrando = terminos.length > 0;
        var visibles = 0;

        tarjetas.forEach(function (t) {
            var ver = terminos.every(function (termino) { return coincide(t, termino); });
            t.el.classList.toggle('filtrada', !ver);
            if (ver) { visibles++; }
        });

        // Contador de cada lista: "visibles/total" mientras se filtra.
        Array.prototype.forEach.call(document.querySelectorAll('.tablero .lista'), function (lista) {
            var contador = lista.querySelector('.lista-contador');
            if (!contador) { return; }
            var total = contador.getAttribute('data-total');
            var enLista = lista.querySelectorAll('.tarjeta:not(.filtrada)').length;
            contador.textContent = filtrando ? enLista + '/' + total : total;
            lista.classList.toggle('sin-coincidencias', filtrando && enLista === 0);
        });

        if (resultado) {
            resultado.textContent = !filtrando ? '' :
                visibles === 0 ? 'Ninguna tarjeta coincide' :
                visibles + ' de ' + tarjetas.length + (tarjetas.length === 1 ? ' tarjeta' : ' tarjetas');
        }
        try { sessionStorage.setItem(claveSesion, campo.value); } catch (e) { /* sin almacenamiento: no se recuerda */ }
    }

    campo.addEventListener('input', aplicar);
    campo.addEventListener('search', aplicar);   // la "x" del campo de búsqueda
    campo.addEventListener('keydown', function (e) {
        if (e.key === 'Enter') {
            e.preventDefault();                  // no enviar el formulario
        } else if (e.key === 'Escape' && campo.value) {
            e.preventDefault();
            campo.value = '';
            aplicar();
        }
    });

    document.addEventListener('keydown', function (e) {
        if (e.key !== '/' || e.ctrlKey || e.metaKey || e.altKey) { return; }
        var destino = e.target;
        if (/^(INPUT|TEXTAREA|SELECT)$/.test(destino.tagName) || destino.isContentEditable) { return; }
        if (document.querySelector('.modal-fondo')) { return; }   // con el detalle de tarjeta abierto, no
        e.preventDefault();
        campo.focus();
        campo.select();
    });

    try { campo.value = sessionStorage.getItem(claveSesion) || ''; } catch (e) { campo.value = ''; }
    aplicar();
}());
