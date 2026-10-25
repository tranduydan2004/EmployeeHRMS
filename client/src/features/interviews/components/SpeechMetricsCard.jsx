import { Activity, AlertTriangle, Info, Clock, Mic, ExternalLink, EyeOff, ShieldAlert, CheckCircle } from 'lucide-react';

export default function SpeechMetricsCard({
  metrics,
  tabSwitchCount = 0,
  outOfFocusDurationSeconds = 0,
  flags = [],
}) {
  if (!metrics && tabSwitchCount === 0 && outOfFocusDurationSeconds === 0) return null;

  // Support both backend camelCase properties (wpm, pauseVariationCv, isRhythmMonotonous) and legacy formats
  const wpm = metrics?.wpm ?? metrics?.wordsPerMinute ?? 0;
  const pauseCount = metrics?.pauseCount ?? 0;
  const avgPause = metrics?.averagePauseSeconds ?? 0;
  // Calculate total pause duration: pauseCount * averagePauseSeconds, or fallback to totalPauseDurationSeconds
  const totalPauseDuration = metrics?.totalPauseDurationSeconds ?? (pauseCount * avgPause);
  const pauseCv = metrics?.pauseVariationCv ?? metrics?.pauseCoefficientOfVariation ?? 0;
  const isSuspiciousPacing = metrics?.isRhythmMonotonous ?? metrics?.isSuspiciousPacing ?? false;
  const speechDurationSeconds = metrics?.speechDurationSeconds ?? 0;
  const wordCount = metrics?.wordCount ?? 0;

  // Articulation rate = words / active speech minutes (excluding pauses)
  const articulationRate =
    metrics?.articulationRate ??
    (speechDurationSeconds > 0 ? Math.round(wordCount / (speechDurationSeconds / 60)) : 0);

  // WPM rating: Normal: 90 - 210; Fast: > 210; Slow: < 90
  const isFast = wpm > 210;
  const isSlow = wpm < 90 && wpm > 0;
  const wpmStatus = isFast ? 'Nhanh' : isSlow ? 'Chậm' : 'Bình thường';
  const wpmColor = isFast || isSlow ? 'var(--warning-text)' : 'var(--success-text)';
  const wpmBg = isFast || isSlow ? 'var(--warning-bg)' : 'var(--success-bg)';

  const hasTabSwitches = tabSwitchCount > 0;
  const hasOutOfFocus = outOfFocusDurationSeconds > 0;

  return (
    <div
      style={{
        marginTop: '0.5rem',
        padding: '0.75rem 0.85rem',
        backgroundColor: 'var(--slate-50)',
        borderRadius: 'var(--radius-md)',
        border: '1px solid var(--border-color)',
        fontSize: '0.8rem',
      }}
    >
      {/* Telemetry Header */}
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '0.5rem', flexWrap: 'wrap', gap: '0.5rem' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '0.35rem', fontWeight: 600, color: 'var(--slate-700)' }}>
          <Activity size={14} color="var(--primary-600)" />
          <span>Chỉ số Nhịp nói & Giám sát trả lời (Speech Telemetry)</span>
        </div>

        <div style={{ display: 'flex', alignItems: 'center', gap: '0.4rem', flexWrap: 'wrap' }}>
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
              title="Khoảng lặng đều đặn bất thường theo tiêu chuẩn phân tích âm học"
            >
              <AlertTriangle size={11} />
              Nhịp nói đọc kịch bản (Monotonous)
            </span>
          )}

          {hasTabSwitches && (
            <span
              style={{
                display: 'inline-flex',
                alignItems: 'center',
                gap: '0.25rem',
                padding: '0.15rem 0.5rem',
                borderRadius: 'var(--radius-full)',
                fontSize: '0.7rem',
                fontWeight: 600,
                backgroundColor: 'var(--warning-bg)',
                color: 'var(--warning-text)',
                border: '1px solid var(--warning-border)',
              }}
            >
              <ExternalLink size={11} />
              {tabSwitchCount} lần đổi tab
            </span>
          )}
        </div>
      </div>

      {/* Grid: Speech rhythm metrics + Anti-cheat metrics */}
      <div
        style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(auto-fit, minmax(130px, 1fr))',
          gap: '0.5rem',
          margin: '0.4rem 0',
        }}
      >
        {/* Tốc độ nói tổng thể (WPM) */}
        <div style={{ padding: '0.4rem 0.55rem', backgroundColor: '#ffffff', borderRadius: 'var(--radius-sm)', border: '1px solid var(--slate-200)' }}>
          <div style={{ fontSize: '0.7rem', color: 'var(--slate-500)', display: 'flex', alignItems: 'center', gap: '0.2rem' }}>
            <Mic size={11} />
            <span>Tốc độ nói (WPM)</span>
          </div>
          <div style={{ display: 'flex', alignItems: 'baseline', gap: '0.3rem', marginTop: '2px' }}>
            <strong style={{ fontSize: '0.95rem', color: 'var(--slate-800)' }}>{Math.round(wpm)}</strong>
            <span style={{ fontSize: '0.7rem', color: 'var(--slate-500)' }}>từ/phút</span>
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

        {/* Khoảng lặng & Tổng thời gian ngừng nghỉ */}
        <div style={{ padding: '0.4rem 0.55rem', backgroundColor: '#ffffff', borderRadius: 'var(--radius-sm)', border: '1px solid var(--slate-200)' }}>
          <div style={{ fontSize: '0.7rem', color: 'var(--slate-500)', display: 'flex', alignItems: 'center', gap: '0.2rem' }}>
            <Clock size={11} />
            <span>Khoảng lặng (Ngừng nghỉ)</span>
          </div>
          <div style={{ display: 'flex', alignItems: 'baseline', gap: '0.3rem', marginTop: '2px' }}>
            <strong style={{ fontSize: '0.95rem', color: 'var(--slate-800)' }}>{pauseCount}</strong>
            <span style={{ fontSize: '0.7rem', color: 'var(--slate-500)' }}>
              lần ({totalPauseDuration > 0 ? totalPauseDuration.toFixed(1) : (avgPause * pauseCount).toFixed(1)}s)
            </span>
          </div>
        </div>

        {/* Hệ số biến thiên khoảng lặng (Pause CV) */}
        <div style={{ padding: '0.4rem 0.55rem', backgroundColor: '#ffffff', borderRadius: 'var(--radius-sm)', border: '1px solid var(--slate-200)' }}>
          <div style={{ fontSize: '0.7rem', color: 'var(--slate-500)' }}>Biến thiên ngừng nghỉ</div>
          <div style={{ display: 'flex', alignItems: 'baseline', gap: '0.3rem', marginTop: '2px' }}>
            <strong style={{ fontSize: '0.95rem', color: 'var(--slate-800)' }}>
              {(pauseCv * 100).toFixed(0)}%
            </strong>
            <span style={{ fontSize: '0.7rem', color: 'var(--slate-500)' }}>(CV)</span>
          </div>
        </div>

        {/* Tốc độ phát âm thực tế (Articulation Rate) */}
        {articulationRate > 0 && (
          <div style={{ padding: '0.4rem 0.55rem', backgroundColor: '#ffffff', borderRadius: 'var(--radius-sm)', border: '1px solid var(--slate-200)' }}>
            <div style={{ fontSize: '0.7rem', color: 'var(--slate-500)' }}>Tốc độ phát âm</div>
            <div style={{ display: 'flex', alignItems: 'baseline', gap: '0.3rem', marginTop: '2px' }}>
              <strong style={{ fontSize: '0.95rem', color: 'var(--slate-800)' }}>{Math.round(articulationRate)}</strong>
              <span style={{ fontSize: '0.7rem', color: 'var(--slate-500)' }}>từ/phút nói</span>
            </div>
          </div>
        )}

        {/* Chuyển tab trình duyệt */}
        <div
          style={{
            padding: '0.4rem 0.55rem',
            backgroundColor: hasTabSwitches ? 'var(--warning-bg)' : '#ffffff',
            borderRadius: 'var(--radius-sm)',
            border: `1px solid ${hasTabSwitches ? 'var(--warning-border)' : 'var(--slate-200)'}`,
          }}
        >
          <div style={{ fontSize: '0.7rem', color: hasTabSwitches ? 'var(--warning-text)' : 'var(--slate-500)', display: 'flex', alignItems: 'center', gap: '0.2rem' }}>
            <ExternalLink size={11} />
            <span>Chuyển tab</span>
          </div>
          <div style={{ display: 'flex', alignItems: 'baseline', gap: '0.3rem', marginTop: '2px' }}>
            <strong style={{ fontSize: '0.95rem', color: hasTabSwitches ? 'var(--warning-text)' : 'var(--slate-800)' }}>
              {tabSwitchCount}
            </strong>
            <span style={{ fontSize: '0.7rem', color: hasTabSwitches ? 'var(--warning-text)' : 'var(--slate-500)' }}>lần</span>
          </div>
        </div>

        {/* Rời khỏi cửa sổ / Mất focus */}
        <div
          style={{
            padding: '0.4rem 0.55rem',
            backgroundColor: hasOutOfFocus ? 'var(--warning-bg)' : '#ffffff',
            borderRadius: 'var(--radius-sm)',
            border: `1px solid ${hasOutOfFocus ? 'var(--warning-border)' : 'var(--slate-200)'}`,
          }}
        >
          <div style={{ fontSize: '0.7rem', color: hasOutOfFocus ? 'var(--warning-text)' : 'var(--slate-500)', display: 'flex', alignItems: 'center', gap: '0.2rem' }}>
            <EyeOff size={11} />
            <span>Rời cửa sổ</span>
          </div>
          <div style={{ display: 'flex', alignItems: 'baseline', gap: '0.3rem', marginTop: '2px' }}>
            <strong style={{ fontSize: '0.95rem', color: hasOutOfFocus ? 'var(--warning-text)' : 'var(--slate-800)' }}>
              {Number(outOfFocusDurationSeconds || 0).toFixed(1)}
            </strong>
            <span style={{ fontSize: '0.7rem', color: hasOutOfFocus ? 'var(--warning-text)' : 'var(--slate-500)' }}>giây</span>
          </div>
        </div>
      </div>

      {/* Flags nếu có */}
      {flags && flags.length > 0 && (
        <div style={{ display: 'flex', alignItems: 'center', gap: '0.35rem', marginTop: '0.35rem', flexWrap: 'wrap' }}>
          <span style={{ fontSize: '0.7rem', color: 'var(--slate-600)', fontWeight: 600 }}>Cờ thiết bị:</span>
          {flags.map((flag, fIdx) => (
            <span
              key={fIdx}
              style={{
                fontSize: '0.65rem',
                padding: '0.1rem 0.4rem',
                borderRadius: '4px',
                backgroundColor: 'var(--warning-bg)',
                color: 'var(--warning-text)',
                border: '1px solid var(--warning-border)',
                display: 'inline-flex',
                alignItems: 'center',
                gap: '0.2rem',
              }}
            >
              <ShieldAlert size={10} />
              {flag === 'MicDisconnected' ? 'Mic bị ngắt kết nối giữa chừng' : flag}
            </span>
          ))}
        </div>
      )}

      {/* Fairness disclaimer */}
      <div style={{ display: 'flex', alignItems: 'center', gap: '0.3rem', fontSize: '0.7rem', color: 'var(--slate-500)', marginTop: '0.4rem' }}>
        <Info size={12} color="var(--primary-600)" />
        <span>Chỉ số nhịp nói và telemetry chuyển tab phục vụ HR tham khảo hành vi phỏng vấn, hoàn toàn không tự động trừ điểm.</span>
      </div>
    </div>
  );
}
