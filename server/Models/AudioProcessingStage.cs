namespace EmployeeHRMS.Api.Models
{
    /// <summary>
    /// Giai đoạn xảy ra lỗi trong pipeline xử lý câu trả lời audio.
    /// Dùng để phân biệt lỗi bóc băng (Transcription) hay lỗi chấm điểm (Scoring).
    /// </summary>
    public enum AudioProcessingStage
    {
        Transcription = 1,
        Scoring = 2
    }
}
