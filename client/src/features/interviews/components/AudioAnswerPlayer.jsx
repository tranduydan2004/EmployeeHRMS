import { useState, useEffect, useRef } from 'react';
import { Play, Pause, Volume2, VolumeX, AlertCircle, Loader2 } from 'lucide-react';
import { interviewApi } from '@/api/interviewApi';

export default function AudioAnswerPlayer({ storageKey, durationSeconds = 0 }) {
  const [blobUrl, setBlobUrl] = useState(null);
  const [isLoading, setIsLoading] = useState(false);
  const [loadError, setLoadError] = useState(null);
  const [isPlaying, setIsPlaying] = useState(false);
  const [currentTime, setCurrentTime] = useState(0);
  const [totalDuration, setTotalDuration] = useState(durationSeconds);
  const [isMuted, setIsMuted] = useState(false);

  const audioRef = useRef(null);

  useEffect(() => {
    let active = true;
    let url = null;

    if (!storageKey) {
      setLoadError('File ghi âm đã được xóa theo chính sách bảo mật dữ liệu cá nhân.');
      return;
    }

    const loadAudio = async () => {
      try {
        setIsLoading(true);
        setLoadError(null);
        const blob = await interviewApi.getAudioBlob(storageKey);
        if (!active) return;
        url = URL.createObjectURL(blob);
        setBlobUrl(url);
      } catch (err) {
        if (!active) return;
        const status = err.response?.status;
        if (status === 410 || status === 404) {
          setLoadError('File ghi âm đã được xóa theo chính sách quyền riêng tư.');
        } else {
          setLoadError('Không thể tải file ghi âm.');
        }
      } finally {
        if (active) setIsLoading(false);
      }
    };

    loadAudio();

    return () => {
      active = false;
      if (url) URL.revokeObjectURL(url);
    };
  }, [storageKey]);

  const togglePlay = () => {
    if (!audioRef.current) return;
    if (isPlaying) {
      audioRef.current.pause();
    } else {
      audioRef.current.play();
    }
  };

  const handleTimeUpdate = () => {
    if (audioRef.current) {
      setCurrentTime(audioRef.current.currentTime);
    }
  };

  const handleLoadedMetadata = () => {
    if (audioRef.current && audioRef.current.duration && !isNaN(audioRef.current.duration)) {
      setTotalDuration(audioRef.current.duration);
    }
  };

  const handleSeek = (e) => {
    const time = parseFloat(e.target.value);
    setCurrentTime(time);
    if (audioRef.current) {
      audioRef.current.currentTime = time;
    }
  };

  const toggleMute = () => {
    if (!audioRef.current) return;
    audioRef.current.muted = !isMuted;
    setIsMuted(!isMuted);
  };

  const formatTime = (secs) => {
    if (isNaN(secs) || secs < 0) return '0:00';
    const m = Math.floor(secs / 60);
    const s = Math.floor(secs % 60);
    return `${m}:${s < 10 ? '0' : ''}${s}`;
  };

  if (!storageKey) return null;

  return (
    <div
      style={{
        display: 'flex',
        alignItems: 'center',
        gap: '0.65rem',
        padding: '0.5rem 0.75rem',
        backgroundColor: 'var(--slate-100)',
        borderRadius: 'var(--radius-md)',
        border: '1px solid var(--border-color)',
        maxWidth: '480px',
      }}
    >
      {blobUrl && (
        <audio
          ref={audioRef}
          src={blobUrl}
          onTimeUpdate={handleTimeUpdate}
          onLoadedMetadata={handleLoadedMetadata}
          onPlay={() => setIsPlaying(true)}
          onPause={() => setIsPlaying(false)}
          onEnded={() => {
            setIsPlaying(false);
            setCurrentTime(0);
          }}
        />
      )}

      {isLoading ? (
        <div style={{ display: 'flex', alignItems: 'center', gap: '0.35rem', fontSize: '0.75rem', color: 'var(--text-muted)' }}>
          <Loader2 size={16} className="animate-spin" />
          <span>Đang tải audio...</span>
        </div>
      ) : loadError ? (
        <div style={{ display: 'flex', alignItems: 'center', gap: '0.35rem', fontSize: '0.75rem', color: 'var(--danger-main)' }}>
          <AlertCircle size={16} />
          <span>{loadError}</span>
        </div>
      ) : (
        <>
          <button
            type="button"
            onClick={togglePlay}
            disabled={!blobUrl}
            style={{
              width: '32px',
              height: '32px',
              borderRadius: '50%',
              backgroundColor: 'var(--primary-600)',
              color: '#ffffff',
              border: 'none',
              cursor: blobUrl ? 'pointer' : 'not-allowed',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              flexShrink: 0,
              transition: 'all 0.15s ease',
            }}
            title={isPlaying ? 'Tạm dừng' : 'Nghe câu trả lời'}
          >
            {isPlaying ? <Pause size={15} /> : <Play size={15} style={{ marginLeft: '2px' }} />}
          </button>

          <div style={{ display: 'flex', flexDirection: 'column', flex: 1, minWidth: '120px' }}>
            <input
              type="range"
              min="0"
              max={totalDuration || 100}
              step="0.1"
              value={currentTime}
              onChange={handleSeek}
              style={{
                width: '100%',
                height: '4px',
                borderRadius: '2px',
                accentColor: 'var(--primary-600)',
                cursor: 'pointer',
              }}
            />
            <div
              style={{
                display: 'flex',
                justifyContent: 'space-between',
                fontSize: '0.7rem',
                color: 'var(--slate-500)',
                marginTop: '2px',
                fontVariantNumeric: 'tabular-nums',
              }}
            >
              <span>{formatTime(currentTime)}</span>
              <span>{formatTime(totalDuration)}</span>
            </div>
          </div>

          <button
            type="button"
            onClick={toggleMute}
            style={{
              background: 'none',
              border: 'none',
              color: 'var(--slate-500)',
              cursor: 'pointer',
              padding: '2px',
              display: 'flex',
              alignItems: 'center',
            }}
            title={isMuted ? 'Bật tiếng' : 'Tắt tiếng'}
          >
            {isMuted ? <VolumeX size={16} /> : <Volume2 size={16} />}
          </button>
        </>
      )}
    </div>
  );
}
