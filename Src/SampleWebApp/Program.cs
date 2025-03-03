public class Program
{
    private readonly WebApplication _app;

    public Program(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        ConfigureServices(builder.Services);
        _app = builder.Build();
        Configure(_app);
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();
        services.AddOpenApi();
    }

    private void Configure(WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();
    }

    public void Run()
    {
        _app.Run();
    }

    public static void Main(string[] args)
    {
        new Program(args).Run();
    }
}