/*
 * Gráficas de Estadisticas.aspx, sin dependencias.
 * Lee el JSON de #datos-graficas y dibuja con HTML/CSS:
 *  - barras horizontales (una serie: color de serie 1; "Sin asignar" en gris de baja énfasis; opcionalmente
 *    una muestra de color junto al nombre —p. ej. el de cada etiqueta— y un enlace),
 *  - una barra apilada de estados (paleta de estado + icono + etiqueta),
 *  - columnas por día,
 *  - anillos ("quesos") para parte del total: como mucho 6 porciones (el resto se agrupa en "Otras", en gris),
 *    colores categóricos en orden fijo (--cat-1…5, validados en ciclo) o de prioridad con icono + etiqueta.
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
    var ICONOS_PRIORIDAD = { alta: '▲', media: '◆', baja: '▼', sin: '–' };
    var SVG = 'http://www.w3.org/2000/svg';
    var COLOR = /^#[0-9a-f]{6}$/i;

    // Solo rutas del propio sitio ("/..."), nunca "//otro-sitio" ni "javascript:".
    function enlaceLocal(url) {
        return typeof url === 'string' && url.charAt(0) === '/' && url.charAt(1) !== '/' && url.charAt(1) !== '\\';
    }

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
            var etiqueta = el(enlaceLocal(f.url) ? 'a' : 'span', 'graf-etiqueta', f.e);
            etiqueta.title = f.e;
            if (enlaceLocal(f.url)) { etiqueta.setAttribute('href', f.url); }
            if (COLOR.test(f.color || '')) {
                // La identidad la lleva una muestra junto al texto; el texto sigue en color de tinta.
                var muestra = el('span', 'graf-muestra');
                muestra.style.backgroundColor = f.color;
                muestra.setAttribute('aria-hidden', 'true');
                etiqueta.insertBefore(muestra, etiqueta.firstChild);
            }
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

    // ----- Anillo (parte del total) -----

    // filas: [{ e, v, clase, icono }]. Solo entran las porciones con valor; si hay más de 6, se quedan las 5
    // mayores (en su orden original, que fija el color) y el resto se suma en "Otras" (gris de baja énfasis).
    function porciones(filas, nombreOtras) {
        var conValor = filas.filter(function (f) { return f.v > 0; });
        if (conValor.length <= 6) { return conValor; }
        var mayores = conValor.slice().sort(function (a, b) { return b.v - a.v; }).slice(0, 5);
        var resto = 0;
        var quedan = conValor.filter(function (f) {
            if (mayores.indexOf(f) >= 0) { return true; }
            resto += f.v;
            return false;
        });
        quedan.push({ e: nombreOtras, v: resto, clase: 'otras' });
        return quedan;
    }

    function anillo(id, filas, o) {
        var g = cuerpoDe(id);
        if (!g) { return; }
        var total = filas.reduce(function (s, f) { return s + f.v; }, 0);
        if (total === 0) {
            vacio(g.cuerpo, o.vacio);
            return;
        }
        var partes = porciones(filas, o.otras || 'Otras');
        function porcentaje(v) { return Math.round(v / total * 100) + ' %'; }

        var raiz = el('div', 'anillo');
        var grafico = el('div', 'anillo-grafico');
        var svg = document.createElementNS(SVG, 'svg');
        svg.setAttribute('viewBox', '0 0 120 120');
        svg.setAttribute('aria-hidden', 'true');   // la información accesible está en la leyenda y en la tabla

        var R = 46, C = 2 * Math.PI * R;
        var HUECO = partes.length > 1 ? 1.5 : 0;   // separación (color de la superficie) entre porciones
        var inicio = 0;
        var segmentos = [];
        partes.forEach(function (f, i) {
            var largo = f.v / total * C;
            var c = document.createElementNS(SVG, 'circle');
            c.setAttribute('cx', '60');
            c.setAttribute('cy', '60');
            c.setAttribute('r', String(R));
            c.setAttribute('class', 'anillo-seg ' + (f.clase || ('cat-' + (i + 1))));
            var visible = Math.max(largo - HUECO, 0.6);
            c.setAttribute('stroke-dasharray', visible + ' ' + (C - visible));
            c.setAttribute('stroke-dashoffset', String(-inicio));
            c.setAttribute('transform', 'rotate(-90 60 60)');   // empieza arriba, sentido horario
            svg.appendChild(c);
            segmentos.push(c);
            inicio += largo;
        });
        grafico.appendChild(svg);

        // Centro: el total; al señalar una porción, su valor y su nombre.
        var centro = el('div', 'anillo-centro');
        var centroValor = el('strong');
        var centroTexto = el('span');
        centro.appendChild(centroValor);
        centro.appendChild(centroTexto);
        grafico.appendChild(centro);
        function centroTotal() {
            centroValor.textContent = numero.format(total);
            centroTexto.textContent = o.textoTotal;
        }
        centroTotal();
        raiz.appendChild(grafico);

        // Leyenda: muestra (con icono si lo hay) + nombre + valor y %; es la parte que se recorre con el teclado.
        var leyenda = el('ul', 'leyenda anillo-leyenda');
        var items = partes.map(function (f, i) {
            var item = el('li', 'leyenda-item');
            var muestra = el('span', 'leyenda-muestra ' + (f.clase || ('cat-' + (i + 1))), f.icono || '');
            muestra.setAttribute('aria-hidden', 'true');
            var nombre = el('span', 'leyenda-etiqueta', f.e);
            nombre.title = f.e;
            item.appendChild(muestra);
            item.appendChild(nombre);
            item.appendChild(el('span', 'leyenda-valor', numero.format(f.v) + ' · ' + porcentaje(f.v)));
            item.tabIndex = 0;
            item.setAttribute('aria-label', f.e + ': ' + plural(f.v, o.unidad[0], o.unidad[1]) + ' (' + porcentaje(f.v) + ')');
            leyenda.appendChild(item);
            return item;
        });
        raiz.appendChild(leyenda);

        function resaltar(i) {
            raiz.classList.add('resaltando');
            segmentos.forEach(function (s, j) { s.classList.toggle('activo', i === j); });
            items.forEach(function (it, j) { it.classList.toggle('activo', i === j); });
            centroValor.textContent = porcentaje(partes[i].v);
            centroTexto.textContent = partes[i].e;
        }
        function quitar() {
            raiz.classList.remove('resaltando');
            segmentos.forEach(function (s) { s.classList.remove('activo'); });
            items.forEach(function (it) { it.classList.remove('activo'); });
            centroTotal();
        }
        partes.forEach(function (f, i) {
            [segmentos[i], items[i]].forEach(function (m) {
                m.addEventListener('pointerenter', function () { resaltar(i); });
                m.addEventListener('pointerleave', quitar);
            });
            items[i].addEventListener('focus', function () { resaltar(i); });
            items[i].addEventListener('blur', quitar);
        });

        g.cuerpo.appendChild(raiz);
        // La tabla lleva todas las filas (también las agrupadas en "Otras" y las que están a cero).
        tabla(g.figura, o.encabezado.concat('%'), filas.map(function (f) {
            return [f.e, numero.format(f.v), porcentaje(f.v)];
        }));
    }

    // ----- Montaje -----

    anillo('grafica-por-lista', datos.porLista, {
        unidad: ['tarjeta', 'tarjetas'], encabezado: ['Lista', 'Tarjetas'], textoTotal: 'tarjetas',
        otras: 'Otras listas', vacio: 'Todavía no hay tarjetas.'
    });
    anillo('grafica-por-prioridad', (datos.porPrioridad || []).map(function (f) {
        return { e: f.e, v: f.v, clase: 'prio-' + f.clave, icono: ICONOS_PRIORIDAD[f.clave] || '' };
    }), {
        unidad: ['tarjeta', 'tarjetas'], encabezado: ['Prioridad', 'Tarjetas'], textoTotal: 'tarjetas',
        vacio: 'Todavía no hay tarjetas.'
    });
    barras('grafica-antiguedad', datos.antiguedad || [], ['tarjeta', 'tarjetas'], ['Antigüedad', 'Tarjetas'],
           'Todavía no hay tarjetas.');
    barras('grafica-por-persona', datos.porPersona, ['tarjeta', 'tarjetas'], ['Persona', 'Tarjetas'], 'Todavía no hay tarjetas.');
    apilada('grafica-fechas', datos.fechas);
    columnas('grafica-por-dia', datos.porDia);
    barras('grafica-cambios-persona', datos.cambiosPersona, ['cambio', 'cambios'], ['Persona', 'Cambios'], 'No hubo cambios en este periodo.');
    barras('grafica-por-etiqueta', datos.porEtiqueta || [], ['tarjeta', 'tarjetas'], ['Etiqueta', 'Tarjetas'],
           'Todavía no hay tarjetas con etiquetas.');
    anillo('grafica-cambios-tipo', datos.cambiosTipo, {
        unidad: ['cambio', 'cambios'], encabezado: ['Tipo', 'Cambios'], textoTotal: 'cambios',
        vacio: 'No hubo cambios en este periodo.'
    });
}());
