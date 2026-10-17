/*
 * Gráficas de Estadisticas.aspx, sin dependencias.
 * Lee el JSON de #datos-graficas y dibuja con HTML/CSS:
 *  - barras horizontales (una serie: color de serie 1; "Sin asignar" en gris de baja énfasis),
 *  - una barra apilada de estados (paleta de estado + icono + etiqueta),
 *  - columnas por día.
 * Cada gráfica lleva tooltip (ratón y teclado) y una vista de tabla ("Ver tabla").
 * Todos los textos procedentes de datos se insertan con textContent / setAttribute.
 */
(function () {
    'use strict';

    var nodo = document.getElementById('datos-graficas');
    if (!nodo) { return; }
    var datos = JSON.parse(nodo.textContent);
    var numero = new Intl.NumberFormat('es-ES');

    var ICONOS = { critico: '⚠', aviso: '◷', bien: '✓', neutro: '–' };

    // ----- Utilidades DOM -----

    function el(etiqueta, clase, texto) {
        var e = document.createElement(etiqueta);
        if (clase) { e.className = clase; }
        if (texto !== undefined) { e.textContent = texto; }
        return e;
    }

    function plural(n, singular, pluralTexto) {
        return numero.format(n) + ' ' + (n === 1 ? singular : pluralTexto);
    }

    function cuerpoDe(id) {
        var figura = document.getElementById(id);
        return figura ? { figura: figura, cuerpo: figura.querySelector('.grafica-cuerpo') } : null;
    }

    function vacio(cuerpo, mensaje) {
        cuerpo.appendChild(el('p', 'grafica-vacia', mensaje || 'Sin datos.'));
    }

    // Vista de tabla (equivalente accesible de cada gráfica).
    function tabla(figura, encabezados, filas) {
        var detalles = el('details', 'grafica-tabla');
        detalles.appendChild(el('summary', null, 'Ver tabla'));
        var t = el('table', 'tabla-datos tabla-compacta');
        var thead = el('thead');
        var tr = el('tr');
        encabezados.forEach(function (h, i) { tr.appendChild(el('th', i > 0 ? 'num' : null, h)); });
        thead.appendChild(tr);
        t.appendChild(thead);
        var tbody = el('tbody');
        filas.forEach(function (fila) {
            var r = el('tr');
            fila.forEach(function (celda, i) { r.appendChild(el('td', i > 0 ? 'num' : null, celda)); });
            tbody.appendChild(r);
        });
        t.appendChild(tbody);
        detalles.appendChild(t);
        figura.appendChild(detalles);
    }

    // ----- Tooltip único: el valor manda, la etiqueta acompaña -----

    var tip = el('div', 'viz-tooltip');
    tip.setAttribute('role', 'tooltip');
    tip.hidden = true;
    document.body.appendChild(tip);
    var activo = null;

    function mostrarTip(marca) {
        if (activo && activo !== marca) { activo.classList.remove('activo'); }
        activo = marca;
        marca.classList.add('activo');
        tip.textContent = '';
        tip.appendChild(el('strong', null, marca.getAttribute('data-tip-valor')));
        tip.appendChild(el('span', null, marca.getAttribute('data-tip-etiqueta')));
        tip.hidden = false;

        var r = marca.getBoundingClientRect();
        var t = tip.getBoundingClientRect();
        var x = Math.min(Math.max(8, r.left + r.width / 2 - t.width / 2), document.documentElement.clientWidth - t.width - 8);
        var y = r.top - t.height - 8;
        if (y < 8) { y = r.bottom + 8; }
        tip.style.left = (x + window.scrollX) + 'px';
        tip.style.top = (y + window.scrollY) + 'px';
    }

    function ocultarTip(marca) {
        marca.classList.remove('activo');
        if (activo === marca) { activo = null; tip.hidden = true; }
    }

    // La zona sensible es toda la fila/franja, no solo los píxeles pintados.
    function conTip(marca, valor, etiqueta) {
        marca.setAttribute('data-tip-valor', valor);
        marca.setAttribute('data-tip-etiqueta', etiqueta);
        marca.setAttribute('aria-label', etiqueta + ': ' + valor);
        marca.tabIndex = 0;
        marca.addEventListener('pointerenter', function () { mostrarTip(marca); });
        marca.addEventListener('pointerleave', function () { ocultarTip(marca); });
        marca.addEventListener('focus', function () { mostrarTip(marca); });
        marca.addEventListener('blur', function () { ocultarTip(marca); });
    }

    // ----- Barras horizontales -----

    function barras(id, filas, unidad, encabezadoTabla, mensajeVacio) {
        var g = cuerpoDe(id);
        if (!g) { return; }
        if (!filas.length || filas.every(function (f) { return f.v === 0; })) {
            vacio(g.cuerpo, mensajeVacio);
            return;
        }
        var maximo = Math.max.apply(null, filas.map(function (f) { return f.v; }));

        filas.forEach(function (f) {
            var fila = el('div', 'graf-fila');
            var etiqueta = el('span', 'graf-etiqueta', f.e);
            etiqueta.title = f.e;
            var zona = el('span', 'graf-zona');
            var pct = maximo > 0 ? f.v / maximo * 100 : 0;
            var barra = el('span', 'graf-barra' + (f.gris ? ' graf-barra-gris' : ''));
            barra.style.width = pct + '%';
            // Valor en la punta de la barra (fuera de ella: nunca se recorta).
            var valor = el('span', 'graf-valor', numero.format(f.v));
            valor.style.left = 'calc(' + pct + '% + 6px)';
            zona.appendChild(barra);
            zona.appendChild(valor);
            fila.appendChild(etiqueta);
            fila.appendChild(zona);
            conTip(fila, plural(f.v, unidad[0], unidad[1]), f.e);
            g.cuerpo.appendChild(fila);
        });
        tabla(g.figura, encabezadoTabla, filas.map(function (f) { return [f.e, numero.format(f.v)]; }));
    }

    // ----- Barra apilada de estados (parte del total) -----

    function apilada(id, segmentos) {
        var g = cuerpoDe(id);
        if (!g) { return; }
        var total = segmentos.reduce(function (s, x) { return s + x.v; }, 0);
        if (total === 0) {
            vacio(g.cuerpo, 'Todavía no hay tarjetas.');
            return;
        }
        function porcentaje(v) { return Math.round(v / total * 100) + ' %'; }

        var barra = el('div', 'apilada');
        segmentos.forEach(function (s) {
            if (!s.v) { return; }
            var seg = el('span', 'apilada-seg estado-' + s.estado);
            seg.style.flexGrow = s.v;
            conTip(seg, plural(s.v, 'tarjeta', 'tarjetas') + ' (' + porcentaje(s.v) + ')', s.e);
            barra.appendChild(seg);
        });
        g.cuerpo.appendChild(barra);

        // Leyenda: icono + color + etiqueta + valor (el color nunca va solo).
        var leyenda = el('ul', 'leyenda');
        segmentos.forEach(function (s) {
            var item = el('li', 'leyenda-item');
            var muestra = el('span', 'leyenda-muestra estado-' + s.estado, ICONOS[s.estado] || '');
            muestra.setAttribute('aria-hidden', 'true');
            item.appendChild(muestra);
            item.appendChild(el('span', 'leyenda-etiqueta', s.e));
            item.appendChild(el('span', 'leyenda-valor', numero.format(s.v) + ' · ' + porcentaje(s.v)));
            leyenda.appendChild(item);
        });
        g.cuerpo.appendChild(leyenda);
        tabla(g.figura, ['Estado', 'Tarjetas', '%'], segmentos.map(function (s) {
            return [s.e, numero.format(s.v), porcentaje(s.v)];
        }));
    }

    // ----- Columnas por día -----

    // Escala con pasos "redondos" (1, 2, 5 × 10^n) y unas 4 divisiones.
    function escala(maximo) {
        if (maximo <= 0) { return { tope: 4, paso: 1 }; }
        var bruto = maximo / 4;
        var magnitud = Math.pow(10, Math.floor(Math.log(bruto) / Math.LN10));
        var paso = [1, 2, 5, 10].map(function (m) { return m * magnitud; })
                                .filter(function (p) { return p >= bruto; })[0];
        paso = Math.max(1, paso);
        return { tope: Math.ceil(maximo / paso) * paso, paso: paso };
    }

    function columnas(id, dias) {
        var g = cuerpoDe(id);
        if (!g) { return; }
        var maximo = Math.max.apply(null, dias.map(function (d) { return d.v; }));
        var e = escala(maximo);

        var grafica = el('div', 'columnas');
        var area = el('div', 'columnas-area');
        for (var valor = 0; valor <= e.tope; valor += e.paso) {
            var linea = el('div', 'columnas-linea' + (valor === 0 ? ' base' : ''));
            linea.style.bottom = (valor / e.tope * 100) + '%';
            linea.appendChild(el('span', 'columnas-tick', numero.format(valor)));
            area.appendChild(linea);
        }
        var franjas = el('div', 'columnas-franjas');
        dias.forEach(function (d) {
            var franja = el('div', 'columna-franja');
            var col = el('span', 'columna');
            col.style.height = (d.v / e.tope * 100) + '%';
            franja.appendChild(col);
            conTip(franja, plural(d.v, 'cambio', 'cambios'), d.larga);
            franjas.appendChild(franja);
        });
        area.appendChild(franjas);
        grafica.appendChild(area);

        // Eje X: no se rotulan todos los días para que no se pisen.
        var cada = dias.length <= 7 ? 1 : (dias.length <= 31 ? 5 : 15);
        var eje = el('div', 'columnas-eje');
        dias.forEach(function (d, i) {
            var rotular = (dias.length - 1 - i) % cada === 0;
            eje.appendChild(el('span', 'columnas-x', rotular ? d.corta : ''));
        });
        grafica.appendChild(eje);
        g.cuerpo.appendChild(grafica);

        if (maximo === 0) {
            g.cuerpo.appendChild(el('p', 'grafica-vacia', 'No hubo cambios en este periodo.'));
        }
        tabla(g.figura, ['Día', 'Cambios'], dias.map(function (d) { return [d.larga, numero.format(d.v)]; }));
    }

    // ----- Montaje -----

    barras('grafica-por-lista', datos.porLista, ['tarjeta', 'tarjetas'], ['Lista', 'Tarjetas'], 'El tablero no tiene listas.');
    barras('grafica-por-persona', datos.porPersona, ['tarjeta', 'tarjetas'], ['Persona', 'Tarjetas'], 'Todavía no hay tarjetas.');
    apilada('grafica-fechas', datos.fechas);
    columnas('grafica-por-dia', datos.porDia);
    barras('grafica-cambios-persona', datos.cambiosPersona, ['cambio', 'cambios'], ['Persona', 'Cambios'], 'No hubo cambios en este periodo.');
    barras('grafica-cambios-tipo', datos.cambiosTipo, ['cambio', 'cambios'], ['Tipo', 'Cambios'], 'No hubo cambios en este periodo.');
}());
