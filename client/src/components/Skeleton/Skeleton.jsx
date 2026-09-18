export default function Skeleton({ width = '100%', height = '20px', borderRadius = 'var(--radius-sm)', className = '' }) {
  return (
    <div
      className={className}
      style={{
        width,
        height,
        borderRadius,
        backgroundColor: 'var(--slate-200)',
        backgroundImage: 'linear-gradient(90deg, var(--slate-200) 0px, var(--slate-100) 40px, var(--slate-200) 80px)',
        backgroundSize: '300% 100%',
        animation: 'skeletonShimmer 1.5s infinite ease-out',
      }}
    >
      <style>{`
        @keyframes skeletonShimmer {
          0% { background-position: 100% 0; }
          100% { background-position: -100% 0; }
        }
      `}</style>
    </div>
  );
}
