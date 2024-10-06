namespace Lesson_48;

class Program
{
    static async Task Main(string[] args)
    {
        MyServer server = new MyServer();
        await server.RunServerAsync("../../../site", 8080);
    }
}