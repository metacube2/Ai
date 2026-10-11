// Aurora-Verlauf, Sterne und perspektivisches Blueprint-Raster (reines CSS).
export default function HeroBackdrop({ floor = true }: { floor?: boolean }) {
  return (
    <div className="aurora" aria-hidden="true">
      <span />
      <span />
      <span />
      <div className="stars" />
      {floor && <div className="blueprint-floor" />}
    </div>
  );
}
