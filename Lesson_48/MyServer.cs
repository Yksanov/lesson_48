using System.Net;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Web;
using RazorEngine;
using RazorEngine.Templating;

namespace Lesson_48;

public class MyServer
{
    private string _siteDirectory;
    private HttpListener _listener;
    private int _port;
    private List<Employee> _employees = JsonSerializer.Deserialize<List<Employee>>(File.ReadAllText("../../../employees.json"));
    
    //--------------------------------------------------
    public async Task RunServerAsync(string path, int port)
    {
        _siteDirectory = path;
        _port = port;
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{_port.ToString()}/");
        _listener.Start();
        Console.WriteLine($"Server started on {_port} \nFiles in {_siteDirectory}");
        await ListenAsync();
    }
    //-------------------------------------------------

    private async Task ListenAsync()
    {
        try
        {
            while (true)
            {
                HttpListenerContext context = await _listener.GetContextAsync();
                Process(context);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
    //-------------------------------------------------

    private void Process(HttpListenerContext context)
    {
        Console.WriteLine(context.Request.HttpMethod);
        string filename = context.Request.Url.AbsolutePath;
        Console.WriteLine(filename);
        filename = _siteDirectory + filename;
        if (File.Exists(filename))
        {
            try
            {
                string content = BuildHtml(filename, new List<Employee>());
                // if (context.Request.HttpMethod == "POST" && context.Request.Url.AbsolutePath == "/addEmployee.html")
                // {
                //     StreamReader st = new StreamReader(context.Request.InputStream);
                //     content = BuildHtml(filename, new List<Employee>());
                // }
                
                if (context.Request.HttpMethod == "POST" && context.Request.Url.AbsolutePath == "/addEmployee.html")
                {
                    StreamReader st = new StreamReader(context.Request.InputStream);
                    string[] emp = st.ReadToEnd().Split("&");
                    
                    Employee employee = new Employee()
                    {
                        Id = Convert.ToInt32(emp[0].Split("=")[1]) + 1,
                        Name = HttpUtility.UrlDecode(emp[1].Split("=")[1]),
                        Surname = HttpUtility.UrlDecode(emp[2].Split("=")[1]),
                        About = HttpUtility.UrlDecode(emp[3].Split("=")[1]),
                        Age = Convert.ToInt32(emp[4].Split("=")[1])
                    };
                    _employees.Add(employee);
                    JsonSerializerOptions op = new JsonSerializerOptions()
                    {
                        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                        WriteIndented = true
                    };
                    
                    File.WriteAllText("../../../employees.json", JsonSerializer.Serialize(_employees, op));
                    context.Response.Redirect("/showEmployees.html");
                }

                if (context.Request.HttpMethod == "GET" && context.Request.Url.AbsolutePath == "/employee.html")
                {
                    List<Employee> employees = _employees.Where(e => e.Id == Convert.ToInt32(context.Request.QueryString["id"])).ToList();
                    content = BuildHtml(filename, employees);
                }
                
                if (context.Request.Url.AbsolutePath == "/showEmployees.html")
                {
                    List<Employee> employee = new List<Employee>(_employees);
                    if (context.Request.QueryString["IdFrom"] != null)
                    {
                        employee = employee.Where(e => e.Id >= Convert.ToInt32(context.Request.QueryString["IdFrom"])).ToList();
                    }
                    if (context.Request.QueryString["IdTo"] != null)
                    {
                        employee = employee.Where(e => e.Id <= Convert.ToInt32(context.Request.QueryString["IdTo"])).ToList();
                    }
                    content = BuildHtml(filename, employee);
                }
                context.Response.ContentType = GetContentType(filename);
                context.Response.ContentLength64 = System.Text.Encoding.UTF8.GetBytes(content).Length;
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(content);
                context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                context.Response.OutputStream.Flush();
                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                context.Response.OutputStream.Write(new byte[0]);

            }
        }
        else
        {
            context.Response.StatusCode = 404;
            context.Response.OutputStream.Write(new byte[0]);
        }
        context.Response.OutputStream.Close();
    }
    //-------------------------------------------------

    private string BuildHtml(string filename, List<Employee> employees)
    {
        string html = "";
        string layoutPath = _siteDirectory + "/layout.html";
        var razorService = Engine.Razor;
        if (!razorService.IsTemplateCached("layout", null))
            razorService.AddTemplate("layout", File.ReadAllText(layoutPath));
        if (!razorService.IsTemplateCached(filename, null))
        {
            razorService.AddTemplate(filename, File.ReadAllText(filename));
            razorService.Compile(filename);
        }
        var viewModel = new { Employee = employees };
        html = razorService.Run(filename, null, viewModel);
        return html;
    }
    
    private string BuildHtml(string filename, Employee? employee)
    {
        string html = "";
        string layoutPath = _siteDirectory + "/layout.html";
        var razorService = Engine.Razor;
        if (!razorService.IsTemplateCached("layout", null))
            razorService.AddTemplate("layout", File.ReadAllText(layoutPath));
        if (!razorService.IsTemplateCached(filename, null))
        {
            razorService.AddTemplate(filename, File.ReadAllText(filename));
            razorService.Compile(filename);
        }

        /*if (employee == null)
        {
            employee = new Employee();
            employee.Id = 0;
            employee.Name = "Not known";
        }*/
        var viewModel = new { Employee = employee };
        html = razorService.Run(filename, null, viewModel);
        return html;
    }
    //-------------------------------------------------
    
    private string? GetContentType(string filename)
    {
        var Dictionary = new Dictionary<string, string>()
        {
            {".css", "text/css"},
            {".js", "application/javascript"},
            {".png", "image/png"},
            {".jpg", "image/jpeg"},
            {".gif", "image/gif"},
            {".html", "text/html"},
            {".json", "application/json"}
        };
        string contentype = "";
        string extension = Path.GetExtension(filename);
        Dictionary.TryGetValue(extension, out contentype);
        return contentype;
    }
    public void Stop()
    {
        _listener.Abort();
        _listener.Stop();
    }
}