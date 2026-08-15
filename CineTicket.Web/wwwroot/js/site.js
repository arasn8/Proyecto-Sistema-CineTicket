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
// ---------- Panel de autenticacion (login / registro / recuperar) ----------
function mostrarAuthTab(id, btn) {
    document.querySelectorAll('.auth-pane').forEach(p => p.style.display = 'none');
    document.getElementById(id).style.display = 'block';
    document.getElementById('authFeedback').innerHTML = '';
    if (btn) {
        document.querySelectorAll('.auth-tabs .nav-link').forEach(b => b.classList.remove('active'));
        btn.classList.add('active');
    }
}

function feedbackAuth(mensaje, tipo) {
    document.getElementById('authFeedback').innerHTML =
        `<div class="alert alert-${tipo === 'error' ? 'danger' : 'success'} py-2">${mensaje}</div>`;
}

async function postAuth(url, form) {
    const token = form.querySelector('input[name="__RequestVerificationToken"]').value;
    const body = new URLSearchParams(new FormData(form));
    const res = await fetch(url, { method: 'POST', headers: { 'RequestVerificationToken': token }, body });
    return res.json();
}

async function enviarLogin(e) {
    e.preventDefault();
    const data = await postAuth('/Account/LoginAjax', e.target);
    if (data.success) { location.reload(); } else { feedbackAuth(data.mensaje, 'error'); }
    return false;
}

async function enviarRegistro(e) {
    e.preventDefault();
    const data = await postAuth('/Account/RegisterAjax', e.target);
    if (data.success) {
        feedbackAuth(data.mensaje, 'success');
        setTimeout(() => mostrarAuthTab('pane-login', document.querySelector('.auth-tabs .nav-link')), 1200);
    } else {
        feedbackAuth(data.mensaje, 'error');
    }
    return false;
}

async function enviarForgot(e) {
    e.preventDefault();
    const data = await postAuth('/Account/ForgotPasswordAjax', e.target);
    if (data.success) {
        feedbackAuth(`${data.mensaje} Tu código (demo): <strong>${data.codigoDemo}</strong>`, 'success');
        document.getElementById('formReset').style.display = 'block';
    } else {
        feedbackAuth(data.mensaje, 'error');
    }
    return false;
}

async function enviarReset(e) {
    e.preventDefault();
    const formData = new FormData(e.target);
    formData.append('correo', document.getElementById('forgotCorreo').value);
    const token = e.target.querySelector('input[name="__RequestVerificationToken"]').value;
    const res = await fetch('/Account/ResetPasswordAjax', {
        method: 'POST', headers: { 'RequestVerificationToken': token }, body: new URLSearchParams(formData)
    });
    const data = await res.json();
    if (data.success) {
        feedbackAuth(data.mensaje, 'success');
        setTimeout(() => mostrarAuthTab('pane-login', document.querySelector('.auth-tabs .nav-link')), 1200);
    } else {
        feedbackAuth(data.mensaje, 'error');
    }
    return false;
}