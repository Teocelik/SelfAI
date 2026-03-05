using System;

namespace SelfAI.Models
{
    /// <summary>
    /// Kullanım:
    ///   return ServiceResult<string>.Success(data, "Başarılı");
    ///   return ServiceResult<string>.Failure("Hata mesajı");
    /// </summary>
    
    // ServiceResult<T> generic bir class : veri tipinden bağımsız çalışabilen sınıftır.
    //Yani sınıfı yazarken içine hangi tipte veri geleceğini önceden sabitlemezsin, dışarıdan belirlersin.
    public class ServiceResult<T>
    {
        /// <summary>İşlem başarılı mı?</summary>
        public bool IsSuccess { get; private set; }

        /// <summary>Başarılıysa dönen veri</summary>
        public T? Data { get; private set; }

        /// <summary>Kullanıcıya gösterilecek mesaj</summary>
        public string Message { get; private set; } = string.Empty;

        public int StatusCode { get; private set; }

        // ═══ FACTORY METHODS (Nesne oluşturma yardımcıları) ═══

        // Başarılı sonuç oluşturur
        public static ServiceResult<T> Success(T data, string message = "İşlem başarılı.")
        {
            return new ServiceResult<T>
            {
                IsSuccess = true,
                Data = data,
                Message = message,
                StatusCode = 200
            };
        }
        
        // Başarısız sonuç oluşturur
        public static ServiceResult<T> Failure(string message, int statusCode = 500)
        {
            return new ServiceResult<T>
            {
                IsSuccess = false,
                Data = default,
                Message = message,
                StatusCode = statusCode
            };
        }
    }
}
