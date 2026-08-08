// Elimina un registro via AJAX, con un modal de confirmacion propio (no el confirm() nativo)
function eliminarRegistro(url, filaId, mensajeConfirm) {
    const modalEl = document.getElementById('modalConfirmar');
    const modal = bootstrap.Modal.getOrCreateInstance(modalEl);
    document.getElementById('modalConfirmarTexto').textContent =
        mensajeConfirm || '¿Seguro que deseas eliminar este registro?';

    const btnConfirmar = document.getElementById('modalConfirmarBtn');
    const nuevoBtn = btnConfirmar.cloneNode(true); // evita que se acumulen listeners de clics anteriores
    btnConfirmar.replaceWith(nuevoBtn);

    nuevoBtn.addEventListener('click', async () => {
        modal.hide();
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

        try {
            const res = await fetch(url, {
                method: 'POST',
                headers: { 'RequestVerificationToken': token }
            });
            const data = await res.json();

            if (data.success) {
                document.getElementById(filaId)?.remove();
                mostrarToast(data.mensaje || 'Eliminado correctamente.', 'success');
            } else {
                mostrarToast(data.mensaje || 'No se pudo eliminar.', 'danger');
            }
        } catch {
            mostrarToast('Error de conexión con el servidor.', 'danger');
        }
    });

    modal.show();
}


// Muestra una notificacion flotante 
function mostrarToast(mensaje, tipo) {
    const cont = document.getElementById('toastContainer');
    if (!cont) { alert(mensaje); return; }

    const div = document.createElement('div');
    div.className = `toast-cine toast-cine-${tipo === 'danger' ? 'error' : 'success'}`;
    div.innerHTML = `
        <span class="toast-cine-icon">${tipo === 'danger' ? '✕' : '✓'}</span>
        <span class="toast-cine-msg">${mensaje}</span>
    `;
    cont.appendChild(div);

    setTimeout(() => div.classList.add('toast-cine-out'), 2600);
    setTimeout(() => div.remove(), 3000);
}