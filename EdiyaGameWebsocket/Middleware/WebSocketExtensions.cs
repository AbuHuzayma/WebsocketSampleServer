namespace EdiyaGameWebsocket.Middleware
{
    public static class WebSocketExtensions
    {
        public static IApplicationBuilder UseWebSocket(this IApplicationBuilder app)
        {
            app.UseWebSockets();
            app.UseMiddleware<WebSocketMiddleware>();
            return app;
        }
    }
}
