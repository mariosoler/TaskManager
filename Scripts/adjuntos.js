/*
 * Adjuntos del detalle de tarjeta (sección bajo las subtareas), de cualquier tipo:
 * - se suben arrastrándolos a la tarjeta (a cualquier parte del detalle), pegando del portapapeles (una captura o
 *   archivos copiados; Ctrl+V con el detalle abierto) o con «Elegir archivos…»; una petición por archivo a
 *   Adjuntos.ashx (accion=subir);
 * - las imágenes se ven en miniatura; al pulsar una se abre un visor emergente (anterior/siguiente con ← →,
 *   con animación de deslizamiento,
 *   Esc o ✕ para cerrar, abrir el original y eliminar); el resto de archivos se listan con su nombre (descarga),
 *   tamaño y quién los subió;
 * - cada adjunto tiene ✕ arriba a la derecha para eliminarlo (pide confirmación);
 * - solo en tarjetas ya creadas (al crear una tarjeta no hay Id al que asociarlos).
 * Datos iniciales en <script type="application/json" id="adjuntos-datos"> (TableroPagina.AdjuntosJson); el
 * servidor devuelve la lista actualizada tras cada cambio. Los textos se insertan con textContent y las URL se validan.
 */
(function () {
    'use strict';

    var cfg = window.TaskManagerAdjuntos;
    if (!cfg || !(cfg.tarjeta > 0)) { return; }
    var galeria = document.getElementById(cfg.galeria);
    var lista = document.getElementById(cfg.lista);
    var zona = document.getElementById(cfg.zona);
    var selector = document.getElementById(cfg.selector);
    var estado = document.getElementById(cfg.estado);
    var total = document.getElementById(cfg.total);
    var modal = document.querySelector(cfg.modal);
    if (!galeria || !lista || !zona || !selector || !modal) { return; }

    var URL_LOCAL = /^\/[^\s"'<>]*$/;     // solo rutas de esta web (las genera el servidor)

    var adjuntos = leerDatos();
    var subiendo = false;
    var cola = [];
    var hechas = 0;        // archivos subidos en la tanda actual
    var avisos = [];       // descartados o subidas fallidas de la tanda (se muestran al terminar)

    function leerDatos() {
        try { return validar(JSON.parse(document.getElementById(cfg.datos).textContent)); } catch (e) { return []; }
    }

    function validar(datos) {
        return (datos || []).filter(function (a) { return a && typeof a.i === 'number' && URL_LOCAL.test(a.u || ''); });
    }

    function imagenes() { return adjuntos.filter(function (a) { return a.img; }); }

    function el(etiqueta, clase, texto) {
        var e = document.createElement(etiqueta);
        if (clase) { e.className = clase; }
        if (texto !== undefined) { e.textContent = texto; }
        return e;
    }

    function tamano(bytes) {
        if (bytes < 1024) { return bytes + ' B'; }
        if (bytes < 1048576) { return Math.round(bytes / 1024) + ' KB'; }
        return (bytes / 1048576).toLocaleString('es-ES', { maximumFractionDigits: 1 }) + ' MB';
    }

    // Icono según la extensión del nombre (solo decorativo: el nombre siempre se ve).
    function icono(nombre) {
        var ext = (String(nombre).split('.').pop() || '').toLowerCase();
        if (ext === 'pdf') { return '📕'; }
        if (/^(doc|docx|odt|rtf|txt|md)$/.test(ext)) { return '📝'; }
        if (/^(xls|xlsx|ods|csv)$/.test(ext)) { return '📊'; }
        if (/^(ppt|pptx|odp)$/.test(ext)) { return '📽'; }
        if (/^(zip|rar|7z|gz|tar)$/.test(ext)) { return '🗜'; }
        if (/^(mp4|mov|avi|mkv|webm)$/.test(ext)) { return '🎞'; }
        if (/^(mp3|wav|ogg|m4a)$/.test(ext)) { return '🎵'; }
        return '📄';
    }

    function mostrarEstado(texto, clase) {
        if (!estado) { return; }
        estado.textContent = texto || '';
        estado.className = 'adjuntos-estado' + (clase ? ' ' + clase : '');
    }

    function botonEliminar(a, clase) {
        var b = el('button', clase, '✕');
        b.type = 'button';
        b.title = 'Eliminar';
        b.setAttribute('aria-label', 'Eliminar ' + a.n);
        b.addEventListener('click', function (e) {
            e.stopPropagation();
            eliminar(a);
        });
        return b;
    }

    // ----- Miniaturas y lista -----
    function pintar() {
        if (total) { total.textContent = adjuntos.length ? String(adjuntos.length) : ''; }
        var fotos = imagenes();

        galeria.textContent = '';
        galeria.hidden = fotos.length === 0;
        fotos.forEach(function (a, i) {
            var caja = el('div', 'adjunto-foto');
            var ver = el('button', 'adjunto-miniatura');
            ver.type = 'button';
            ver.title = a.n;
            ver.setAttribute('aria-label', 'Ver ' + a.n);
            var img = el('img');
            img.src = a.u;
            img.alt = '';
            img.loading = 'lazy';
            ver.appendChild(img);
            ver.addEventListener('click', function () { abrirVisor(i, ver); });
            caja.appendChild(ver);
            caja.appendChild(botonEliminar(a, 'adjunto-eliminar'));
            galeria.appendChild(caja);
        });

        lista.textContent = '';
        var otros = adjuntos.filter(function (a) { return !a.img; });
        lista.hidden = otros.length === 0;
        otros.forEach(function (a) {
            var li = el('li', 'adjunto-archivo');
            li.appendChild(el('span', 'adjunto-icono', icono(a.n))).setAttribute('aria-hidden', 'true');
            var datos = el('span', 'adjunto-datos');
            var enlace = el('a', 'adjunto-nombre', a.n);
            enlace.href = a.u;      // el servidor lo envía como descarga
            enlace.title = 'Descargar ' + a.n;
            datos.appendChild(enlace);
            datos.appendChild(el('small', null, tamano(a.t) + ' · ' + (a.a || '') + (a.f ? ' · ' + a.f : '')));
            li.appendChild(datos);
            li.appendChild(botonEliminar(a, 'adjunto-eliminar adjunto-eliminar-fila'));
            lista.appendChild(li);
        });
    }

    // ----- Visor emergente de imágenes -----
    var visor = null, visorImg, visorPie, visorOriginal, visorAnterior, visorSiguiente, visorCerrar;
    var visorIndice = 0, visorOrigen = null;

    function crearVisor() {
        visor = el('div', 'visor-fondo');
        visor.hidden = true;
        visor.setAttribute('role', 'dialog');
        visor.setAttribute('aria-modal', 'true');
        visor.setAttribute('aria-label', 'Imagen adjunta');

        var marco = el('div', 'visor');
        visorCerrar = el('button', 'visor-cerrar', '✕');
        visorCerrar.type = 'button';
        visorCerrar.setAttribute('aria-label', 'Cerrar (Esc)');
        visorCerrar.addEventListener('click', cerrarVisor);
        visorImg = el('img', 'visor-imagen');
        visorAnterior = el('button', 'visor-nav visor-anterior', '‹');
        visorAnterior.type = 'button';
        visorAnterior.setAttribute('aria-label', 'Imagen anterior');
        visorAnterior.addEventListener('click', function () { mostrarEnVisor(visorIndice - 1, -1); });
        visorSiguiente = el('button', 'visor-nav visor-siguiente', '›');
        visorSiguiente.type = 'button';
        visorSiguiente.setAttribute('aria-label', 'Imagen siguiente');
        visorSiguiente.addEventListener('click', function () { mostrarEnVisor(visorIndice + 1, 1); });

        var barra = el('div', 'visor-barra');
        visorPie = el('span', 'visor-pie');
        visorOriginal = el('a', 'visor-accion', 'Abrir original');
        visorOriginal.target = '_blank';
        visorOriginal.rel = 'noopener';
        var borrar = el('button', 'visor-accion visor-eliminar', 'Eliminar');
        borrar.type = 'button';
        borrar.addEventListener('click', function () {
            var a = imagenes()[visorIndice];
            if (a) { eliminar(a); }
        });
        barra.appendChild(visorPie);
        barra.appendChild(visorOriginal);
        barra.appendChild(borrar);

        marco.appendChild(visorImg);
        marco.appendChild(visorAnterior);
        marco.appendChild(visorSiguiente);
        marco.appendChild(visorCerrar);
        visor.appendChild(marco);
        visor.appendChild(barra);

        // Pulsar fuera de la imagen cierra el visor (no el detalle de la tarjeta).
        visor.addEventListener('click', function (e) { if (e.target === visor) { cerrarVisor(); } });
        visor.addEventListener('keydown', function (e) {
            if (e.key === 'Escape') { e.preventDefault(); e.stopPropagation(); cerrarVisor(); }
            else if (e.key === 'ArrowLeft') { e.preventDefault(); mostrarEnVisor(visorIndice - 1, -1); }
            else if (e.key === 'ArrowRight') { e.preventDefault(); mostrarEnVisor(visorIndice + 1, 1); }
            else if (e.key === 'Tab') {
                // El foco no sale del visor mientras está abierto.
                var focos = Array.prototype.filter.call(visor.querySelectorAll('button, a[href]'), function (n) { return !n.hidden; });
                var primero = focos[0], ultimo = focos[focos.length - 1];
                if (e.shiftKey && document.activeElement === primero) { e.preventDefault(); ultimo.focus(); }
                else if (!e.shiftKey && document.activeElement === ultimo) { e.preventDefault(); primero.focus(); }
            }
        });
        document.body.appendChild(visor);
    }

    function abrirVisor(indice, origen) {
        if (!visor) { crearVisor(); }
        visorOrigen = origen || null;
        visor.hidden = false;
        mostrarEnVisor(indice);
        visorCerrar.focus();
    }

    // Animación al cambiar de imagen: entra deslizándose desde el lado hacia el que se avanza (direccion = 1
    // siguiente, -1 anterior) o, al abrir el visor, aparece ampliándose. Con «reducir movimiento» no se anima (CSS).
    var CLASES_ANIMACION = ['visor-desde-derecha', 'visor-desde-izquierda', 'visor-aparece'];

    function animar(direccion) {
        CLASES_ANIMACION.forEach(function (c) { visorImg.classList.remove(c); });
        void visorImg.offsetWidth;   // reinicia la animación aunque se repita la misma clase
        visorImg.classList.add(direccion > 0 ? 'visor-desde-derecha' : direccion < 0 ? 'visor-desde-izquierda' : 'visor-aparece');
    }

    function mostrarEnVisor(indice, direccion) {
        var fotos = imagenes();
        if (fotos.length === 0) { cerrarVisor(); return; }
        var anterior = visorIndice;
        visorIndice = (indice + fotos.length) % fotos.length;
        var a = fotos[visorIndice];
        if (direccion === undefined || fotos.length > 1 || visorIndice !== anterior) { animar(direccion || 0); }
        visorImg.src = a.u;
        visorImg.alt = a.n;
        visorOriginal.href = a.u;
        visorPie.textContent = a.n + ' · ' + (visorIndice + 1) + ' / ' + fotos.length + ' · ' + (a.a || '') + (a.f ? ' · ' + a.f : '');
        visorAnterior.hidden = visorSiguiente.hidden = fotos.length < 2;
    }

    function cerrarVisor() {
        if (!visor || visor.hidden) { return; }
        visor.hidden = true;
        visorImg.removeAttribute('src');
        if (visorOrigen && document.body.contains(visorOrigen)) { visorOrigen.focus(); }
        visorOrigen = null;
    }

    // ----- Peticiones -----
    function enviar(cuerpo) {
        return fetch(cfg.url, {
            method: 'POST',
            credentials: 'same-origin',
            headers: { 'X-Chat-Token': cfg.token },
            body: cuerpo
        }).then(function (r) {
            return r.json().catch(function () { return {}; }).then(function (d) {
                if (!r.ok || !d.ok) {
                    // 404/413 sin JSON: lo más probable es que el archivo supere el límite de tamaño de la petición.
                    throw new Error(d.error || (r.status === 404 || r.status === 413
                        ? 'El archivo es demasiado grande.' : 'No se ha podido completar la acción.'));
                }
                return validar(d.adjuntos);
            });
        });
    }

    function datosBase(accion) {
        var datos = new FormData();
        datos.append('accion', accion);
        datos.append('tablero', String(cfg.tablero));
        datos.append('tarjeta', String(cfg.tarjeta));
        return datos;
    }

    function eliminar(a) {
        if (!window.confirm('¿Eliminar «' + a.n + '»? No se puede deshacer.')) { return; }
        var datos = datosBase('eliminar');
        datos.append('adjunto', String(a.i));
        mostrarEstado('Eliminando…', 'guardando');
        enviar(datos).then(function (nuevos) {
            adjuntos = nuevos;
            pintar();
            if (visor && !visor.hidden) { mostrarEnVisor(visorIndice); }
            mostrarEstado('✓ Adjunto eliminado', 'ok');
        }).catch(function (e) { mostrarEstado(e.message, 'error'); });
    }

    // ----- Subida (un archivo por petición, en orden) -----
    function subir(archivos) {
        var validos = [], descartados = [];
        Array.prototype.forEach.call(archivos || [], function (a) {
            if (!a.size) { descartados.push((a.name || 'archivo') + ' (vacío o es una carpeta)'); }
            else if (a.size > cfg.tamanoMaximo) { descartados.push((a.name || 'archivo') + ' (más de ' + Math.round(cfg.tamanoMaximo / 1048576) + ' MB)'); }
            else { validos.push(a); }
        });
        if (descartados.length) { avisos.push('No se suben: ' + descartados.join(', ')); }
        cola = cola.concat(validos);
        if (!subiendo) { siguienteSubida(); }
    }

    function siguienteSubida() {
        if (cola.length === 0) {
            subiendo = false;
            zona.classList.remove('subiendo');
            var hecho = hechas === 0 ? '' : (hechas === 1 ? '✓ Archivo subido' : '✓ ' + hechas + ' archivos subidos');
            mostrarEstado([hecho].concat(avisos).filter(Boolean).join(' · '), avisos.length ? 'error' : 'ok');
            hechas = 0;
            avisos = [];
            return;
        }
        subiendo = true;
        zona.classList.add('subiendo');
        var archivo = cola.shift();
        mostrarEstado('Subiendo ' + (hechas + 1) + ' de ' + (hechas + cola.length + 1) + '…', 'guardando');
        var datos = datosBase('subir');
        datos.append('archivo', archivo, archivo.name || 'captura.png');
        enviar(datos).then(function (nuevos) {
            adjuntos = nuevos;
            pintar();
            hechas++;
            siguienteSubida();
        }).catch(function (e) {
            avisos.push((archivo.name ? archivo.name + ': ' : '') + e.message);
            siguienteSubida();
        });
    }

    selector.addEventListener('change', function () {
        subir(selector.files);
        selector.value = '';   // permite volver a elegir el mismo archivo
    });

    // ----- Arrastrar a la tarjeta -----
    function llevaArchivos(e) {
        var tipos = e.dataTransfer && e.dataTransfer.types;
        return !!tipos && Array.prototype.indexOf.call(tipos, 'Files') >= 0;
    }

    var profundidad = 0;   // dragenter/dragleave se disparan en cada hijo: se cuentan
    modal.addEventListener('dragenter', function (e) {
        if (!llevaArchivos(e)) { return; }
        e.preventDefault();
        profundidad++;
        modal.classList.add('soltar-adjuntos');
    });
    modal.addEventListener('dragover', function (e) {
        if (!llevaArchivos(e)) { return; }
        e.preventDefault();
        e.dataTransfer.dropEffect = 'copy';
    });
    modal.addEventListener('dragleave', function (e) {
        if (!llevaArchivos(e)) { return; }
        profundidad = Math.max(0, profundidad - 1);
        if (profundidad === 0) { modal.classList.remove('soltar-adjuntos'); }
    });
    modal.addEventListener('drop', function (e) {
        if (!llevaArchivos(e)) { return; }
        e.preventDefault();
        e.stopPropagation();
        profundidad = 0;
        modal.classList.remove('soltar-adjuntos');
        subir(e.dataTransfer.files);
    });
    // Fuera del detalle, soltar un archivo no debe abrirlo en el navegador (se perdería la página).
    ['dragover', 'drop'].forEach(function (tipo) {
        document.addEventListener(tipo, function (e) { if (llevaArchivos(e)) { e.preventDefault(); } });
    });

    // ----- Pegar del portapapeles (una captura o archivos copiados) -----
    document.addEventListener('paste', function (e) {
        var datos = e.clipboardData;
        if (!datos || !datos.items) { return; }
        var archivos = [];
        Array.prototype.forEach.call(datos.items, function (item) {
            if (item.kind === 'file') {
                var archivo = item.getAsFile();
                if (archivo) { archivos.push(archivo); }
            }
        });
        if (!archivos.length) { return; }   // texto normal: se pega como siempre
        e.preventDefault();
        subir(archivos);
    });

    pintar();
}());
