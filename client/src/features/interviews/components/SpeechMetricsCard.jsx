import { Activity, AlertTriangle, Info, Clock, Mic } from 'lucide-react';

export default function SpeechMetricsCard({ metrics }) {
  if (!metrics) return null;

  const {
    wordsPerMinute = 0,
    pauseCount = 0,
    totalPauseDurationSeconds = 0,
    pauseCoefficientOfVariation = 0,
    articulationRate = 0,
    isSuspiciousPacing = false,
  } = metrics;

  // WPM rating: Normal: 90 - 210; Fast: > 210; Slow: < 90
  const isFast = wordsPerMinute > 210;
  const isSlow = wordsPerMinute < 90 && wordsPerMinute > 0;
  const wpmStatus = isFast ? 'Nhanh' : isSlow ? 'Chậm' : 'Bình thường';
  const wpmColor = isFast || isSlow ? 'var(--warning-text)' : 'var(--success-text)';
  const wpmBg = isFast || isSlow ? 'var(--warning-bg)' : 'var(--success-bg)';

  return (
    <div
      style={{
        marginTop: '0.5rem',
        padding: '0.65rem 0.85rem',
        backgroundColor: 'var(--slate-50)',
        borderRadius: 'var(--radius-md)',
        border: '1px solid var(--border-color)',
        fontSize: '0.8rem',
      }}
    >
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '0.4rem', flexWrap: 'wrap', gap: '0.5rem' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '0.35rem', fontWeight: 600, color: 'var(--slate-700)' }}>
          <Activity size={14} color="var(--primary-600)" />
          <span>Chỉ số nhịp nói (Speech Telemetry)</span>
        </div>

        {isSuspiciousPacing && (
          <span
            style={{
              display: 'inline-flex',
              alignItems: 'center',
              gap: '0.25rem',
              padding: '0.15rem 0.5rem',
              borderRadius: 'var(--radius-full)',
              fontSize: '0.7rem',
              fontWeight: 600,
              backgroundColor: 'var(--danger-bg)',
              color: 'var(--danger-text)',
              border: '1px solid var(--danger-border)',
            }}
          >
            <AlertTriangle size={11} />
            Nhịp nói bất thường
          </span>
        )}
      </div>

      <div
        style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(auto-fit, minmax(130px, 1fr))',
          gap: '0.5rem',
          margin: '0.4rem 0',
        }}
      >
        <div style={{ padding: '0.35rem 0.5rem', backgroundColor: '#ffffff', borderRadius: 'var(--radius-sm)', border: '1px solid var(--slate-200)' }}>
          <div style={{ fontSize: '0.7rem', color: 'var(--slate-500)', display: 'flex', alignItems: 'center', gap: '0.2rem' }}>
            <Mic size={11} />
            <span>Tốc độ nói</span>
          </div>
          <div style={{ display: 'flex', alignItems: 'baseline', gap: '0.3rem', marginTop: '2px' }}>
            <strong style={{ fontSize: '0.9rem', color: 'var(--slate-800)' }}>{Math.round(wordsPerMinute)}</strong>
            <span style={{ fontSize: '0.7rem', color: 'var(--slate-500)' }}>WPM</span>
            <span
              style={{
                fontSize: '0.65rem',
                padding: '0.05rem 0.3rem',
                borderRadius: '4px',
                backgroundColor: wpmBg,
                color: wpmColor,
                fontWeight: 600,
                marginLeft: 'auto',
              }}
            >
              {wpmStatus}
            </span>
          </div>
        </div>

        <div style={{ padding: '0.35rem 0.5rem', backgroundColor: '#ffffff', borderRadius: 'var(--radius-sm)', border: '1px solid var(--slate-200)' }}>
          <div style={{ fontSize: '0.7rem', color: 'var(--slate-500)', display: 'flex', alignItems: 'center', gap: '0.2rem' }}>
            <Clock size={11} />
            <span>Khoảng lặng</span>
          </div>
          <div style={{ display: 'flex', alignItems: 'baseline', gap: '0.3rem', marginTop: '2px' }}>
            <strong style={{ fontSize: '0.9rem', color: 'var(--slate-800)' }}>{pauseCount}</strong>
            <span style={{ fontSize: '0.7rem', color: 'var(--slate-500)' }}>lần ({totalPauseDurationSeconds.toFixed(1)}s)</span>
          </div>
        </div>

        <div style={{ padding: '0.35rem 0.5rem', backgroundColor: '#ffffff', borderRadius: 'var(--radius-sm)', border: '1px solid var(--slate-200)' }}>
          <div style={{ fontSize: '0.7rem', color: 'var(--slate-500)' }}>Biến thiên ngừng nghỉ</div>
          <div style={{ display: 'flex', alignItems: 'baseline', gap: '0.3rem', marginTop: '2px' }}>
            <strong style={{ fontSize: '0.9rem', color: 'var(--slate-800)' }}>
              {(pauseCoefficientOfVariation * 100).toFixed(0)}%
            </strong>
            <span style={{ fontSize: '0.7rem', color: 'var(--slate-500)' }}>(CV)</span>
          </div>
        </div>

        {articulationRate > 0 && (
          <div style={{ padding: '0.35rem 0.5rem', backgroundColor: '#ffffff', borderRadius: 'var(--radius-sm)', border: '1px solid var(--slate-200)' }}>
            <div style={{ fontSize: '0.7rem', color: 'var(--slate-500)' }}>Tốc độ phát âm</div>
            <div style={{ display: 'flex', alignItems: 'baseline', gap: '0.3rem', marginTop: '2px' }}>
              <strong style={{ fontSize: '0.9rem', color: 'var(--slate-800)' }}>{Math.round(articulationRate)}</strong>
              <span style={{ fontSize: '0.7rem', color: 'var(--slate-500)' }}>WPM phát âm</span>
            </div>
          </div>
        )}
      </div>

      <div style={{ display: 'flex', alignItems: 'center', gap: '0.3rem', fontSize: '0.7rem', color: 'var(--slate-500)', marginTop: '0.35rem' }}>
        <Info size={12} color="var(--primary-600)" />
        <span>Chỉ số nhịp nói phục vụ HR tham khảo hành vi trả lời, hoàn toàn không tự động trừ điểm.</span>
      </div>
    </div>
  );
}
