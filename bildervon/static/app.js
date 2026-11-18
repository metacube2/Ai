document.addEventListener("submit", (e) => {
  if (e.target.classList.contains("loeschen") && !confirm("Wirklich löschen?")) e.preventDefault();
});
