using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System.Diagnostics;

class Program
{
    const string GITHUB_USER = "GITHUB_USER";
    const string GITHUB_TOKEN = "GITHUB_TOKEN";

    const string GITLAB_USER = "GITLAB_USER";
    const string GITLAB_PASSWORD = "GITLAB_PASSWORD";

    const string SELF_HOSTED_URL = "git.YOUR_SELF_HOSTED_NAME.app";
    const string ORGANIZATION = "ORGANIZATION";

    static async Task Main()
    {
        //if ( !File.Exists( "output.json" ) ) //For preventing executing the script twice if something fails with mirroring
        //    await RunPythonScriptAsync();

        //var result = await GetResultAsync();

        //var successProjects = result.Success;
        //var errorProjects = result.Error;

        //IWebDriver driver = new ChromeDriver();
        //WebDriverWait wait = new WebDriverWait( driver, TimeSpan.FromSeconds( 10 ) );

        //await SeleniumLogin( driver, wait );

        //errorProjects?.ForEach( project =>
        //{
        //    Console.WriteLine( $"Project {project.Name} ERROR CREATING PROJECT" );
        //} );

        //Console.WriteLine();

        //successProjects?.ForEach( project =>
        //{
        //    Console.WriteLine( $"Project {project.Name} CREATED" );
        //} );

        //Console.WriteLine();

        //foreach ( var project in successProjects ?? new() )
        //{
        //    await SeleniumMirroringAsync( driver, wait, project );
        //}

        //driver.Quit();

        Console.WriteLine( "Press any key to exit..." );
        Console.ReadKey();
    }

    static async Task RunPythonScriptAsync()
    {
        var currentDirectoryPath = Directory.GetParent( Environment.CurrentDirectory )?.Parent?.Parent?.FullName;
        var scriptPath = $"{currentDirectoryPath}\\preparemirroring.py";

        if ( !File.Exists( scriptPath ) ) throw new FileNotFoundException( "Python script not found" );

        ProcessStartInfo start = new ProcessStartInfo
        {
            FileName = "python",
            Arguments = scriptPath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using ( Process process = new Process { StartInfo = start } )
        {
            process.Start();

            string stderr = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if ( !string.IsNullOrEmpty( stderr ) )
                Console.WriteLine( "Python script error: " + stderr );
        }
    }

    static async Task<OutputData> GetResultAsync()
    {
        string json = await File.ReadAllTextAsync( "output.json" );

        return JsonConvert.DeserializeObject<OutputData>( json ) ?? new();
    }

    static async Task SeleniumLogin( IWebDriver driver, WebDriverWait wait )
    {
        driver.Navigate().GoToUrl( $"https://{SELF_HOSTED_URL}/users/sign_in" ); // Self hosted url
        //driver.Navigate().GoToUrl( $"https://gitlab.com/users/sign_in" ); // Gitlab url

        await Task.Delay( 500 );

        IWebElement userLoginInput = wait.Until( ExpectedConditions.ElementToBeClickable( By.Id( "user_login" ) ) );

        userLoginInput.Clear();
        userLoginInput.SendKeys( GITLAB_USER );

        IWebElement userLoginPassword = wait.Until( ExpectedConditions.ElementToBeClickable( By.Id( "user_password" ) ) );
        userLoginPassword.Clear();
        userLoginPassword.SendKeys( GITLAB_PASSWORD );

        IWebElement submitLogin = wait.Until( ExpectedConditions.ElementToBeClickable( By.Name( "commit" ) ) );
        submitLogin.Click();
    }

    static async Task SeleniumMirroringAsync( IWebDriver driver, WebDriverWait wait, GitProject gitProject )
    {
        try
        {
            driver.Navigate().GoToUrl( $"{gitProject.WebUrl}/-/settings/repository" );

            IWebElement expandSectionButton = wait.Until( ExpectedConditions.ElementToBeClickable( By.CssSelector( "#js-push-remote-settings .js-settings-toggle" ) ) );
            expandSectionButton.Click();

            await Task.Delay( 500 );

            IWebElement urlInput = wait.Until( ExpectedConditions.ElementToBeClickable( By.Id( "url" ) ) );
            urlInput.Clear();
            urlInput.SendKeys( $"https://{GITHUB_USER}@github.com/{ORGANIZATION}/{gitProject.Namespace}.git" ); //Remove organization if isn't an organization

            IWebElement passwordInput = wait.Until( ExpectedConditions.ElementToBeClickable( By.Id( "project_remote_mirrors_attributes_0_password" ) ) );
            passwordInput.Clear();
            passwordInput.SendKeys( GITHUB_TOKEN );

            await Task.Delay( 500 );

            IWebElement submitMirror = wait.Until( ExpectedConditions.ElementToBeClickable( By.Name( "update_remote_mirror" ) ) );
            submitMirror.Click();

            await Task.Delay( 500 );

            IWebElement updateMirror = wait.Until( ExpectedConditions.ElementToBeClickable( By.CssSelector( "div.btn-group.mirror-actions-group a[title='Update now']" ) ) );
            updateMirror.Click();

            Console.WriteLine( $"Project {gitProject.Namespace} MIRRORED" );
        }
        catch ( Exception ex )
        {
            Console.WriteLine( $"Project {gitProject.Namespace} ERROR ON MIRRORING" );
            Console.WriteLine( ex.Message );
        }
    }

    class OutputData
    {
        [JsonProperty( "success" )]
        public List<GitProject>? Success { get; set; }

        [JsonProperty( "error" )]
        public List<GitProject>? Error { get; set; }
    }

    class GitProject
    {
        [JsonProperty( "id" )]
        public int Id { get; set; }

        [JsonProperty( "name" )]
        public string? Name { get; set; }

        [JsonProperty( "namespace" )]
        public string? Namespace { get; set; }

        [JsonProperty( "weburl" )]
        public string? WebUrl { get; set; }

        [JsonProperty( "httpurl" )]
        public string? HttpUrl { get; set; }

        [JsonProperty( "sshurl" )]
        public string? SSHUrl { get; set; }

        [JsonProperty( "description" )]
        public string? Description { get; set; }

        [JsonProperty( "visibility" )]
        public string? Visibility { get; set; }
    }
}