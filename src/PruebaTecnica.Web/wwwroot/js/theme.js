// Interop minimal para persistir la preferencia de tema en localStorage.
// Deliberadamente no se usa ningún framework JS: es la única interacción con el DOM
// que Blazor Server no puede resolver sin un round-trip (evitar parpadeo en el primer render).

export function obtenerTema() {
    return document.documentElement.getAttribute('data-theme') || 'light';
}

export function establecerTema(tema) {
    document.documentElement.setAttribute('data-theme', tema);
    localStorage.setItem('pt-theme', tema);
}
