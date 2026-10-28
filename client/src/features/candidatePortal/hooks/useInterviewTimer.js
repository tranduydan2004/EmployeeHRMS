import { useState, useEffect, useRef, useCallback } from 'react';

/**
 * Custom Hook: Drift-Free Countdown Timer
 * Sử dụng performance.now() và mốc thời gian đích (targetEndTime)
 * để tính toán chính xác số giây còn lại ngay cả khi tab bị trình duyệt throttle lúc ẩn.
 * 
 * @param {object} options
 * @param {number} options.totalSeconds - Thời gian đếm ngược (giây)
 * @param {boolean} options.autoStart - Tự động bắt đầu đếm
 * @param {() => void} [options.onFinish] - Callback khi đếm về 0
 */
export function useInterviewTimer({ totalSeconds = 0, autoStart = false, onFinish } = {}) {
  const [secondsLeft, setSecondsLeft] = useState(totalSeconds);
  const [isRunning, setIsRunning] = useState(autoStart);

  const targetEndTimeRef = useRef(null);
  const animationFrameRef = useRef(null);
  const timeoutRef = useRef(null);
  const onFinishRef = useRef(onFinish);

  useEffect(() => {
    onFinishRef.current = onFinish;
  }, [onFinish]);

  const clearTimers = useCallback(() => {
    if (animationFrameRef.current) {
      cancelAnimationFrame(animationFrameRef.current);
      animationFrameRef.current = null;
    }
    if (timeoutRef.current) {
      clearTimeout(timeoutRef.current);
      timeoutRef.current = null;
    }
  }, []);

  const start = useCallback((seconds) => {
    clearTimers();
    const sec = seconds !== undefined ? seconds : totalSeconds;
    if (sec <= 0) {
      setSecondsLeft(0);
      setIsRunning(false);
      if (onFinishRef.current) onFinishRef.current();
      return;
    }

    targetEndTimeRef.current = performance.now() + sec * 1000;
    setSecondsLeft(sec);
    setIsRunning(true);

    // Fallback timer: đảm bảo onFinish vẫn kích hoạt đúng giờ ngay cả khi tab bị throttle lúc ẩn
    timeoutRef.current = setTimeout(() => {
      setSecondsLeft(0);
      setIsRunning(false);
      if (onFinishRef.current) onFinishRef.current();
    }, sec * 1000);
  }, [totalSeconds, clearTimers]);

  const pause = useCallback(() => {
    setIsRunning(false);
    clearTimers();
  }, [clearTimers]);

  const reset = useCallback((seconds) => {
    const sec = seconds !== undefined ? seconds : totalSeconds;
    targetEndTimeRef.current = null;
    setIsRunning(false);
    setSecondsLeft(sec);
    clearTimers();
  }, [totalSeconds, clearTimers]);

  useEffect(() => {
    if (!isRunning || !targetEndTimeRef.current) return;

    const tick = () => {
      const now = performance.now();
      const remainingMs = targetEndTimeRef.current - now;

      if (remainingMs <= 0) {
        clearTimers();
        setSecondsLeft(0);
        setIsRunning(false);
        if (onFinishRef.current) {
          onFinishRef.current();
        }
        return;
      }

      const nextSec = Math.ceil(remainingMs / 1000);
      setSecondsLeft((prev) => (prev !== nextSec ? nextSec : prev));

      animationFrameRef.current = requestAnimationFrame(tick);
    };

    animationFrameRef.current = requestAnimationFrame(tick);

    return () => {
      if (animationFrameRef.current) {
        cancelAnimationFrame(animationFrameRef.current);
      }
    };
  }, [isRunning, clearTimers]);

  // Định dạng hiển thị phút:giây (ví dụ: 01:30 hoặc 00:25)
  const formatTime = (secs) => {
    const m = Math.floor(secs / 60);
    const s = secs % 60;
    return `${m.toString().padStart(2, '0')}:${s.toString().padStart(2, '0')}`;
  };

  const progressPercent = totalSeconds > 0
    ? Math.max(0, Math.min(100, Math.round(((totalSeconds - secondsLeft) / totalSeconds) * 100)))
    : 100;

  return {
    secondsLeft,
    isRunning,
    start,
    pause,
    reset,
    formattedTime: formatTime(secondsLeft),
    progressPercent,
  };
}
