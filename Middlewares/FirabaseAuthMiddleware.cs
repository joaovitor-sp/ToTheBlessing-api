using FirebaseAdmin.Auth;

namespace ToTheBlessing.Middlewares
{
    public class FirebaseAuthMiddleware
    {
        private readonly RequestDelegate _next;

        public FirebaseAuthMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (authHeader == null || !authHeader.StartsWith("Bearer "))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Token não enviado.");
                return;
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();

            try
            {
                Console.WriteLine($"[DEBUG] absolutePath (combinado): ");   
                FirebaseToken decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(token);

                context.Items["FirebaseUser"] = decodedToken;
                string uid = decodedToken.Uid;
                await _next(context);
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync($"Token inválido: {ex.Message}");
            }
        }
    }
}