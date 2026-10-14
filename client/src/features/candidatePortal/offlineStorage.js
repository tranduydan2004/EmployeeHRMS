/**
 * Offline Audio Storage using native browser IndexedDB
 * Tự động sao lưu âm thanh ngay sau khi ghi âm hoàn tất để bảo đảm an toàn dữ liệu
 * nếu ứng viên gặp sự cố mạng hoặc lỗi upload giữa chừng.
 */

const DB_NAME = 'EmployeeHRMS_CandidateInterview';
const DB_VERSION = 1;
const STORE_NAME = 'offline_audio_answers';

function openDatabase() {
  return new Promise((resolve, reject) => {
    if (!window.indexedDB) {
      reject(new Error('Trình duyệt không hỗ trợ IndexedDB.'));
      return;
    }

    const request = window.indexedDB.open(DB_NAME, DB_VERSION);

    request.onupgradeneeded = (event) => {
      const db = event.target.result;
      if (!db.objectStoreNames.contains(STORE_NAME)) {
        db.createObjectStore(STORE_NAME, { keyPath: 'id' });
      }
    };

    request.onsuccess = () => resolve(request.result);
    request.onerror = () => reject(request.error);
  });
}

/**
 * Lưu trữ Blob câu trả lời vào IndexedDB
 * @param {string} inviteToken 
 * @param {number} questionId 
 * @param {Blob} audioBlob 
 * @param {object} metadata { durationSeconds, tabSwitchCount, outOfFocusDurationSeconds, flags }
 */
export async function saveOfflineAnswer(inviteToken, questionId, audioBlob, metadata = {}) {
  try {
    const db = await openDatabase();
    return new Promise((resolve, reject) => {
      const tx = db.transaction(STORE_NAME, 'readwrite');
      const store = tx.objectStore(STORE_NAME);

      const record = {
        id: `${inviteToken}_${questionId}`,
        inviteToken,
        questionId,
        audioBlob,
        metadata,
        savedAt: Date.now(),
      };

      const putRequest = store.put(record);
      putRequest.onsuccess = () => resolve(true);
      putRequest.onerror = () => reject(putRequest.error);
    });
  } catch (err) {
    console.warn('[OfflineStorage] Không thể sao lưu vào IndexedDB:', err);
    return false;
  }
}

/**
 * Lấy Blob câu trả lời đã lưu trữ
 * @param {string} inviteToken 
 * @param {number} questionId 
 * @returns {Promise<{ id: string, audioBlob: Blob, metadata: object } | null>}
 */
export async function getOfflineAnswer(inviteToken, questionId) {
  try {
    const db = await openDatabase();
    return new Promise((resolve, reject) => {
      const tx = db.transaction(STORE_NAME, 'readonly');
      const store = tx.objectStore(STORE_NAME);
      const getRequest = store.get(`${inviteToken}_${questionId}`);

      getRequest.onsuccess = () => resolve(getRequest.result || null);
      getRequest.onerror = () => reject(getRequest.error);
    });
  } catch (err) {
    console.warn('[OfflineStorage] Lỗi khi đọc từ IndexedDB:', err);
    return null;
  }
}

/**
 * Xóa câu trả lời khỏi IndexedDB sau khi upload thành công lên server
 * @param {string} inviteToken 
 * @param {number} questionId 
 */
export async function deleteOfflineAnswer(inviteToken, questionId) {
  try {
    const db = await openDatabase();
    return new Promise((resolve, reject) => {
      const tx = db.transaction(STORE_NAME, 'readwrite');
      const store = tx.objectStore(STORE_NAME);
      const delRequest = store.delete(`${inviteToken}_${questionId}`);

      delRequest.onsuccess = () => resolve(true);
      delRequest.onerror = () => reject(delRequest.error);
    });
  } catch (err) {
    console.warn('[OfflineStorage] Lỗi khi xóa IndexedDB:', err);
    return false;
  }
}
