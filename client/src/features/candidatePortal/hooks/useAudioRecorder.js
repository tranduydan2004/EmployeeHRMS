import { useState, useRef, useEffect, useCallback } from 'react';

/**
 * Custom Hook: MediaRecorder & Audio Visualizer
 * Hỗ trợ thu âm micro, đo mức âm lượng thời gian thực (AudioContext),
 * phát hiện mất kết nối mic giữa chừng (track.onended), và đóng gói audio Blob chuẩn.
 */
export function useAudioRecorder({ onMicDisconnected } = {}) {
  const [isRecording, setIsRecording] = useState(false);
  const [hasPermission, setHasPermission] = useState(null); // null = chưa hỏi, true = đã cấp, false = bị từ chối
  const [permissionError, setPermissionError] = useState(null);
  const [deviceLabel, setDeviceLabel] = useState('');
  const [volumeLevel, setVolumeLevel] = useState(0); // 0 đến 100

  const mediaStreamRef = useRef(null);
  const mediaRecorderRef = useRef(null);
  const audioChunksRef = useRef([]);
  const startTimeRef = useRef(0);
  const audioContextRef = useRef(null);
  const analyserRef = useRef(null);
  const animationFrameRef = useRef(null);

  // Chọn định dạng MIME tối ưu nhất trình duyệt hỗ trợ
  const getSupportedMimeType = () => {
    const types = [
      'audio/webm;codecs=opus',
      'audio/webm',
      'audio/ogg;codecs=opus',
      'audio/mp4',
    ];
    for (const type of types) {
      if (MediaRecorder.isTypeSupported(type)) return type;
    }
    return '';
  };

  // Khởi tạo và đo mức âm lượng thời gian thực
  const setupVisualizer = (stream) => {
    try {
      const AudioCtx = window.AudioContext || window.webkitAudioContext;
      if (!AudioCtx) return;

      const audioCtx = new AudioCtx();
      audioContextRef.current = audioCtx;

      const analyser = audioCtx.createAnalyser();
      analyser.fftSize = 256;
      analyserRef.current = analyser;

      const source = audioCtx.createMediaStreamSource(stream);
      source.connect(analyser);

      const bufferLength = analyser.frequencyBinCount;
      const dataArray = new Uint8Array(bufferLength);

      const updateVolume = () => {
        if (!analyserRef.current) return;
        analyserRef.current.getByteFrequencyData(dataArray);

        // Tính trung bình cường độ âm thanh
        let sum = 0;
        for (let i = 0; i < bufferLength; i++) {
          sum += dataArray[i];
        }
        const average = sum / bufferLength;
        const normalized = Math.min(100, Math.round((average / 128) * 100));

        setVolumeLevel(normalized);
        animationFrameRef.current = requestAnimationFrame(updateVolume);
      };

      updateVolume();
    } catch (err) {
      console.warn('[useAudioRecorder] Không thể khởi tạo AudioContext visualizer:', err);
    }
  };

  // Dọn dẹp visualizer
  const stopVisualizer = () => {
    if (animationFrameRef.current) {
      cancelAnimationFrame(animationFrameRef.current);
      animationFrameRef.current = null;
    }
    if (audioContextRef.current && audioContextRef.current.state !== 'closed') {
      audioContextRef.current.close().catch(() => {});
      audioContextRef.current = null;
    }
    setVolumeLevel(0);
  };

  // Yêu cầu quyền truy cập micro (Mic Check)
  const requestPermission = useCallback(async () => {
    setPermissionError(null);
    try {
      // Dừng stream cũ nếu có
      if (mediaStreamRef.current) {
        mediaStreamRef.current.getTracks().forEach((t) => t.stop());
      }

      const stream = await navigator.mediaDevices.getUserMedia({
        audio: {
          echoCancellation: true,
          noiseSuppression: true,
          autoGainControl: true,
        },
      });

      mediaStreamRef.current = stream;
      setHasPermission(true);

      const audioTrack = stream.getAudioTracks()[0];
      if (audioTrack) {
        setDeviceLabel(audioTrack.label || 'Microphone mặc định');

        // Bắt sự kiện mic bị rút/mất kết nối đột ngột
        audioTrack.onended = () => {
          console.warn('[useAudioRecorder] Thiết bị micro đã bị ngắt kết nối!');
          if (onMicDisconnected) onMicDisconnected();
        };
      }

      setupVisualizer(stream);
      return true;
    } catch (err) {
      setHasPermission(false);
      let errorText = 'Không thể truy cập micro.';
      if (err.name === 'NotAllowedError' || err.name === 'PermissionDeniedError') {
        errorText = 'Quyền truy cập micro đã bị từ chối. Vui lòng cho phép quyền trên thanh địa chỉ trình duyệt.';
      } else if (err.name === 'NotFoundError' || err.name === 'DevicesNotFoundError') {
        errorText = 'Không tìm thấy thiết bị micro nào được kết nối với máy tính của bạn.';
      } else if (err.name === 'NotReadableError' || err.name === 'TrackStartError') {
        errorText = 'Micro đang bị ứng dụng khác sử dụng (ví dụ: Zoom, Teams, Zalo). Vui lòng tắt các ứng dụng đó.';
      }
      setPermissionError(errorText);
      return false;
    }
  }, [onMicDisconnected]);

  // Bắt đầu ghi âm
  const startRecording = useCallback(async () => {
    audioChunksRef.current = [];
    startTimeRef.current = Date.now();

    let stream = mediaStreamRef.current;
    if (!stream || !stream.active) {
      const ok = await requestPermission();
      if (!ok) throw new Error(permissionError || 'Không có quyền micro.');
      stream = mediaStreamRef.current;
    }

    const mimeType = getSupportedMimeType();
    const options = mimeType ? { mimeType } : {};

    const recorder = new MediaRecorder(stream, options);
    mediaRecorderRef.current = recorder;

    recorder.ondataavailable = (event) => {
      if (event.data && event.data.size > 0) {
        audioChunksRef.current.push(event.data);
      }
    };

    recorder.start(500); // Lưu chunk mỗi 500ms
    setIsRecording(true);
  }, [permissionError, requestPermission]);

  // Dừng ghi âm và trả về Blob
  const stopRecording = useCallback(() => {
    return new Promise((resolve, reject) => {
      const recorder = mediaRecorderRef.current;
      if (!recorder || recorder.state === 'inactive') {
        setIsRecording(false);
        resolve(null);
        return;
      }

      recorder.onstop = () => {
        const durationSeconds = Math.max(1, (Date.now() - startTimeRef.current) / 1000);
        const mimeType = recorder.mimeType || 'audio/webm';
        const blob = new Blob(audioChunksRef.current, { type: mimeType });

        setIsRecording(false);
        audioChunksRef.current = [];
        resolve({ blob, durationSeconds });
      };

      recorder.onerror = (event) => {
        setIsRecording(false);
        reject(event.error || new Error('Lỗi xảy ra trong quá trình ghi âm.'));
      };

      recorder.stop();
    });
  }, []);

  // Cleanup khi component unmount
  useEffect(() => {
    return () => {
      stopVisualizer();
      if (mediaStreamRef.current) {
        mediaStreamRef.current.getTracks().forEach((track) => track.stop());
      }
    };
  }, []);

  return {
    isRecording,
    hasPermission,
    permissionError,
    deviceLabel,
    volumeLevel,
    requestPermission,
    startRecording,
    stopRecording,
    stopVisualizer,
  };
}
