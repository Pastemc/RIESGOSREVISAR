// ── DESCARGA DE ARCHIVOS ──
window.downloadFile = function (base64, fileName, mimeType) {
    var link = document.createElement('a');
    link.href = 'data:' + mimeType + ';base64,' + base64;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
};

// Alias usado por CargaMasiva.razor → saveAsFile(fileName, mimeType, base64)
window.saveAsFile = function (fileName, mimeType, base64) {
    var link = document.createElement('a');
    link.href = 'data:' + mimeType + ';base64,' + base64;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
};

// ── DARK / LIGHT MODE ──
window.initTheme = function () {
    const saved = localStorage.getItem('riesgoselor-theme') || 'light';
    document.documentElement.setAttribute('data-theme', saved);
    return saved;
};

window.setTheme = function (theme) {
    document.documentElement.setAttribute('data-theme', theme);
    localStorage.setItem('riesgoselor-theme', theme);
};

window.getTheme = function () {
    return localStorage.getItem('riesgoselor-theme') || 'light';
};