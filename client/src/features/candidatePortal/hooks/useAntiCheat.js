import { useEffect, useRef, useState, useCallback } from 'react';

/**
 * Custom Hook: Anti-Cheat Telemetry & Page Visibility
 * Lắng nghe visibilitychange, window blur/focus, và cảnh báo trước khi rời trang (beforeunload).
 * 
 * @param {object} options
 * @param {boolean} options.isActive - Bật theo dõi khi ứng viên đang trong bài phỏng vấn
 * @param {(durationSeconds: number) => void} options.onTabSwitched - Callback khi ứng viên rời khỏi tab/cửa sổ
 */
export function useAntiCheat({ isActive = false, onTabSwitched } = {}) {
  const [isFullscreen, setIsFullscreen] = useState(false);
  const hiddenStartTimeRef = useRef(null);
  const onTabSwitchedRef = useRef(onTabSwitched);

  useEffect(() => {
    onTabSwitchedRef.current = onTabSwitched;
  }, [onTabSwitched]);

  // 1. Theo dõi visibilitychange và window blur
  useEffect(() => {
    if (!isActive) return;

    const handleVisibilityChange = () => {
      if (document.hidden) {
        hiddenStartTimeRef.current = performance.now();
      } else if (hiddenStartTimeRef.current) {
        const durationSec = Math.max(0.1, (performance.now() - hiddenStartTimeRef.current) / 1000);
        hiddenStartTimeRef.current = null;
        if (onTabSwitchedRef.current) {
          onTabSwitchedRef.current(durationSec);
        }
      }
    };

    const handleWindowBlur = () => {
      if (!hiddenStartTimeRef.current) {
        hiddenStartTimeRef.current = performance.now();
      }
    };

    const handleWindowFocus = () => {
      if (hiddenStartTimeRef.current) {
        const durationSec = Math.max(0.1, (performance.now() - hiddenStartTimeRef.current) / 1000);
        hiddenStartTimeRef.current = null;
        if (onTabSwitchedRef.current) {
          onTabSwitchedRef.current(durationSec);
        }
      }
    };

    document.addEventListener('visibilitychange', handleVisibilityChange);
    window.addEventListener('blur', handleWindowBlur);
    window.addEventListener('focus', handleWindowFocus);

    return () => {
      document.removeEventListener('visibilitychange', handleVisibilityChange);
      window.removeEventListener('blur', handleWindowBlur);
      window.removeEventListener('focus', handleWindowFocus);
    };
  }, [isActive]);

  // 2. Cảnh báo beforeunload khi ứng viên bấm đóng tab/F5 giữa chừng
  useEffect(() => {
    if (!isActive) return;

    const handleBeforeUnload = (event) => {
      event.preventDefault();
      // Chuẩn HTML specification yêu cầu gán returnValue
      event.returnValue = 'Bài phỏng vấn của bạn đang diễn ra. Nếu rời trang bây giờ, tiến trình có thể bị hủy.';
      return event.returnValue;
    };

    window.addEventListener('beforeunload', handleBeforeUnload);
    return () => {
      window.removeEventListener('beforeunload', handleBeforeUnload);
    };
  }, [isActive]);

  // 3. Toàn màn hình (Full-screen API)
  const toggleFullscreen = useCallback(async () => {
    try {
      if (!document.fullscreenElement) {
        await document.documentElement.requestFullscreen();
        setIsFullscreen(true);
      } else {
        await document.exitFullscreen();
        setIsFullscreen(false);
      }
    } catch (err) {
      console.warn('[useAntiCheat] Không thể thay đổi fullscreen:', err);
    }
  }, []);

  useEffect(() => {
    const handleFullscreenChange = () => {
      setIsFullscreen(!!document.fullscreenElement);
    };

    document.addEventListener('fullscreenchange', handleFullscreenChange);
    return () => {
      document.removeEventListener('fullscreenchange', handleFullscreenChange);
    };
  }, []);

  return {
    isFullscreen,
    toggleFullscreen,
  };
}
