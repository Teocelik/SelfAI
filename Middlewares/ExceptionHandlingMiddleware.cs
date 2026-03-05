namespace SelfAI.Middlewares
{
    /// <summary>
    /// Uygulamadaki TÜM yakalanmamış hataları merkezi olarak yakalar.
    /// - Hatayı loglar (ILogger ile)
    /// - AJAX isteklerinde JSON hata döner
    /// - Normal isteklerde hata sayfasına yönlendirir
    /// 
    /// 💡 Bu middleware sayesinde her Controller/Action'da try-catch yazmak zorunda kalmazsın.
    ///    Yakalanmamış hatalar burada yakalanır.
    /// </summary>
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        private readonly IWebHostEnvironment _env;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IWebHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // Bir sonraki middleware'e / controller'a geç
                await _next(context);
            }
            catch (Exception ex)
            {
                // 🔴 Yakalanmamış hata! Logla ve kullanıcıya uygun yanıt dön.
                _logger.LogError(ex,
                    "İşlenmeyen hata yakalandı! | Path: {Path} | Method: {Method} | QueryString: {Query} | Hata: {ErrorMessage}",
                    context.Request.Path,
                    context.Request.Method,
                    context.Request.QueryString,
                    ex.Message);

                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            // AJAX isteği mi kontrol et
            // (fetch ile yapılan istekler genelde Accept: application/json gönderir)
            bool isAjaxRequest = IsAjaxRequest(context);

            if (isAjaxRequest)
            {
                // 📡 AJAX İsteği → JSON yanıt dön (frontend toast gösterecek)
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;

                var errorResponse = new
                {
                    success = false,
                    message = "Beklenmeyen bir hata oluştu. Lütfen tekrar deneyin.",
                    // Development ortamında detaylı hata mesajı göster
                    detail = _env.IsDevelopment() ? exception.Message : null,
                    stackTrace = _env.IsDevelopment() ? exception.StackTrace : null
                };

                await context.Response.WriteAsJsonAsync(errorResponse);
            }
            else
            {
                // 🌐 Normal İstek → Hata sayfasına yönlendir
                context.Response.Redirect("/Home/Error");
            }
        }

        /// <summary>
        /// İsteğin AJAX (fetch/XMLHttpRequest) olup olmadığını kontrol eder
        /// </summary>
        private static bool IsAjaxRequest(HttpContext context)
        {
            var request = context.Request;

            // 1. XMLHttpRequest kontrolü (jQuery AJAX vb.)
            if (request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return true;

            // 2. Accept header'da JSON varsa (fetch API genelde bunu gönderir)
            if (request.Headers.Accept.ToString().Contains("application/json"))
                return true;

            // 3. Content-Type JSON ise
            if (request.ContentType?.Contains("application/json") == true)
                return true;

            return false;
        }
    }

    // Extension method - Program.cs'de temiz kullanım için
    public static class ExceptionHandlingMiddlewareExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder app)
        {
            return app.UseMiddleware<ExceptionHandlingMiddleware>();
        }
    }
}