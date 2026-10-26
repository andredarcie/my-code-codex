// Comportamentos do site: menu lateral no celular, "Nesta página" e botão de copiar código.
// Tudo é melhoria progressiva: sem JavaScript o conteúdo e a navegação continuam lá.
(function () {
  var estreito = window.matchMedia('(max-width: 60rem)');

  // Menu lateral: aberto no desktop, recolhido no celular.
  var menu = document.querySelector('.side-menu');
  function ajustarMenu() {
    if (!menu) return;
    if (estreito.matches) menu.removeAttribute('open');
    else menu.setAttribute('open', '');
  }
  ajustarMenu();
  if (estreito.addEventListener) estreito.addEventListener('change', ajustarMenu);

  // Nesta página: os h2 do artigo, com a seção visível marcada.
  var toc = document.querySelector('.toc');
  var titulos = Array.prototype.slice.call(document.querySelectorAll('.prose h2[id]'));
  if (toc && titulos.length >= 2) {
    var lista = toc.querySelector('ol');
    var links = titulos.map(function (h) {
      var li = document.createElement('li');
      var a = document.createElement('a');
      a.href = '#' + h.id;
      a.textContent = h.textContent;
      li.appendChild(a);
      lista.appendChild(li);
      return a;
    });
    toc.hidden = false;

    if ('IntersectionObserver' in window) {
      var visiveis = new Set();
      var obs = new IntersectionObserver(function (entradas) {
        entradas.forEach(function (e) {
          if (e.isIntersecting) visiveis.add(e.target); else visiveis.delete(e.target);
        });
        var atual = titulos.find(function (h) { return visiveis.has(h); });
        if (!atual) return;
        links.forEach(function (a, i) {
          if (titulos[i] === atual) a.setAttribute('aria-current', 'true');
          else a.removeAttribute('aria-current');
        });
      }, { rootMargin: '0px 0px -70% 0px' });
      titulos.forEach(function (h) { obs.observe(h); });
    }
  }

  // Copiar código.
  var blocos = document.querySelectorAll('.prose .highlight, .graphite-band .highlight');
  Array.prototype.forEach.call(blocos, function (bloco) {
    var pre = bloco.querySelector('pre');
    if (!pre || !navigator.clipboard) return;
    var botao = document.createElement('button');
    botao.type = 'button';
    botao.className = 'copy';
    botao.textContent = 'Copiar';
    botao.setAttribute('aria-label', 'Copiar o código');
    var timer = 0;
    botao.addEventListener('click', function () {
      navigator.clipboard.writeText(pre.innerText.replace(/\n$/, '')).then(function () {
        botao.textContent = 'Copiado';
        botao.dataset.state = 'copied';
      }, function () {
        botao.textContent = 'Não copiou';
        botao.dataset.state = 'copied';
      }).then(function () {
        clearTimeout(timer);
        timer = setTimeout(function () {
          botao.textContent = 'Copiar';
          delete botao.dataset.state;
        }, 1800);
      });
    });
    bloco.appendChild(botao);
  });
})();
