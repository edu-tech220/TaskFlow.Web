// Tema claro/oscuro: Bootstrap 5.3 cambia de paleta con data-bs-theme en <html>.
// El tema inicial se aplica con un script en línea en el <head> (antes de pintar,
// para evitar el parpadeo); aquí solo se maneja el botón.
(function () {
  const root = document.documentElement;

  function syncIcons() {
    const dark = root.getAttribute('data-bs-theme') === 'dark';
    document.querySelectorAll('[data-theme-icon]').forEach(i => {
      i.className = dark ? 'bi bi-sun' : 'bi bi-moon-stars';
    });
  }

  document.addEventListener('click', e => {
    const btn = e.target.closest('[data-theme-toggle]');
    if (!btn) return;
    const next = root.getAttribute('data-bs-theme') === 'dark' ? 'light' : 'dark';
    root.setAttribute('data-bs-theme', next);
    try { localStorage.setItem('tf-theme', next); } catch { /* modo privado: sin persistencia */ }
    syncIcons();
  });

  // Confirmaciones: <form data-confirm="¿Seguro?">
  document.addEventListener('submit', e => {
    const msg = e.target.getAttribute('data-confirm');
    if (msg && !confirm(msg)) e.preventDefault();
  });

  document.addEventListener('DOMContentLoaded', syncIcons);
})();
